using UnityEngine;
using UnityEngine.Events;

namespace Jamie.Gameplay
{
    [RequireComponent(typeof(DoorMechanism))]
    [DisallowMultipleComponent]
    public sealed class PhaseDoorLock : MonoBehaviour
    {
        [SerializeField] private PhaseController phases;

        [SerializeField] private HospitalPhase unlockFrom =
            HospitalPhase.Second;

        [SerializeField]
        private UnityEvent onLockedAttempt = new UnityEvent();

        private DoorMechanism door;

        public int DoorNumber => 4 - (int)unlockFrom;

        public bool IsLocked =>
            phases == null ||
            (int)phases.CurrentPhase < (int)unlockFrom;

        private void Awake()
        {
            door = GetComponent<DoorMechanism>();

            if (phases == null)
                phases = FindFirstObjectByType<PhaseController>();

            if (phases == null)
            {
                Debug.LogError(
                    "Assign Phases on PhaseDoorLock.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (phases == null)
                return;

            phases.PhaseChanged += Refresh;
            Refresh(phases.CurrentPhase);
        }

        private void OnDisable()
        {
            if (phases != null)
                phases.PhaseChanged -= Refresh;
        }

        private void Refresh(HospitalPhase phase)
        {
            if (IsLocked && door != null)
                door.SetOpen(false);
        }

        public bool TryAccess()
        {
            if (phases == null)
                return false;

            if (!IsLocked)
                return true;

            Debug.Log(
                $"[Door] This door is marked {DoorNumber}.",
                this);

            onLockedAttempt.Invoke();
            return false;
        }
    }
}