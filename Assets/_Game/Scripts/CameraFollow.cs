using UnityEngine;

namespace Jamie
{
    [RequireComponent(typeof(Camera))]
    public sealed class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField, Min(0.01f)] private float smoothTime = 0.14f;
        [SerializeField] private Vector2 minimum = new Vector2(-15.3f, -10.3f);
        [SerializeField] private Vector2 maximum = new Vector2(15.3f, 10.3f);

        private Camera viewCamera;
        private Vector3 velocity;

        public void Configure(Transform followTarget)
        {
            target = followTarget;
        }

        private void Awake()
        {
            viewCamera = GetComponent<Camera>();
            if (target != null)
                transform.position = BoundedPosition(target.position);
        }

        private void LateUpdate()
        {
            if (target == null)
                return;
            transform.position = Vector3.SmoothDamp(transform.position, BoundedPosition(target.position),
                ref velocity, smoothTime);
        }

        private Vector3 BoundedPosition(Vector3 position)
        {
            float halfHeight = viewCamera.orthographicSize;
            float halfWidth = halfHeight * viewCamera.aspect;
            float x = maximum.x - minimum.x <= halfWidth * 2f
                ? (minimum.x + maximum.x) * 0.5f
                : Mathf.Clamp(position.x, minimum.x + halfWidth, maximum.x - halfWidth);
            float y = maximum.y - minimum.y <= halfHeight * 2f
                ? (minimum.y + maximum.y) * 0.5f
                : Mathf.Clamp(position.y, minimum.y + halfHeight, maximum.y - halfHeight);
            return new Vector3(x, y, -10f);
        }
    }
}
