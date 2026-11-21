using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Jamie
{
    [DisallowMultipleComponent]
    public sealed class VisionReactiveLight : MonoBehaviour
    {
        [SerializeField] private PlayerVision vision;
        [SerializeField] private Collider2D revealArea;
        [SerializeField] private Light2D[] controlledLights =
            new Light2D[0];

        [Header("Fade timings")]
        [SerializeField, Min(0.01f)]
        private float fadeInSeconds = 0.25f;

        [SerializeField, Min(0.01f)]
        private float fadeOutSeconds = 0.6f;

        [SerializeField, Min(0f)]
        private float holdSeconds = 0.15f;

        [SerializeField] private bool powered = true;

        private float[] normalIntensities;
        private float visibility;
        private float visibleUntil = float.NegativeInfinity;

        private void Awake()
        {
            if (vision == null)
            {
                vision = FindFirstObjectByType<PlayerVision>();
            }
            
            if (vision == null ||
                revealArea == null ||
                controlledLights == null ||
                controlledLights.Length == 0)
            {
                Debug.LogError(
                    "Assign Vision, Reveal Area and Controlled Lights.",
                    this);

                enabled = false;
                return;
            }

            foreach (Light2D light in controlledLights)
            {
                if (light == null || light.blendStyleIndex != 0)
                {
                    Debug.LogError(
                        "Controlled Lights must use Environment blend style.",
                        this);

                    enabled = false;
                    return;
                }
            }

            normalIntensities =
                new float[controlledLights.Length];

            for (int i = 0; i < controlledLights.Length; i++)
                normalIntensities[i] = controlledLights[i].intensity;

            ApplyBrightness(0f);
        }

        private void LateUpdate()
        {
            if (Time.timeScale <= 0f)
                return;

            if (powered && IsAreaVisible())
                visibleUntil = Time.time + holdSeconds;

            float target =
                powered && Time.time <= visibleUntil ? 1f : 0f;

            float duration = target > visibility
                ? fadeInSeconds
                : fadeOutSeconds;

            visibility = Mathf.MoveTowards(
                visibility,
                target,
                Time.deltaTime / Mathf.Max(duration, 0.001f));

            ApplyBrightness(
                Mathf.SmoothStep(0f, 1f, visibility));
        }

        private bool IsAreaVisible()
        {
            if (!revealArea.enabled ||
                !revealArea.gameObject.activeInHierarchy)
            {
                return false;
            }

            Vector2 closest =
                revealArea.ClosestPoint(vision.Origin);

            if (vision.CanSeePoint(closest))
                return true;

            Bounds bounds = revealArea.bounds;
            const int samples = 7;

            for (int y = 0; y < samples; y++)
            {
                for (int x = 0; x < samples; x++)
                {
                    Vector2 point = new(
                        Mathf.Lerp(
                            bounds.min.x,
                            bounds.max.x,
                            x / (samples - 1f)),
                        Mathf.Lerp(
                            bounds.min.y,
                            bounds.max.y,
                            y / (samples - 1f)));

                    if (revealArea.OverlapPoint(point) &&
                        vision.CanSeePoint(point))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void ApplyBrightness(float amount)
        {
            for (int i = 0; i < controlledLights.Length; i++)
            {
                if (controlledLights[i] != null)
                {
                    controlledLights[i].intensity =
                        normalIntensities[i] * amount;
                }
            }
        }

        public void SetPowered(bool value)
        {
            powered = value;
        }

        private void OnDisable()
        {
            if (normalIntensities != null)
                ApplyBrightness(1f);
        }
    }
}