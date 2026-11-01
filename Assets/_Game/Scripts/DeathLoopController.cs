using System.Collections;
using UnityEngine;

namespace Jamie
{
    public sealed class DeathLoopController : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private PlayerController player = null;
        [SerializeField] private CameraFollow cameraFollow = null;
        [SerializeField] private Transform bedSpawn = null;
        [SerializeField] private CanvasGroup fadeOverlay = null;

        [Header("Fade Timing")]
        [SerializeField, Min(0f)] private float fadeOutSeconds = 0.8f;
        [SerializeField, Min(0f)] private float blackHoldSeconds = 0.35f;
        [SerializeField, Min(0f)] private float fadeInSeconds = 1f;

        [Header("Reveal After First Death")]
        [SerializeField] private GameObject[] afterFirstDeathItems =
            new GameObject[0];

        private bool ready;
        private float timeScaleBeforeDeath = 1f;

        public int DeathCount { get; private set; }
        public bool IsTransitioning { get; private set; }

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

            fadeOverlay.alpha = 0f;
            fadeOverlay.blocksRaycasts = false;

            if (afterFirstDeathItems != null)
            {
                foreach (GameObject item in afterFirstDeathItems)
                {
                    if (item != null)
                        item.SetActive(false);
                }
            }
        }

        public void BeginDeath()
        {
            if (!ready || !isActiveAndEnabled ||
                IsTransitioning || Time.timeScale <= 0f)
                return;

            IsTransitioning = true;
            timeScaleBeforeDeath = Time.timeScale;
            Time.timeScale = 0f;
            fadeOverlay.blocksRaycasts = true;
            StartCoroutine(DeathSequence());
        }

        private IEnumerator DeathSequence()
        {
            yield return FadeTo(1f, fadeOutSeconds);

            player.RespawnAt((Vector2)bedSpawn.position);
            cameraFollow.SnapToTarget();
            DeathCount++;

            if (afterFirstDeathItems != null)
            {
                foreach (GameObject item in afterFirstDeathItems)
                {
                    if (item != null)
                        item.SetActive(true);
                }
            }

            Debug.Log("Respawned at bed. Deaths completed: " + DeathCount, this);

            yield return new WaitForSecondsRealtime(
                Mathf.Max(0f, blackHoldSeconds));
            yield return FadeTo(0f, fadeInSeconds);

            fadeOverlay.blocksRaycasts = false;
            Time.timeScale = timeScaleBeforeDeath;
            IsTransitioning = false;
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
            if (!IsTransitioning)
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
