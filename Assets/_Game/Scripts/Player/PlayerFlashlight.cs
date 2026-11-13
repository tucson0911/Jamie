using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

namespace Jamie
{
    [DisallowMultipleComponent]
    public sealed class PlayerFlashlight : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        [SerializeField] private Light2D flashlight;
        [SerializeField] private bool startWithFlashlight;

        public bool HasFlashlight { get; private set; }

        public bool IsOn =>
            HasFlashlight &&
            flashlight != null &&
            flashlight.isActiveAndEnabled;

        private void Awake()
        {
            if (player == null)
                player = GetComponent<PlayerController>();

            if (player == null || flashlight == null)
            {
                Debug.LogError(
                    "Assign Player and Flashlight on PlayerFlashlight.",
                    this);

                enabled = false;
                return;
            }

            HasFlashlight = startWithFlashlight;
            SetOn(HasFlashlight);
        }

        private void Update()
        {
            if (Time.timeScale <= 0f ||
                !player.isActiveAndEnabled ||
                !HasFlashlight)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;

            if (keyboard != null &&
                keyboard.fKey.wasPressedThisFrame)
            {
                SetOn(!IsOn);
            }
        }

        public void Collect()
        {
            HasFlashlight = true;
            SetOn(true);
        }

        public void SetOn(bool on)
        {
            if (flashlight != null)
                flashlight.enabled = HasFlashlight && on;
        }
    }
}