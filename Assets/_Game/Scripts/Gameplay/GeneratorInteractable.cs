using UnityEngine;
using UnityEngine.Events;

namespace Jamie.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class GeneratorInteractable : WorldInteractable
    {
        [SerializeField] private PhaseController phases;
        [SerializeField] private MonsterPeekEncounter nurseDoorPeek;
        
        [Header("Trigger Settings")]
        [Tooltip("The trigger zone GameObject to enable once the generator is started.")]
        [SerializeField] private GameObject triggerZoneToEnable;

        [SerializeField]
        private UnityEvent onStarted = new UnityEvent();

        private bool ready;

        public bool IsPowered { get; private set; }

        public override string Prompt => "E - Start generator";

        public override bool RequiresFlashlight => false;

        private void Awake()
        {
            if (phases == null)
                phases = FindFirstObjectByType<PhaseController>();

            if (phases == null || nurseDoorPeek == null)
            {
                Debug.LogError(
                    "Assign Phases and Nurse Door Peek " +
                    "on GeneratorInteractable.", this);

                enabled = false;
                return;
            }

            ready = true;
        }

        public override bool CanInteract(WorldInteractor interactor)
        {
            return ready &&
                isActiveAndEnabled &&
                Time.timeScale > 0f &&
                !IsPowered &&
                phases.CurrentPhase == HospitalPhase.First &&
                nurseDoorPeek.HasPlayed &&
                !nurseDoorPeek.IsPlaying;
        }

        public override void Interact(WorldInteractor interactor)
        {
            if (!CanInteract(interactor))
                return;

            IsPowered = true;

            Debug.Log("[Generator] Started.", this);

            // Enable the trigger zone directly via code
            if (triggerZoneToEnable != null)
            {
                triggerZoneToEnable.SetActive(true);
            }

            onStarted.Invoke();
        }
    }
}