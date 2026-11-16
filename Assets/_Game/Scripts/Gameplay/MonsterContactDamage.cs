using UnityEngine;

namespace Jamie.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class MonsterContactDamage : MonoBehaviour
    {
        [SerializeField] private PhaseController phases;

        [SerializeField] private HospitalPhase activePhase =
            HospitalPhase.Second;

        [SerializeField, Min(0f)]
        private float damage = 20f;

        private void Awake()
        {
            if (phases == null)
                phases = FindFirstObjectByType<PhaseController>();

            if (phases == null)
            {
                Debug.LogError(
                    "Assign Phases on MonsterContactDamage.", this);
                enabled = false;
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            TryDamage(collision.collider);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            TryDamage(collision.collider);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryDamage(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            TryDamage(other);
        }

        private void TryDamage(Collider2D other)
        {
            if (!isActiveAndEnabled ||
                Time.timeScale <= 0f ||
                phases == null ||
                phases.CurrentPhase != activePhase ||
                other == null ||
                other.isTrigger)
            {
                return;
            }

            PlayerHealth health =
                other.GetComponentInParent<PlayerHealth>();

            if (health != null)
                health.TakeDamage(damage);
        }
    }
}