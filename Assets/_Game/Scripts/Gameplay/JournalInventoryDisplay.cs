using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Jamie.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class JournalInventoryDisplay : MonoBehaviour
    {
        [Header("Entry List")]
        [SerializeField] private ScrollRect entryScroll;
        [SerializeField] private Button entryButtonTemplate;
        [SerializeField] private Text emptyLabel;

        [Header("Entry Reader")]
        [SerializeField] private GameObject readerPanel;
        [SerializeField] private ScrollRect readerScroll;
        [SerializeField] private Text entryTitle;
        [SerializeField] private Text entryBody;
        [SerializeField] private Image entryImage;
        [SerializeField] private Button backButton;

        private readonly List<Button> buttons = new();
        private GameSession session;
        private bool ready;

        private void Awake()
        {
            ready =
                entryScroll != null &&
                entryScroll.content != null &&
                entryButtonTemplate != null &&
                entryButtonTemplate.GetComponentInChildren<Text>(true)
                    != null &&
                emptyLabel != null &&
                readerPanel != null &&
                readerScroll != null &&
                readerScroll.content != null &&
                entryTitle != null &&
                entryBody != null &&
                entryImage != null &&
                backButton != null;

            if (!ready)
            {
                Debug.LogError(
                    "Assign all JournalInventoryDisplay references.",
                    this);

                enabled = false;
                return;
            }

            entryButtonTemplate.gameObject.SetActive(false);
            emptyLabel.text = "Nothing collected yet.";
            backButton.onClick.AddListener(ShowList);
        }

        private void OnEnable()
        {
            if (!ready)
                return;

            session = GameSession.GetOrCreate();
            session.JournalChanged += Refresh;

            Refresh();
        }

        private void Refresh()
        {
            foreach (Button button in buttons)
            {
                if (button == null)
                    continue;

                button.gameObject.SetActive(false);
                Destroy(button.gameObject);
            }

            buttons.Clear();

            foreach (JournalEntry entry in session.JournalEntries)
            {
                if (entry == null)
                    continue;

                Button button = Instantiate(
                    entryButtonTemplate, entryScroll.content);

                Text label =
                    button.GetComponentInChildren<Text>(true);

                label.text = entry.Title;

                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => ShowEntry(entry));
                button.gameObject.SetActive(true);

                buttons.Add(button);
            }

            ShowList();

            Canvas.ForceUpdateCanvases();
            entryScroll.verticalNormalizedPosition = 1f;
        }

        private void ShowList()
        {
            bool hasEntries = buttons.Count > 0;

            readerPanel.SetActive(false);
            entryScroll.gameObject.SetActive(hasEntries);
            emptyLabel.gameObject.SetActive(!hasEntries);
        }

        private void ShowEntry(JournalEntry entry)
        {
            if (entry == null)
                return;

            entryTitle.text = entry.Title;
            entryBody.text = entry.Body;

            entryImage.sprite = entry.Image;
            entryImage.preserveAspect = true;
            entryImage.gameObject.SetActive(entry.Image != null);

            entryScroll.gameObject.SetActive(false);
            emptyLabel.gameObject.SetActive(false);
            readerPanel.SetActive(true);

            Canvas.ForceUpdateCanvases();
            readerScroll.verticalNormalizedPosition = 1f;
        }

        private void OnDisable()
        {
            if (session != null)
                session.JournalChanged -= Refresh;
        }

        private void OnDestroy()
        {
            if (backButton != null)
                backButton.onClick.RemoveListener(ShowList);
        }
    }
}