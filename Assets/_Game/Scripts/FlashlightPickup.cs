namespace Jamie
{
    public sealed class FlashlightPickup : WorldInteractable
    {
        public override string Prompt => "E - Pick up flashlight";
        public override bool RequiresFlashlight => false;

        public override bool CanInteract(WorldInteractor interactor)
        {
            return !interactor.HasFlashlight;
        }

        public override void Interact(WorldInteractor interactor)
        {
            if (interactor.HasFlashlight)
                return;
            interactor.CollectFlashlight();
            gameObject.SetActive(false);
        }
    }
}
