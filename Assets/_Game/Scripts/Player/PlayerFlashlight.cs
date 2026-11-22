using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using Jamie.Gameplay;

namespace Jamie
{
    [DisallowMultipleComponent]
    public sealed class PlayerFlashlight : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        [SerializeField] private Light2D flashlight;
        [SerializeField] private bool startWithFlashlight;
        [Header("Journal")]
        [SerializeField] private JournalEntry flashlightJournalEntry;

        public bool HasFlashlight { get; private set; }
        public bool IsPowerAvailable { get; private set; } = true;
        private bool inputBlocked;

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

            GameSession session = GameSession.GetOrCreate();

            HasFlashlight = startWithFlashlight || session.HasFlashlight;

            if (HasFlashlight)
            {
                session.CollectFlashlight();
                session.AddJournalEntry(flashlightJournalEntry);
            }

            SetOn(HasFlashlight);
        }

        private void Update()
        {
            if (Time.timeScale <= 0f ||
                !player.isActiveAndEnabled ||
                inputBlocked ||
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
            GameSession session = GameSession.GetOrCreate();

            session.CollectFlashlight();

            HasFlashlight = true;
            SetOn(true);

            session.AddJournalEntry(flashlightJournalEntry);
        }

        public void SetOn(bool on)
        {
            if (flashlight != null)
                flashlight.enabled = HasFlashlight && IsPowerAvailable && on;
        }

        public void SetPowerAvailable(bool available)
        {
            IsPowerAvailable = available;
            if (!available)
                SetOn(false);
        }

        public void SetInputBlocked(bool blocked)
        {
            inputBlocked = blocked;
        }
    }
}
