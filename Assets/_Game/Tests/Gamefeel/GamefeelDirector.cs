using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace Jamie.Gamefeel
{
    [DefaultExecutionOrder(-150)]
    public sealed class GamefeelDirector : MonoBehaviour
    {
        [Header("Player and scene")]
        public PlayerController player;
        public CameraFollow cameraFollow;
        public Transform facing;
        public Transform spawn504;
        public Light2D flashlight;
        public GamefeelNavigation navigation;
        public GamefeelMonster placeholderMonster;
        public bool usePlaceholderMonster = true;
        public GamefeelInteractable[] interactables = new GamefeelInteractable[0];
        public Light2D[] hospitalLights = new Light2D[0];
        public GameObject exitSigns;
        public GameObject[] phaseTwoDiscoveries = new GameObject[0];
        public VisibilityMemory visibilityMemory;

        [Header("Gamefeel")]
        public float interactionRange = 1.8f;
        public float minimumHealth = 20f;
        public float blackoutLightMultiplier = 0.08f;
        public float numberBeatSeconds = 0.45f;

        [Header("HUD")]
        public Text numberText;
        public Text statusText;
        public Text promptText;
        public Text toastText;
        public Image healthFill;
        public Text healthText;
        public Image hurtOverlay;
        public CanvasGroup fade;
        public Text transitionNumber;
        public GameObject inspectionPanel;
        public Text inspectionTitle;
        public Text inspectionBody;
        public Button closeInspection;
        public GameObject pausePanel;
        public Button resumeButton;
        public GameObject endPanel;
        public Button restartButton;
        public Button pauseRestartButton;

        [SerializeField] private int countdown = 3;
        [SerializeField] private float health = 100f;
        private bool hasFlashlight;
        private bool batteryDead;
        private bool hasReceptionKey;
        private bool receptionUnlocked;
        private bool blackoutTriggered;
        private bool batteryInstalled;
        private bool cinematic;
        private bool transitioning;
        private bool paused;
        private bool inspecting;
        private bool complete;
        private bool ready;
        private float[] normalIntensities;
        private float normalFlashlightIntensity;
        private float toastUntil;
        private GamefeelInteractable currentTarget;
        private readonly List<Collider2D> nearby = new List<Collider2D>(24);
        private readonly List<RaycastHit2D> hits = new List<RaycastHit2D>(12);

        public PlayerController Player => player;
        public int Countdown => countdown;
        public float Health => health;
        public bool HasFlashlight => hasFlashlight;
        public bool FlashlightOn => hasFlashlight && !batteryDead && flashlight != null && flashlight.enabled;
        public bool HasReceptionKey => hasReceptionKey;
        public bool ReceptionUnlocked => receptionUnlocked;
        public bool BlackoutTriggered => blackoutTriggered;
        public bool BatteryInstalled => batteryInstalled;
        public bool TestComplete => complete;
        public bool IsBusy => cinematic || transitioning || paused || inspecting;
        public bool IsTransitioning => transitioning;
        public event Action<int> PhaseChanged;

        private void Awake()
        {
            ready = player != null && cameraFollow != null && facing != null && spawn504 != null &&
                flashlight != null && navigation != null && fade != null && numberText != null &&
                inspectionPanel != null && closeInspection != null;
            if (!ready)
            {
                Debug.LogError("Gamefeel_Test has an incomplete scene reference on GamefeelDirector.", this);
                enabled = false; return;
            }
            normalFlashlightIntensity = flashlight.intensity;
            normalIntensities = new float[hospitalLights.Length];
            for (int i = 0; i < hospitalLights.Length; i++)
                normalIntensities[i] = hospitalLights[i] != null ? hospitalLights[i].intensity : 0;
            closeInspection.onClick.AddListener(CloseInspection);
            if (resumeButton != null) resumeButton.onClick.AddListener(TogglePause);
            if (restartButton != null) restartButton.onClick.AddListener(RestartTest);
            if (pauseRestartButton != null) pauseRestartButton.onClick.AddListener(RestartTest);
            player.Configure(cameraFollow.GetComponent<Camera>(), facing, null);
            RestartTest();
        }

        private IEnumerator Start()
        {
            // Cancel only the old prototype's logging timer in this scene.
            yield return null;
            if (player != null) player.CancelInvoke("DecrementOxygen");
        }

        public void RestartTest()
        {
            if (!ready) return;
            StopAllCoroutines();
            Time.timeScale = 1;
            countdown = 3; health = 100;
            hasFlashlight = batteryDead = hasReceptionKey = receptionUnlocked = false;
            blackoutTriggered = batteryInstalled = cinematic = transitioning = paused = inspecting = complete = false;
            inspectionPanel.SetActive(false);
            if (pausePanel != null) pausePanel.SetActive(false);
            if (endPanel != null) endPanel.SetActive(false);
            fade.alpha = 0; fade.blocksRaycasts = false;
            transitionNumber.gameObject.SetActive(false);
            if (exitSigns != null) exitSigns.SetActive(false);
            foreach (GamefeelInteractable item in interactables)
            {
                if (item == null) continue;
                item.gameObject.SetActive(true); item.ResetForRun();
            }
            foreach (GameObject discovery in phaseTwoDiscoveries)
                if (discovery != null) discovery.SetActive(false);
            SetHospitalLights(1);
            flashlight.intensity = normalFlashlightIntensity; flashlight.enabled = false;
            player.enabled = true; player.RespawnAt(spawn504.position); cameraFollow.Configure(player.transform);
            cameraFollow.SnapToTarget();
            if (placeholderMonster != null) placeholderMonster.Hide();
            if (visibilityMemory != null)
                visibilityMemory.SendMessage("ClearExploredMemory", SendMessageOptions.DontRequireReceiver);
            ShowToast("A flashlight lies beside the bed.", 5);
            UpdateHud();
        }

        private void Update()
        {
            if (!ready) return;
            if (hurtOverlay != null)
            {
                Color c = hurtOverlay.color;
                c.a = Mathf.MoveTowards(c.a, 0, Time.unscaledDeltaTime * 0.55f);
                hurtOverlay.color = c;
            }
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.rKey.wasPressedThisFrame && !transitioning)
            { RestartTest(); return; }
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame && !transitioning && !complete)
            {
                if (inspecting) CloseInspection(); else TogglePause();
            }
            if (!IsBusy && !complete && keyboard != null && keyboard.fKey.wasPressedThisFrame)
            {
                if (!hasFlashlight) ShowToast("The flashlight is still beside the bed.");
                else if (batteryDead) ShowToast("The flashlight needs a fresh battery.");
                else flashlight.enabled = !flashlight.enabled;
            }
            UpdateHud();
        }

        private void LateUpdate()
        {
            currentTarget = null;
            if (!ready || IsBusy || complete) return;
            ContactFilter2D filter = new ContactFilter2D();
            filter.SetLayerMask((1 << 0) | (1 << 3) | (1 << 6)); filter.useTriggers = true;
            nearby.Clear();
            Physics2D.OverlapCircle(player.transform.position, interactionRange, filter, nearby);
            float best = interactionRange * interactionRange;
            foreach (Collider2D collider in nearby)
            {
                GamefeelInteractable item = collider.GetComponentInParent<GamefeelInteractable>();
                if (item == null || !item.isActiveAndEnabled || item.director != this) continue;
                float distance = ((Vector2)item.transform.position - (Vector2)player.transform.position).sqrMagnitude;
                if (distance <= best && CanReach(item)) { currentTarget = item; best = distance; }
            }
            promptText.text = currentTarget != null ? PromptFor(currentTarget) : string.Empty;
            if (currentTarget != null && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                TryInteract(currentTarget);
        }

        public bool CanReach(GamefeelInteractable item)
        {
            if (item == null || !item.gameObject.activeInHierarchy) return false;
            Vector2 direction = item.transform.position - player.transform.position;
            return direction.sqrMagnitude <= interactionRange * interactionRange &&
                (direction.sqrMagnitude < 0.01f || Vector2.Angle(facing.up, direction) <= 85f) &&
                HasLineOfSight(player.transform.position, item.transform);
        }

        public bool HasLineOfSight(Vector2 origin, Transform target)
        {
            ContactFilter2D filter = new ContactFilter2D();
            filter.SetLayerMask((1 << 0) | (1 << 6)); filter.useTriggers = false;
            hits.Clear(); Physics2D.Linecast(origin, target.position, filter, hits);
            foreach (RaycastHit2D hit in hits)
            {
                Transform obstacle = hit.transform;
                if (obstacle == player.transform || obstacle.IsChildOf(player.transform) ||
                    obstacle == target || obstacle.IsChildOf(target) || target.IsChildOf(obstacle) ||
                    placeholderMonster != null && obstacle.IsChildOf(placeholderMonster.transform)) continue;
                return false;
            }
            return true;
        }

        private string PromptFor(GamefeelInteractable item)
        {
            if (item.action == GamefeelAction.Door)
            {
                if (countdown > item.unlockAtNumber) return "E  -  Examine the " + item.unlockAtNumber + " on the door";
                if (item.requiresReceptionKey && !receptionUnlocked)
                    return hasReceptionKey ? "E  -  Unlock reception" : "E  -  Check reception lock";
                return "E  -  " + (item.IsOpen ? "Close " : "Open ") + item.displayName;
            }
            return "E  -  " + item.displayName;
        }

        public bool TryInteract(GamefeelInteractable item)
        {
            if (!ready || IsBusy || complete || !CanReach(item)) return false;
            switch (item.action)
            {
                case GamefeelAction.Door:
                    if (item.identifier == "room504" && !hasFlashlight && countdown == 3)
                    { ShowToast("Your flashlight is still beside the bed."); return false; }
                    if (countdown > item.unlockAtNumber)
                    {
                        ShowToast("The plate reads " + item.unlockAtNumber + ". The handle will not turn.");
                        if (item.identifier == "nurse" && countdown == 3 && receptionUnlocked && !blackoutTriggered)
                            StartCoroutine(NurseDoorBlackout());
                        return false;
                    }
                    if (item.requiresReceptionKey && !receptionUnlocked)
                    {
                        if (!hasReceptionKey) { ShowToast("Locked. A small key-shaped slot catches the light."); return false; }
                        receptionUnlocked = true; ShowToast("The reception lock clicks open.");
                    }
                    item.SetDoorOpen(!item.IsOpen);
                    return true;
                case GamefeelAction.Flashlight:
                    hasFlashlight = true; flashlight.enabled = !batteryDead;
                    item.gameObject.SetActive(false); ShowToast("Flashlight collected. F switches it on or off."); return true;
                case GamefeelAction.ReceptionKey:
                    hasReceptionKey = true; item.gameObject.SetActive(false);
                    ShowToast("A small brass key. Its tag reads RECEPTION.", 5); return true;
                case GamefeelAction.Battery:
                    if (!blackoutTriggered) { ShowToast("A fresh battery. Your flashlight is working for now."); return false; }
                    if (batteryInstalled) return false;
                    batteryInstalled = true; batteryDead = false; hasFlashlight = true;
                    flashlight.intensity = normalFlashlightIntensity; flashlight.enabled = true;
                    item.gameObject.SetActive(false); StartCoroutine(BatteryRevealAndChase()); return true;
                case GamefeelAction.Potion:
                    if (countdown != 2) return false;
                    item.gameObject.SetActive(false); StartCoroutine(PotionDeath()); return true;
                case GamefeelAction.Reveal:
                    item.ToggleReveal(); ShowToast(item.description, 6); return true;
                default:
                    OpenInspection(item.displayName, item.description); return true;
            }
        }

        private IEnumerator NurseDoorBlackout()
        {
            blackoutTriggered = true; cinematic = true;
            ShowToast("The handle refuses to move. The light in your hand sputters.", 5);
            for (int i = 0; i < 9; i++)
            {
                flashlight.enabled = hasFlashlight && i % 2 == 0;
                SetHospitalLights(Mathf.Lerp(1, blackoutLightMultiplier, i / 8f));
                yield return new WaitForSeconds(i % 2 == 0 ? 0.11f : 0.07f);
            }
            if (usePlaceholderMonster && placeholderMonster != null)
            {
                placeholderMonster.Reveal(new Vector2(11.5f, -21));
                yield return new WaitForSeconds(0.7f);
                placeholderMonster.Hide();
            }
            batteryDead = true; flashlight.enabled = false;
            SetHospitalLights(blackoutLightMultiplier);
            if (exitSigns != null) exitSigns.SetActive(true);
            cinematic = false;
            ShowToast("Red emergency lights flicker on.", 6);
        }

        private IEnumerator BatteryRevealAndChase()
        {
            cinematic = true; player.enabled = false;
            Vector2 behind = (Vector2)player.transform.position - (Vector2)facing.up * 3f;
            behind.x = Mathf.Clamp(behind.x, -50.5f, -37.5f);
            behind.y = Mathf.Clamp(behind.y, -10f, -1.5f);
            if (usePlaceholderMonster && placeholderMonster != null) placeholderMonster.Reveal(behind);
            ShowToast("Something is standing behind you.", 2);
            yield return new WaitForSeconds(1.1f);
            player.enabled = true; cinematic = false;
            ShowToast("RUN  -  HOLD SHIFT TO SPRINT", 3);
            if (usePlaceholderMonster && placeholderMonster != null) placeholderMonster.StartForcedChase();
        }

        public void ReportStoryCatch()
        {
            if (countdown != 3 || !batteryInstalled || transitioning || complete) return;
            StartCoroutine(DeathTransition(2));
        }

        public void ReportMonsterHit(float amount)
        {
            if (countdown != 2 || IsBusy || complete || !navigation.IsInHallway(player.transform.position)) return;
            health = Mathf.Max(minimumHealth, health - Mathf.Max(0, amount));
            if (hurtOverlay != null) hurtOverlay.color = new Color(0.7f, 0.02f, 0.01f, 0.38f);
            ShowToast("You're hurt. Get inside a room.", 3);
            UpdateHud();
        }

        private IEnumerator PotionDeath()
        {
            cinematic = true; player.enabled = false;
            ShowToast("The bottle is labelled RESTORATIVE. You swallow. Something is wrong.", 3);
            yield return new WaitForSeconds(1.1f);
            yield return DeathTransition(1);
        }

        private IEnumerator DeathTransition(int nextNumber)
        {
            if (transitioning) yield break;
            transitioning = true; cinematic = false; paused = inspecting = false;
            inspectionPanel.SetActive(false); if (pausePanel != null) pausePanel.SetActive(false);
            player.enabled = false;
            if (placeholderMonster != null) placeholderMonster.Hide();
            Time.timeScale = 0; fade.blocksRaycasts = true;
            yield return FadeTo(1, 0.8f);
            transitionNumber.text = countdown.ToString(); transitionNumber.color = Color.white;
            transitionNumber.gameObject.SetActive(true);
            yield return NumberColour(Color.white, new Color(0.8f, 0.07f, 0.05f), numberBeatSeconds);
            yield return new WaitForSecondsRealtime(0.2f);
            countdown = nextNumber; transitionNumber.text = countdown.ToString();
            yield return NumberColour(new Color(0.8f, 0.07f, 0.05f), Color.white, numberBeatSeconds);
            player.RespawnAt(spawn504.position); cameraFollow.SnapToTarget(); health = 100;
            flashlight.enabled = hasFlashlight; flashlight.intensity = normalFlashlightIntensity; batteryDead = false;
            SetHospitalLights(countdown == 2 ? 0.8f : 1f);
            if (exitSigns != null) exitSigns.SetActive(false);
            foreach (GamefeelInteractable item in interactables)
                if (item != null && item.action == GamefeelAction.Door)
                    item.SetDoorOpen(item.requiresReceptionKey && receptionUnlocked, false);
            foreach (GameObject discovery in phaseTwoDiscoveries)
                if (discovery != null) discovery.SetActive(countdown == 2);
            UpdateHud(); PhaseChanged?.Invoke(countdown);
            if (countdown == 2 && usePlaceholderMonster && placeholderMonster != null)
                placeholderMonster.StartRoaming(new Vector2(-7, 1.5f));
            yield return new WaitForSecondsRealtime(0.35f);
            yield return FadeTo(0, 0.8f);
            transitionNumber.gameObject.SetActive(false); fade.blocksRaycasts = false;
            Time.timeScale = 1; transitioning = false;
            if (countdown == 1)
            {
                complete = true;
                if (endPanel != null) endPanel.SetActive(true);
                ShowToast(string.Empty);
            }
            else
            {
                player.enabled = true;
                ShowToast("Back in 504. The doors marked 2 will now turn.", 6);
            }
        }

        private IEnumerator NumberColour(Color from, Color to, float duration)
        {
            float t = 0;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                transitionNumber.color = Color.Lerp(from, to, t / Mathf.Max(duration, 0.001f));
                yield return null;
            }
            transitionNumber.color = to;
        }

        private IEnumerator FadeTo(float destination, float duration)
        {
            float from = fade.alpha, elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                fade.alpha = Mathf.Lerp(from, destination, elapsed / Mathf.Max(duration, 0.001f));
                yield return null;
            }
            fade.alpha = destination;
        }

        private void SetHospitalLights(float multiplier)
        {
            for (int i = 0; i < hospitalLights.Length; i++)
                if (hospitalLights[i] != null) hospitalLights[i].intensity = normalIntensities[i] * multiplier;
        }

        private void ShowToast(string message, float seconds = 3)
        {
            if (toastText != null) toastText.text = message;
            toastUntil = Time.unscaledTime + seconds;
        }

        private void OpenInspection(string title, string description)
        {
            inspectionTitle.text = title; inspectionBody.text = description;
            inspectionPanel.SetActive(true); inspecting = true; Time.timeScale = 0;
        }

        public void CloseInspection()
        {
            if (!inspecting) return;
            inspectionPanel.SetActive(false); inspecting = false; Time.timeScale = 1;
        }

        public void TogglePause()
        {
            if (transitioning || complete || inspecting) return;
            paused = !paused; if (pausePanel != null) pausePanel.SetActive(paused);
            Time.timeScale = paused ? 0 : 1;
        }

        private void UpdateHud()
        {
            numberText.text = countdown.ToString();
            statusText.text = !hasFlashlight ? "FLASHLIGHT MISSING" : batteryDead ? "BATTERY EMPTY" :
                flashlight.enabled ? "FLASHLIGHT ON  [F]" : "FLASHLIGHT OFF  [F]";
            healthFill.fillAmount = health / 100f;
            healthFill.color = Color.Lerp(new Color(0.65f, 0.12f, 0.08f), new Color(0.55f, 0.66f, 0.48f), health / 100f);
            healthText.text = "HEALTH  " + Mathf.RoundToInt(health);
            if (Time.unscaledTime > toastUntil && toastText != null) toastText.text = string.Empty;
            if (IsBusy || complete) promptText.text = string.Empty;
        }

        private void OnDestroy()
        {
            if (closeInspection != null) closeInspection.onClick.RemoveListener(CloseInspection);
            if (resumeButton != null) resumeButton.onClick.RemoveListener(TogglePause);
            if (restartButton != null) restartButton.onClick.RemoveListener(RestartTest);
            if (pauseRestartButton != null) pauseRestartButton.onClick.RemoveListener(RestartTest);
            if (transitioning || paused || inspecting) Time.timeScale = 1;
        }
    }
}
