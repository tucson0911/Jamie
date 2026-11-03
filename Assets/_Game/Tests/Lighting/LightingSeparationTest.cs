using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

namespace Jamie.Tests
{
    // This component changes only its scene's live objects on entering Play mode.
    // Shared materials and prefabs are never edited.
    [DefaultExecutionOrder(-200)]
    public sealed class LightingSeparationTest : MonoBehaviour
    {
        [Header("Test Assets")]
        [SerializeField] private Shader objectShader = null;
        [SerializeField] private Shader structureShader = null;
        [SerializeField] private Sprite pickupSprite = null;

        [Header("Vision (does not illuminate)")]
        [SerializeField, Range(30f, 360f)] private float visionAngle = 160f;
        [SerializeField, Min(1f)] private float visionRange = 8f;
        [SerializeField, Range(0f, 0.1f)] private float minimumSeenLight = 0.015f;
        [SerializeField, Range(0.02f, 0.5f)] private float fullySeenLight = 0.10f;

        [Header("Hallway Lighting")]
        [SerializeField, Range(0f, 0.1f)] private float ambientIntensity = 0.003f;
        [SerializeField, Range(0f, 2f)] private float startingRoomIntensity = 0.7f;
        [SerializeField, Range(0f, 1f)] private float hallEntranceIntensity = 0.20f;
        [SerializeField, Range(0f, 0.5f)] private float waitingAreaIntensity = 0.045f;
        [SerializeField, Range(0f, 0.5f)] private float pickupLampIntensity = 0.12f;

        [Header("Flashlight Pickup")]
        [SerializeField] private bool startWithFlashlight = false;
        [SerializeField] private Vector2 pickupPosition = new Vector2(4.25f, 0f);
        [SerializeField, Min(0.1f)] private float interactionDistance = 1.8f;
        [SerializeField, Range(0f, 5f)] private float flashlightIntensity = 1.5f;
        [SerializeField, Range(10f, 150f)] private float flashlightAngle = 64f;
        [SerializeField, Min(1f)] private float flashlightRange = 8f;

        private readonly List<Material> temporaryMaterials = new List<Material>();
        private readonly List<RaycastHit2D> obstacleHits = new List<RaycastHit2D>(8);
        private readonly List<Collider2D> nearbyNotes = new List<Collider2D>(8);
        private PlayerController player;
        private Transform facing;
        private Light2D sightMask;
        private Light2D torch;
        private Light2D ambient;
        private Light2D startingRoom;
        private Light2D entrance;
        private Light2D waitingArea;
        private Light2D pickupLamp;
        private GameObject pickup;
        private Material pickupMaterial;
        private Camera memoryCamera;
        private Texture2D interactionSample;
        private bool ownsFlashlight;
        private bool torchOn;
        private bool ready;

        public bool OwnsFlashlight => ownsFlashlight;
        public bool FlashlightOn => torch != null && torch.enabled;
        public Light2D SightMask => sightMask;
        public Light2D Flashlight => torch;

        private T[] SceneComponents<T>() where T : Component
        {
            List<T> result = new List<T>();
            foreach (GameObject root in gameObject.scene.GetRootGameObjects())
                result.AddRange(root.GetComponentsInChildren<T>(true));
            return result.ToArray();
        }

        private void Awake()
        {
            PlayerController[] players = SceneComponents<PlayerController>();
            CameraFollow[] cameras = SceneComponents<CameraFollow>();
            if (players.Length != 1 || cameras.Length != 1 ||
                objectShader == null || structureShader == null || pickupSprite == null)
            {
                Debug.LogError("Lighting test needs one player/camera and its three test assets. " +
                    "Players=" + players.Length + ", cameras=" + cameras.Length +
                    ", object shader=" + (objectShader != null) +
                    ", structure shader=" + (structureShader != null) +
                    ", pickup sprite=" + (pickupSprite != null), this);
                enabled = false;
                return;
            }

            player = players[0];
            foreach (Transform child in player.GetComponentsInChildren<Transform>(true))
                if (child.name == "Facing - mouse aim")
                    facing = child;

            if (facing == null)
            {
                Debug.LogError("Lighting test could not find the player's aiming transform.", this);
                enabled = false;
                return;
            }

            // Movement and aiming stay on PlayerController. This test owns F input,
            // so its original visionCone toggle is explicitly left unassigned.
            player.Configure(cameras[0].GetComponent<Camera>(), facing, null);
            cameras[0].Configure(player.transform);
            foreach (Light2D light in SceneComponents<Light2D>())
                light.enabled = false;
            foreach (PlayfieldHud hud in SceneComponents<PlayfieldHud>())
                hud.enabled = false;
            foreach (NoteInteractionHandler handler in SceneComponents<NoteInteractionHandler>())
                handler.enabled = false;
            foreach (VisibilityMemory memory in SceneComponents<VisibilityMemory>())
                memoryCamera = memory.GetComponent<Camera>();

            ReplaceSceneMaterials();
            sightMask = MakeLight("Vision mask - no illumination", facing,
                new Vector2(0f, 0.28f), 1, Color.white, 1.25f, visionRange, visionAngle);
            torch = MakeLight("Flashlight beam", facing,
                new Vector2(0f, 0.28f), 2, new Color(1f, 0.88f, 0.64f),
                flashlightIntensity, flashlightRange, flashlightAngle);

            ambient = MakeLight("Test ambient", transform, Vector2.zero,
                0, new Color(0.76f, 0.85f, 0.8f), ambientIntensity, 1f, 360f);
            ambient.lightType = Light2D.LightType.Global;
            startingRoom = MakeLight("Room 1 ceiling", transform,
                new Vector2(-5.75f, -9.5f), 0, new Color(1f, 0.92f, 0.78f),
                startingRoomIntensity, 6.8f, 360f);
            entrance = MakeLight("Hall entrance", transform,
                new Vector2(-4.75f, -4f), 0, new Color(0.8f, 0.87f, 1f),
                hallEntranceIntensity, 3.6f, 360f);
            waitingArea = MakeLight("Waiting area - dim", transform,
                new Vector2(-2.8f, 0f), 0, new Color(0.8f, 0.87f, 1f),
                waitingAreaIntensity, 4.5f, 360f);
            pickupLamp = MakeLight("Small lamp by flashlight", transform,
                pickupPosition, 0, new Color(1f, 0.9f, 0.65f),
                pickupLampIntensity, 2f, 360f);

            CreatePickup();
            interactionSample = new Texture2D(1, 1, TextureFormat.RGBAFloat, false, true);
            ready = true;
            ResetFlashlight();
            ApplySettings();
        }

        private void ReplaceSceneMaterials()
        {
            Dictionary<Material, Material> replacements = new Dictionary<Material, Material>();
            foreach (SpriteRenderer sprite in SceneComponents<SpriteRenderer>())
            {
                Material original = sprite.sharedMaterial;
                if (original == null)
                    continue;
                Shader replacement = original.shader.name == "Jamie/Flashlight Reveal" ? objectShader
                    : original.shader.name == "Jamie/Structure Visibility" ? structureShader : null;
                if (replacement == null)
                    continue;
                if (!replacements.TryGetValue(original, out Material material))
                {
                    material = new Material(original) { shader = replacement,
                        name = original.name + " (lighting test)" };
                    replacements.Add(original, material);
                    temporaryMaterials.Add(material);
                }
                sprite.sharedMaterial = material;
            }
        }

        private Light2D MakeLight(string label, Transform parent, Vector2 position,
            int blendStyle, Color color, float intensity, float radius, float angle)
        {
            GameObject lightObject = new GameObject(label);
            lightObject.layer = LayerMask.NameToLayer("MemoryCapture");
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.localPosition = position;
            Light2D light = lightObject.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.blendStyleIndex = blendStyle;
            light.color = color;
            light.intensity = intensity;
            light.pointLightOuterRadius = radius;
            light.pointLightInnerRadius = 0.5f;
            light.pointLightOuterAngle = angle;
            light.pointLightInnerAngle = angle * 0.65f;
            light.shadowIntensity = 1f;
            light.shadowsEnabled = true;
            return light;
        }

        private void CreatePickup()
        {
            pickup = new GameObject("Flashlight pickup (placeholder)");
            pickup.transform.SetParent(transform, false);
            pickup.transform.position = pickupPosition;
            pickupMaterial = new Material(objectShader) { name = "Test flashlight pickup" };
            pickupMaterial.SetFloat("_HighlightStrength", 0f);
            temporaryMaterials.Add(pickupMaterial);
            AddPickupPart("Handle", new Vector2(0f, -0.10f),
                new Vector2(0.16f, 0.40f), new Color(0.4f, 0.45f, 0.42f));
            AddPickupPart("Lens", new Vector2(0f, 0.15f),
                new Vector2(0.28f, 0.16f), new Color(0.85f, 0.8f, 0.56f));
        }

        private void AddPickupPart(string label, Vector2 position, Vector2 size, Color color)
        {
            GameObject part = new GameObject(label);
            part.transform.SetParent(pickup.transform, false);
            part.transform.localPosition = position;
            part.transform.localScale = new Vector3(
                size.x / pickupSprite.bounds.size.x, size.y / pickupSprite.bounds.size.y, 1f);
            SpriteRenderer sprite = part.AddComponent<SpriteRenderer>();
            sprite.sprite = pickupSprite;
            sprite.color = color;
            sprite.sharedMaterial = pickupMaterial;
            sprite.sortingLayerName = "Props";
            sprite.sortingOrder = 10;
        }

        private void ApplySettings()
        {
            sightMask.pointLightOuterAngle = visionAngle;
            sightMask.pointLightInnerAngle = visionAngle * 0.75f;
            sightMask.pointLightOuterRadius = visionRange;
            torch.pointLightOuterAngle = flashlightAngle;
            torch.pointLightInnerAngle = flashlightAngle * 0.6f;
            torch.pointLightOuterRadius = Mathf.Min(flashlightRange, visionRange);
            torch.intensity = flashlightIntensity;
            torch.enabled = ownsFlashlight && torchOn;
            ambient.intensity = ambientIntensity;
            startingRoom.intensity = startingRoomIntensity;
            entrance.intensity = hallEntranceIntensity;
            waitingArea.intensity = waitingAreaIntensity;
            pickupLamp.intensity = pickupLampIntensity;
            pickupLamp.transform.position = pickupPosition;
            pickup.transform.position = pickupPosition;
            Shader.SetGlobalVector("_JamieTestSeenLightRange", new Vector4(
                minimumSeenLight, Mathf.Max(fullySeenLight, minimumSeenLight + 0.001f), 0f, 0f));
        }

        private void Update()
        {
            if (!ready)
                return;
            ApplySettings();
            if (Time.timeScale <= 0f || Keyboard.current == null)
                return;

            Keyboard keyboard = Keyboard.current;
            if (ownsFlashlight && keyboard.fKey.wasPressedThisFrame)
                SetFlashlight(!torchOn);
            if (keyboard.tKey.wasPressedThisFrame)
                ResetFlashlight();
            if (keyboard.eKey.wasPressedThisFrame)
            {
                if (!ownsFlashlight && CanReach(pickup.transform))
                    CollectFlashlight();
                else
                    TryReadNote();
            }
        }

        public void CollectFlashlight()
        {
            if (!ready)
                return;
            ownsFlashlight = true;
            pickup.SetActive(false);
            SetFlashlight(true);
        }

        public void SetFlashlight(bool on)
        {
            torchOn = ownsFlashlight && on;
            if (torch != null)
                torch.enabled = torchOn;
        }

        public void ResetFlashlight()
        {
            ownsFlashlight = startWithFlashlight;
            pickup.SetActive(!ownsFlashlight);
            SetFlashlight(ownsFlashlight);
        }

        private bool CanReach(Transform target)
        {
            if (Vector2.Distance(player.transform.position, target.position) > interactionDistance)
                return false;
            Vector2 direction = target.position - sightMask.transform.position;
            if (Vector2.Angle(sightMask.transform.up, direction) > visionAngle * 0.5f)
                return false;

            ContactFilter2D filter = new ContactFilter2D();
            filter.SetLayerMask((1 << 0) | (1 << 6));
            filter.useTriggers = false;
            obstacleHits.Clear();
            Physics2D.Linecast(sightMask.transform.position, target.position, filter, obstacleHits);
            foreach (RaycastHit2D hit in obstacleHits)
                if (!hit.transform.IsChildOf(player.transform) && hit.transform != player.transform &&
                    !hit.transform.IsChildOf(target) && hit.transform != target)
                    return false;
            return true;
        }

        private bool IsReadable(Vector2 position)
        {
            if (memoryCamera == null || memoryCamera.targetTexture == null)
                return false;
            RenderTexture capture = memoryCamera.targetTexture;
            Vector3 uv = memoryCamera.WorldToViewportPoint(position);
            if (uv.x < 0f || uv.x >= 1f || uv.y < 0f || uv.y >= 1f)
                return false;

            // Read one visibility pixel only on E, not every frame. The capture's R
            // channel already combines sight, real illumination, and wall shadows.
            RenderTexture previous = RenderTexture.active;
            try
            {
                RenderTexture.active = capture;
                interactionSample.ReadPixels(new Rect(
                    Mathf.FloorToInt(uv.x * capture.width),
                    Mathf.FloorToInt(uv.y * capture.height), 1f, 1f), 0, 0, false);
                interactionSample.Apply(false);
                return interactionSample.GetPixel(0, 0).r > 0.02f;
            }
            finally
            {
                RenderTexture.active = previous;
            }
        }

        private void TryReadNote()
        {
            if (NoteManager.Instance == null || NoteManager.Instance.IsOpen)
                return;
            ContactFilter2D filter = new ContactFilter2D();
            filter.SetLayerMask(1 << 3);
            filter.useTriggers = true;
            nearbyNotes.Clear();
            Physics2D.OverlapCircle(player.transform.position, interactionDistance, filter, nearbyNotes);
            NoteItem best = null;
            float bestDistance = interactionDistance;
            foreach (Collider2D collider in nearbyNotes)
            {
                NoteItem note = collider.GetComponentInParent<NoteItem>();
                if (note == null || !note.isActiveAndEnabled || !CanReach(note.transform))
                    continue;
                float distance = Vector2.Distance(player.transform.position, note.transform.position);
                if (distance <= bestDistance && IsReadable(note.transform.position))
                {
                    best = note;
                    bestDistance = distance;
                }
            }
            if (best != null)
                NoteManager.Instance.OpenNote(best);
        }

        private void OnGUI()
        {
            if (!ready || Time.timeScale <= 0f)
                return;
            float scale = Mathf.Max(0.65f, Screen.height / 720f);
            Matrix4x4 previous = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            GUI.Box(new Rect(14f, 14f, 460f, 110f), "VISION / LIGHTING TEST");
            GUI.Label(new Rect(26f, 40f, 440f, 25f), ownsFlashlight
                ? "Flashlight: " + (FlashlightOn ? "ON" : "OFF") + "    |    F: toggle    T: reset pickup"
                : "No flashlight. Follow the corridor to the waiting area.");
            GUI.Label(new Rect(26f, 66f, 440f, 25f),
                "WASD: move    Mouse: look    Shift: sprint    E: interact");
            GUI.Label(new Rect(26f, 92f, 440f, 25f),
                !ownsFlashlight && CanReach(pickup.transform)
                    ? "E - pick up flashlight" : "Vision stays active when the flashlight is off.");
            GUI.matrix = previous;
        }

        private void OnDestroy()
        {
            foreach (Material material in temporaryMaterials)
                if (material != null)
                    Destroy(material);
            if (interactionSample != null)
                Destroy(interactionSample);
        }
    }
}
