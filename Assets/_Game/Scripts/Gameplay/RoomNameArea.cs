using UnityEngine;

namespace Jamie.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class RoomNameArea : MonoBehaviour
    {
        [SerializeField] private string roomName = "Room";
        [SerializeField] private SpriteRenderer[] floors;
        [SerializeField, Min(0f)] private float boundaryInset = 0.1f;
        [SerializeField] private int priority = 10;

        public string RoomName => roomName;
        public int Priority => priority;

        public bool Contains(Vector2 worldPosition)
        {
            if (!isActiveAndEnabled || floors == null)
                return false;

            foreach (SpriteRenderer floor in floors)
            {
                if (floor == null || !floor.gameObject.activeInHierarchy)
                    continue;

                // Floor geometry defines the room independently of its lighting.
                Vector3 point = floor.transform.InverseTransformPoint(worldPosition);
                Bounds bounds = floor.localBounds;
                Vector3 scale = floor.transform.lossyScale;
                float insetX = boundaryInset / Mathf.Max(Mathf.Abs(scale.x), 0.0001f);
                float insetY = boundaryInset / Mathf.Max(Mathf.Abs(scale.y), 0.0001f);

                if (point.x >= bounds.min.x + insetX &&
                    point.x <= bounds.max.x - insetX &&
                    point.y >= bounds.min.y + insetY &&
                    point.y <= bounds.max.y - insetY)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
