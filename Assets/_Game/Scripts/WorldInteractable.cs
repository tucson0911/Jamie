using UnityEngine;

namespace Jamie
{
    public abstract class WorldInteractable : MonoBehaviour
    {
        public abstract string Prompt { get; }
        public virtual bool RequiresFlashlight => true;
        public virtual string ScreenMessage => string.Empty;

        public virtual bool CanInteract(WorldInteractor interactor)
        {
            return true;
        }

        public abstract void Interact(WorldInteractor interactor);
    }
}
