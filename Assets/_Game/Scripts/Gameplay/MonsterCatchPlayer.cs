using UnityEngine;
using UnityEngine.AI;

namespace Jamie.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class MonsterCatchPlayer : MonoBehaviour
    {
        [SerializeField] private PhaseController phases;
        [SerializeField] private DeathLoopController deathLoop;
        [Header("Chase speed")]
        [SerializeField, Min(0.1f)] private float chaseStartSpeed = 5f;
        [SerializeField, Min(0.1f)] private float chaseEndSpeed = 8f;
        [SerializeField, Min(0.1f)] private float speedRampSeconds = 5f;

        private NavMeshAgent chaseAgent;
        private float chaseElapsed;

        private bool caught;

        private void Awake()
        {
            if (phases == null)
                phases = FindFirstObjectByType<PhaseController>();

            if (deathLoop == null)
                deathLoop = FindFirstObjectByType<DeathLoopController>();

            if (phases == null || deathLoop == null)
            {
                Debug.LogError("Assign Phases and Death Loop.", this);
                enabled = false;
            }
        }

        private void Update()
        {
            if (caught || Time.timeScale <= 0f ||
                phases == null || phases.CurrentPhase != HospitalPhase.First ||
                deathLoop == null || deathLoop.IsTransitioning ||
                chaseAgent == null || !chaseAgent.isActiveAndEnabled ||
                !chaseAgent.isOnNavMesh)
            {
                return;
            }

            chaseElapsed += Time.deltaTime;

            float progress = Mathf.Clamp01(
                chaseElapsed / Mathf.Max(0.1f, speedRampSeconds));

            chaseAgent.speed = Mathf.Lerp(
                chaseStartSpeed, chaseEndSpeed, progress);
        }

        private void OnEnable()
        {
            chaseAgent = GetComponent<NavMeshAgent>();
            chaseElapsed = 0f;
            caught = false;

            if (chaseAgent != null)
                chaseAgent.speed = chaseStartSpeed;

            if (phases == null)
                return;

            phases.PhaseChanged += OnPhaseChanged;
            OnPhaseChanged(phases.CurrentPhase);
        }

        private void OnDisable()
        {
            if (phases != null)
                phases.PhaseChanged -= OnPhaseChanged;

            foreach (var emitter in
                GetComponentsInChildren<FMODUnity.StudioEventEmitter>(true))
            {
                if (emitter.IsActive)
                    emitter.Stop();
            }
        }

        private void OnPhaseChanged(HospitalPhase phase)
        {
            if (phase != HospitalPhase.First)
                gameObject.SetActive(false);
        }

        private void OnCollisionEnter2D(Collision2D collision)
            => TryCatch(collision.collider);

        private void OnCollisionStay2D(Collision2D collision)
            => TryCatch(collision.collider);

        private void OnTriggerEnter2D(Collider2D other)
            => TryCatch(other);

        private void OnTriggerStay2D(Collider2D other)
            => TryCatch(other);

        private void TryCatch(Collider2D other)
        {
            if (caught ||
                Time.timeScale <= 0f ||
                phases == null ||
                phases.CurrentPhase != HospitalPhase.First ||
                deathLoop == null ||
                deathLoop.IsTransitioning ||
                other.isTrigger ||
                other.GetComponentInParent<PlayerController>() == null)
            {
                return;
            }

            deathLoop.BeginDeath();
            caught = deathLoop.IsTransitioning;
        }
    }
}