using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

namespace Jamie
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float walkSpeed = 3f;
        [SerializeField, Min(0.1f)] private float sprintSpeed = 4.8f;
        [SerializeField] private Camera viewCamera;
        [SerializeField] private Transform facing;
        [SerializeField] private Light2D flashlight;

        private Rigidbody2D body;
        private Vector2 movement;
        private Vector2 spawn;
        private bool sprinting;
        private int oxygenMinutes;
        private int oxygenSeconds

        public bool FlashlightEnabled => flashlight != null && flashlight.enabled;
        public Vector2 FacingDirection => facing != null ? (Vector2)facing.up : Vector2.up;

        public void Configure(Camera camera, Transform visual, Light2D light)
        {
            viewCamera = camera;
            facing = visual;
            flashlight = light;
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            spawn = body.position;
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            movement = Vector2.zero;
            sprinting = false;
            if (keyboard != null)
            {
                movement.x = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1 : 0)
                    - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1 : 0);
                movement.y = (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1 : 0)
                    - (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1 : 0);
                movement = Vector2.ClampMagnitude(movement, 1f);
                sprinting = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;

                if (keyboard.fKey.wasPressedThisFrame && flashlight != null)
                    flashlight.enabled = !flashlight.enabled;
                if (keyboard.rKey.wasPressedThisFrame)
                    ResetToSpawn();
            }

            if (Mouse.current != null && viewCamera != null && facing != null)
            {
                Vector2 target = viewCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
                Vector2 direction = target - (Vector2)transform.position;
                if (direction.sqrMagnitude > 0.01f)
                    facing.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f);
            }
        }

        private void Start() {
            Debug.unityLogger.logEnabled = true;
            Debug.log("PLEAAAAASSE");
            InvokeRepeating("DecrementOxygen", 1.0f, 1.0f);print
        }

        private void DecrementOxygen() {
            Debug.log(oxygenMinutes + ":" + oxygenSeconds);
            if (oxygenSeconds > 0) {
                oxygenSeconds--;
            } else {
                oxygenMinutes--;
                oxygenSeconds = 60; // Reset seconds counter to a minute
            }
        }

        private void FixedUpdate()
        {
            body.linearVelocity = movement * (sprinting ? sprintSpeed : walkSpeed);
        }

        private void OnDisable()
        {
            movement = Vector2.zero;
            if (body != null)
                body.linearVelocity = Vector2.zero;
        }

        public void ResetToSpawn()
        {
            body.position = spawn;
            body.linearVelocity = Vector2.zero;
            movement = Vector2.zero;
        }
    }
}
