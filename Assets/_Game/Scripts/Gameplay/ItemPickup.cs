using UnityEngine;

namespace Jamie.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class ItemPickup : WorldInteractable
    {
        [SerializeField] private ItemId item =
            ItemId.ReceptionKey;

        [SerializeField] private string displayName =
            "reception key";

        [SerializeField] private bool requireFlashlight;

        public override string Prompt =>
            $"E - Pick up {displayName}";

        public override bool RequiresFlashlight =>
            requireFlashlight;

        private void OnEnable()
        {
            if (GameSession.GetOrCreate().HasItem(item))
                gameObject.SetActive(false);
        }

        public override bool CanInteract(WorldInteractor interactor)
        {
            return isActiveAndEnabled && interactor != null &&
                !GameSession.GetOrCreate().HasItem(item);
        }

        public override void Interact(WorldInteractor interactor)
        {
            if (!CanInteract(interactor))
                return;

            ItemInventory inventory =
                interactor.Player.GetComponent<ItemInventory>();

            if (inventory == null)
            {
                Debug.LogError(
                    "Add ItemInventory to the Player.", this);
                return;
            }

            inventory.Add(item);

            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }
}