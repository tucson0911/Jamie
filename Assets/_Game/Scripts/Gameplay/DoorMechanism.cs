using UnityEngine;

namespace Jamie.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class DoorMechanism : MonoBehaviour
    {
        [SerializeField] private Transform doorVisual;
        [SerializeField] private Collider2D blockingCollider;
        [SerializeField] private Transform openDoorVisual;

        public bool IsOpen { get; private set; }

        private void Awake()
        {
            if (doorVisual == null || blockingCollider == null ||
                doorVisual == transform ||
                !doorVisual.IsChildOf(transform))
            {
                Debug.LogError(
                    "Assign a child DoorPanel and its blocking collider.",
                    this);
                enabled = false;
                return;
            }

            SetOpen(false);
        }

        public void SetOpen(bool open)
        {
            if (!isActiveAndEnabled ||
                doorVisual == null || blockingCollider == null)
            {
                return;
            }

            IsOpen = open;
            blockingCollider.enabled = !open;
            doorVisual.gameObject.SetActive(!open);
            if (openDoorVisual != null)
            {
                openDoorVisual.gameObject.SetActive(open);
            }
        }
    }
}