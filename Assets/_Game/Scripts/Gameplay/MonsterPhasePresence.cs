using UnityEngine;

namespace Jamie.Gameplay
{
    [RequireComponent(typeof(CrawlerAI))]
    [DisallowMultipleComponent]
    public sealed class MonsterPhasePresence : MonoBehaviour
    {
        [SerializeField] private PhaseController phases;

        private CrawlerAI ai;
        private Collider2D bodyCollider;

        private Renderer[] visuals;
        private bool[] originalRendererStates;

        private bool originalIsTrigger;
        private bool ready;

        private void Awake()
        {
            ai = GetComponent<CrawlerAI>();
            bodyCollider = GetComponent<Collider2D>();

            if (phases == null)
                phases = FindFirstObjectByType<PhaseController>();

            if (phases == null || bodyCollider == null)
            {
                Debug.LogError(
                    "MonsterPhasePresence needs Phases and " +
                    "a body Collider2D on the crawler root.",
                    this);

                enabled = false;
                return;
            }

            originalIsTrigger = bodyCollider.isTrigger;

            visuals = GetComponentsInChildren<Renderer>(true);
            originalRendererStates = new bool[visuals.Length];

            for (int i = 0; i < visuals.Length; i++)
                originalRendererStates[i] = visuals[i].enabled;

            ready = true;
        }

        private void OnEnable()
        {
            if (!ready)
                return;

            phases.PhaseChanged += ApplyPhase;
            ApplyPhase(phases.CurrentPhase);
        }

        private void Start()
        {
            if (ready)
                ApplyPhase(phases.CurrentPhase);
        }

        private void OnDisable()
        {
            if (!ready)
                return;

            if (phases != null)
                phases.PhaseChanged -= ApplyPhase;

            SetVisible(true);

            if (bodyCollider != null)
                bodyCollider.isTrigger = originalIsTrigger;

            if (ai != null)
                ai.SetPlayerHuntingEnabled(true);
        }

        private void ApplyPhase(HospitalPhase phase)
        {
            bool hiddenPatrol = phase == HospitalPhase.First;

            ai.SetPlayerHuntingEnabled(!hiddenPatrol);

            bodyCollider.isTrigger =
                hiddenPatrol || originalIsTrigger;

            SetVisible(!hiddenPatrol);
        }

        public void SetVisible(bool visible)
        {
            if (!ready)
                return;

            for (int i = 0; i < visuals.Length; i++)
            {
                if (visuals[i] != null)
                {
                    visuals[i].enabled =
                        visible && originalRendererStates[i];
                }
            }
        }
    }
}