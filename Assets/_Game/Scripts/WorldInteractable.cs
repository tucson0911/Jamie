using UnityEngine;

namespace Jamie
{
    public abstract class WorldInteractable : MonoBehaviour
    {
        public abstract string Prompt { get; }
        public virtual bool RequiresFlashlight => true;
        public virtual string ScreenMessage => string.Empty;
        public virtual bool HighlightWithoutFlashlight => false;

        public virtual Vector2 GetInteractionPoint(Vector2 origin)
        {
            return transform.position;
        }

        public virtual bool CanInteract(WorldInteractor interactor)
        {
            return true;
        }

        public abstract void Interact(WorldInteractor interactor);
    }
}
