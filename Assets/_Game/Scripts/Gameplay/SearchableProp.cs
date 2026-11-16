using UnityEngine;

namespace Jamie.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class SearchableProp : WorldInteractable
    {
        public enum SearchReward { ReceptionKey, Flashlight }

        [SerializeField] private string prompt = "E - Look inside sink";
        [SerializeField] private SearchReward reward;
        [SerializeField] private bool requireFlashlight = true;
        [SerializeField] private Collider2D searchBounds;

        public bool Searched { get; private set; }
        public override string Prompt => prompt;
        public override bool RequiresFlashlight => requireFlashlight;
        public override bool HighlightWithoutFlashlight =>
            reward == SearchReward.Flashlight;

        public override Vector2 GetInteractionPoint(Vector2 origin)
        {
            return searchBounds != null ? searchBounds.ClosestPoint(origin) :
                base.GetInteractionPoint(origin);
        }

        public override bool CanInteract(WorldInteractor interactor)
        {
            if (Searched || interactor == null ||
                (requireFlashlight && !interactor.FlashlightOn))
                return false;

            if (reward == SearchReward.Flashlight)
                return !interactor.HasFlashlight;

            ItemInventory inventory = interactor.Player.GetComponent<ItemInventory>();
            return inventory != null && !inventory.Has(ItemId.ReceptionKey);
        }

        public override void Interact(WorldInteractor interactor)
        {
            if (Time.timeScale <= 0f || !CanInteract(interactor))
                return;

            if (reward == SearchReward.Flashlight)
                interactor.CollectFlashlight();
            else
                interactor.Player.GetComponent<ItemInventory>().Add(ItemId.ReceptionKey);

            // The furniture remains in place; its prompt and highlight stop.
            Searched = true;
        }
    }
}
