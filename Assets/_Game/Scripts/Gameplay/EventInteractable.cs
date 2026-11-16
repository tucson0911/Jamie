using UnityEngine;
using UnityEngine.Events;

namespace Jamie.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class EventInteractable : WorldInteractable
    {
        [SerializeField] private string prompt = "E - Interact";
        [SerializeField] private bool requireFlashlight;
        [SerializeField] private bool consumeOnInteract;

        [SerializeField]
        private UnityEvent onInteracted = new UnityEvent();

        private bool used;

        public override string Prompt => prompt;

        public override bool RequiresFlashlight =>
            requireFlashlight;

        public override bool CanInteract(WorldInteractor interactor)
        {
            return !used;
        }

        public override void Interact(WorldInteractor interactor)
        {
            if (used)
                return;

            if (consumeOnInteract)
                used = true;

            onInteracted.Invoke();

            if (consumeOnInteract)
            {
                gameObject.SetActive(false);
                Destroy(gameObject);
            }
        }
    }
}