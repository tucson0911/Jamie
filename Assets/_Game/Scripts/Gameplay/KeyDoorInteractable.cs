using UnityEngine;

namespace Jamie.Gameplay
{
    [RequireComponent(typeof(DoorMechanism))]
    [DisallowMultipleComponent]
    public sealed class KeyDoorInteractable : WorldInteractable
    {
        [SerializeField] private string doorName = "reception door";
        [SerializeField] private bool needsKey = true;

        [SerializeField] private ItemId requiredItem =
            ItemId.ReceptionKey;

        [SerializeField] private bool requireFlashlight;

        private DoorMechanism door;
        private PhaseDoorLock phaseLock;
        private bool unlocked;

        public override bool RequiresFlashlight =>
            requireFlashlight;
        public override bool HighlightWithoutFlashlight =>
            !RequiresFlashlight;    

        public override string Prompt
        {
            get
            {
                if (phaseLock != null && phaseLock.IsLocked)
                {
                    return $"E - Examine {doorName} " +
                        $"(marked {phaseLock.DoorNumber})";
                }
                if (needsKey && !unlocked)
                    return $"E - Unlock {doorName} (key needed)";

                string action = door.IsOpen ? "Close" : "Open";
                return $"E - {action} {doorName}";
            }
        }

        private void Awake()
        {
            door = GetComponent<DoorMechanism>();
            phaseLock = GetComponent<PhaseDoorLock>();
        }

        public override bool CanInteract(WorldInteractor interactor)
        {
            return door != null && door.isActiveAndEnabled;
        }

        public override void Interact(WorldInteractor interactor)
        {
            if (!CanInteract(interactor))
                return;

            if (phaseLock != null && !phaseLock.TryAccess())
                return;    

            if (needsKey && !unlocked)
            {
                ItemInventory inventory =
                    interactor.Player.GetComponent<ItemInventory>();

                if (inventory == null || !inventory.Has(requiredItem))
                {
                    Debug.Log(
                        $"[Door] The {doorName} is locked. Find its key.",
                        this);
                    return;
                }

                unlocked = true;
            }

            door.SetOpen(!door.IsOpen);
        }
    }
}