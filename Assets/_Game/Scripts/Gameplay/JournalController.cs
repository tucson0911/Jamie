using UnityEngine;
using UnityEngine.InputSystem;

namespace Jamie.Gameplay
{
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class JournalController : MonoBehaviour
    {
        [SerializeField] private CanvasGroup journalPanel;

        private float previousTimeScale = 1f;

        public bool IsOpen { get; private set; }

        private void Awake()
        {
            if (journalPanel == null)
            {
                Debug.LogError(
                    "Assign Journal Panel on JournalController.", this);

                enabled = false;
                return;
            }

            SetVisible(false);
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;

            if (keyboard == null ||
                !keyboard.tabKey.wasPressedThisFrame)
            {
                return;
            }

            if (IsOpen)
                CloseJournal();
            else
                OpenJournal();
        }

        public void OpenJournal()
        {
            // Notes, death transitions and other paused screens
            // already set the time scale to zero.
            if (!isActiveAndEnabled ||
                IsOpen ||
                journalPanel == null ||
                Time.timeScale <= 0f)
            {
                return;
            }

            previousTimeScale = Time.timeScale;
            IsOpen = true;

            SetVisible(true);
            Time.timeScale = 0f;
        }

        public void CloseJournal()
        {
            if (!IsOpen)
                return;

            IsOpen = false;
            SetVisible(false);

            Time.timeScale = previousTimeScale;
        }

        private void SetVisible(bool visible)
        {
            if (journalPanel == null)
                return;

            journalPanel.alpha = visible ? 1f : 0f;
            journalPanel.interactable = visible;
            journalPanel.blocksRaycasts = visible;
        }

        private void OnDisable()
        {
            CloseJournal();
        }
    }
}