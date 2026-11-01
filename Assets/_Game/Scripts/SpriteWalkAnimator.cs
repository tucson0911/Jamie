using UnityEngine;

namespace Jamie
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class SpriteWalkAnimator : MonoBehaviour
    {
        [SerializeField] private Rigidbody2D body = null;
        [SerializeField] private Sprite[] walkFrames = new Sprite[0];
        [SerializeField, Min(0.1f)] private float framesPerSecond = 8f;
        [SerializeField, Min(0.1f)] private float referenceMoveSpeed = 3f;

        private SpriteRenderer spriteRenderer;
        private float frameProgress;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();

            if (body == null)
                body = GetComponentInParent<Rigidbody2D>();

            if (body == null || walkFrames == null || walkFrames.Length == 0)
            {
                Debug.LogError(
                    "Assign Body and Walk Frames on SpriteWalkAnimator.", this);
                enabled = false;
                return;
            }

            foreach (Sprite frame in walkFrames)
            {
                if (frame == null)
                {
                    Debug.LogError(
                        "Fill every Walk Frames slot on SpriteWalkAnimator.", this);
                    enabled = false;
                    return;
                }
            }

            ShowIdle();
        }

        private void LateUpdate()
        {
            if (body == null || walkFrames == null || walkFrames.Length == 0)
                return;

            float speed = body.linearVelocity.magnitude;

            if (speed < 0.05f)
            {
                ShowIdle();
                return;
            }

            if (Time.timeScale <= 0f)
                return;

            float speedMultiplier = Mathf.Clamp(
                speed / Mathf.Max(referenceMoveSpeed, 0.01f), 0.5f, 2f);

            frameProgress = Mathf.Repeat(
                frameProgress +
                Time.deltaTime * framesPerSecond * speedMultiplier,
                walkFrames.Length);

            int frameIndex = Mathf.FloorToInt(frameProgress);
            spriteRenderer.sprite = walkFrames[frameIndex];
        }

        private void ShowIdle()
        {
            frameProgress = 0f;

            if (spriteRenderer != null &&
                walkFrames != null && walkFrames.Length > 0 &&
                walkFrames[0] != null)
            {
                spriteRenderer.sprite = walkFrames[0];
            }
        }

        private void OnDisable()
        {
            ShowIdle();
        }
    }
}
