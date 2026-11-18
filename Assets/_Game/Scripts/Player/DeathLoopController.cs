using System.Collections;
using Jamie.Gameplay;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace Jamie
{
    public sealed class DeathLoopController : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private PlayerController player;
        [SerializeField] private CameraFollow cameraFollow;
        [SerializeField] private Transform bedSpawn;
        [SerializeField] private CanvasGroup fadeOverlay;

        [Header("Scene Transition")]
        [Tooltip("Leave empty to respawn in the current scene.")]
        [SerializeField] private string nextPhaseScene = "";

        [Header("Fade Timing")]
        [SerializeField, Min(0f)] private float fadeOutSeconds = 0.8f;
        [SerializeField, Min(0f)] private float blackHoldSeconds = 0.35f;
        [SerializeField, Min(0f)] private float fadeInSeconds = 1f;
        [SerializeField, Min(0f)] private float beforeRespawnHoldSeconds = 0f;

        [Header("Reveal After First Death")]
        [SerializeField] private GameObject[] afterFirstDeathItems =
            new GameObject[0];

        [Header("Events")]
        [SerializeField] private UnityEvent onRespawned = new UnityEvent();
        [SerializeField] private UnityEvent onTransitionFinished = new UnityEvent();
        [SerializeField] private UnityEvent onBlackReached = new UnityEvent();
        [SerializeField] private UnityEvent onFadeInStarting = new UnityEvent();

        private bool ready;
        private bool arrivingFromScene;
        private bool loadingNextScene;
        private float timeScaleBeforeDeath = 1f;

        public int DeathCount { get; private set; }
        public bool IsTransitioning { get; private set; }

        private bool HasNextScene =>
            !string.IsNullOrWhiteSpace(nextPhaseScene);

        private void Awake()
        {
            ready = player != null && cameraFollow != null &&
                bedSpawn != null && fadeOverlay != null;

            if (!ready)
            {
                Debug.LogError(
                    "Assign Player, Camera Follow, Bed Spawn and Fade Overlay " +
                    "on DeathLoopController.", this);
                enabled = false;
                return;
            }

            arrivingFromScene = GameSession.GetOrCreate()
                .TryTakeSceneArrival(
                    gameObject.scene.name, out timeScaleBeforeDeath);

            IsTransitioning = arrivingFromScene;
            fadeOverlay.alpha = arrivingFromScene ? 1f : 0f;
            fadeOverlay.blocksRaycasts = arrivingFromScene;

            if (arrivingFromScene)
                Time.timeScale = 0f;

            SetAfterDeathItems(false);
        }

        private IEnumerator Start()
        {
            if (!ready || !arrivingFromScene)
                yield break;

            cameraFollow.SnapToTarget();
            onBlackReached.Invoke();

            Debug.Log(
                $"[DeathLoop] Arrived in {gameObject.scene.name}.", this);

            yield return FinishTransition();
        }

        [ContextMenu("Begin Death (Play Mode)")]
        public void BeginDeath()
        {
            if (!Application.isPlaying ||
                !ready || !isActiveAndEnabled ||
                IsTransitioning || Time.timeScale <= 0f)
            {
                return;
            }

            if (HasNextScene &&
                !Application.CanStreamedLevelBeLoaded(nextPhaseScene))
            {
                Debug.LogError(
                    $"Cannot load '{nextPhaseScene}'. " +
                    "Add it to the enabled Build Profiles scene list.", this);
                return;
            }

            IsTransitioning = true;
            timeScaleBeforeDeath = Time.timeScale;
            Time.timeScale = 0f;
            fadeOverlay.blocksRaycasts = true;

            StartCoroutine(DeathSequence());
        }

        private IEnumerator DeathSequence()
        {
            yield return FadeTo(1f, fadeOutSeconds);
            onBlackReached.Invoke();

            if (beforeRespawnHoldSeconds > 0f)
            {
                yield return new WaitForSecondsRealtime(
                    beforeRespawnHoldSeconds);
            }

            if (HasNextScene)
            {
                GameSession.GetOrCreate().PrepareSceneChange(
                    nextPhaseScene, timeScaleBeforeDeath);

                loadingNextScene = true;

                Debug.Log(
                    $"[DeathLoop] Loading {nextPhaseScene}.", this);

                yield return SceneManager.LoadSceneAsync(
                    nextPhaseScene, LoadSceneMode.Single);

                yield break;
            }

            player.RespawnAt((Vector2)bedSpawn.position);
            cameraFollow.SnapToTarget();
            DeathCount++;

            onRespawned.Invoke();
            SetAfterDeathItems(true);

            Debug.Log(
                "Respawned at bed. Deaths completed: " + DeathCount, this);

            yield return FinishTransition();
        }

        private IEnumerator FinishTransition()
        {
            yield return new WaitForSecondsRealtime(
                Mathf.Max(0f, blackHoldSeconds));

            onFadeInStarting.Invoke();

            yield return FadeTo(0f, fadeInSeconds);

            fadeOverlay.blocksRaycasts = false;
            Time.timeScale = timeScaleBeforeDeath;
            IsTransitioning = false;

            onTransitionFinished.Invoke();
        }

        private void SetAfterDeathItems(bool visible)
        {
            if (afterFirstDeathItems == null)
                return;

            foreach (GameObject item in afterFirstDeathItems)
            {
                if (item != null)
                    item.SetActive(visible);
            }
        }

        private IEnumerator FadeTo(float targetAlpha, float seconds)
        {
            if (seconds <= 0f)
            {
                fadeOverlay.alpha = targetAlpha;
                yield break;
            }

            float initialAlpha = fadeOverlay.alpha;
            float elapsed = 0f;

            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;

                fadeOverlay.alpha = Mathf.Lerp(
                    initialAlpha, targetAlpha,
                    Mathf.Clamp01(elapsed / seconds));

                yield return null;
            }

            fadeOverlay.alpha = targetAlpha;
        }

        private void OnDisable()
        {
            // The arriving scene takes over the fade and paused time
            if (!IsTransitioning || loadingNextScene)
                return;

            StopAllCoroutines();

            if (fadeOverlay != null)
            {
                fadeOverlay.alpha = 0f;
                fadeOverlay.blocksRaycasts = false;
            }

            Time.timeScale = timeScaleBeforeDeath;
            IsTransitioning = false;
        }
    }
}