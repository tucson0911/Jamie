using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Jamie.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class HospitalPowerOutage : MonoBehaviour
    {
        [SerializeField] private PhaseController phases;
        [SerializeField] private PlayerFlashlight flashlight;
        [SerializeField] private Light2D[] hospitalLights = new Light2D[0];
        [SerializeField] private Light2D[] emergencyLights = new Light2D[0];
        [SerializeField] private SpriteRenderer[] emergencyFixtures = new SpriteRenderer[0];
        [SerializeField, Min(0.1f)] private float outageSeconds = 1.4f;
        [SerializeField, Range(0f, 1f)] private float remainingLight = 0.04f;
        [SerializeField, Min(0.1f)] private float restoreSeconds = 0.7f;

        private float[] normalIntensities;
        private float[] emergencyIntensities;
        private Coroutine transition;
        private bool ready;
        private bool triggered;

        public bool IsOutage { get; private set; }
        public bool HasTriggered => triggered;

        private void Awake()
        {
            if (phases == null || flashlight == null)
            {
                Debug.LogError("Assign Phases and Flashlight on HospitalPowerOutage.", this);
                enabled = false;
                return;
            }

            normalIntensities = new float[hospitalLights.Length];
            for (int i = 0; i < hospitalLights.Length; i++)
                normalIntensities[i] = hospitalLights[i] != null ? hospitalLights[i].intensity : 0f;

            emergencyIntensities = new float[emergencyLights.Length];
            for (int i = 0; i < emergencyLights.Length; i++)
                emergencyIntensities[i] = emergencyLights[i] != null ? emergencyLights[i].intensity : 0f;

            SetEmergency(0f);
            ready = true;
        }

        private void OnEnable()
        {
            if (phases != null)
                phases.PhaseChanged += OnPhaseChanged;
        }

        private void OnDisable()
        {
            if (phases != null)
                phases.PhaseChanged -= OnPhaseChanged;
            if (!ready) return;
            StopAllCoroutines();
            SetHospital(1f);
            SetEmergency(0f);
            flashlight.SetInputBlocked(false);
            flashlight.SetPowerAvailable(true);
        }

        private void OnPhaseChanged(HospitalPhase phase)
        {
            if (phase != HospitalPhase.First && IsOutage)
                RestorePower();
        }

        public void BeginOutage()
        {
            if (!ready || !isActiveAndEnabled || triggered ||
                phases.CurrentPhase != HospitalPhase.First || Time.timeScale <= 0f)
                return;

            triggered = true;
            IsOutage = true;
            if (transition != null) StopCoroutine(transition);
            transition = StartCoroutine(FailPower());
            Debug.Log("[Power] Hospital outage; emergency route enabled.", this);
        }

        private IEnumerator FailPower()
        {
            bool wasOn = flashlight.IsOn;
            flashlight.SetInputBlocked(true);
            float elapsed = 0f;
            while (elapsed < outageSeconds)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / outageSeconds);
                SetHospital(Mathf.Lerp(1f, remainingLight, t));
                SetEmergency(Mathf.SmoothStep(0f, 1f, t));

                bool pulse = t < 0.12f || (t > 0.28f && t < 0.40f) ||
                    (t > 0.62f && t < 0.74f);
                flashlight.SetOn(wasOn && pulse);
                yield return null;
            }

            SetHospital(remainingLight);
            SetEmergency(1f);
            flashlight.SetPowerAvailable(false);
            flashlight.SetInputBlocked(false);
            transition = null;
        }

        public void RestorePower()
        {
            if (!ready || !isActiveAndEnabled) return;
            if (transition != null) StopCoroutine(transition);
            IsOutage = false;
            flashlight.SetInputBlocked(false);
            flashlight.SetPowerAvailable(true);
            flashlight.SetOn(flashlight.HasFlashlight);
            transition = StartCoroutine(RestoreLighting());
        }

        private IEnumerator RestoreLighting()
        {
            float[] from = new float[hospitalLights.Length];
            for (int i = 0; i < from.Length; i++)
                from[i] = hospitalLights[i] != null ? hospitalLights[i].intensity : 0f;
            float elapsed = 0f;
            while (elapsed < restoreSeconds)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / restoreSeconds);
                for (int i = 0; i < from.Length; i++)
                    if (hospitalLights[i] != null)
                        hospitalLights[i].intensity = Mathf.Lerp(from[i], normalIntensities[i], t);
                SetEmergency(1f - t);
                yield return null;
            }
            SetHospital(1f);
            SetEmergency(0f);
            transition = null;
        }

        private void SetHospital(float multiplier)
        {
            for (int i = 0; i < hospitalLights.Length; i++)
                if (hospitalLights[i] != null)
                    hospitalLights[i].intensity = normalIntensities[i] * multiplier;
        }

        private void SetEmergency(float amount)
        {
            for (int i = 0; i < emergencyLights.Length; i++)
                if (emergencyLights[i] != null)
                    emergencyLights[i].intensity = emergencyIntensities[i] * amount;
            foreach (SpriteRenderer fixture in emergencyFixtures)
                if (fixture != null)
                    fixture.enabled = amount > 0.01f;
        }
    }
}
