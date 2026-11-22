using UnityEngine;

namespace Jamie.Gameplay
{
    public enum JournalEntryCategory
    {
        StoryItem,
        KeyItem
    }

    [CreateAssetMenu(
        fileName = "JournalEntry",
        menuName = "Jamie/Journal Entry")]
    public sealed class JournalEntry : ScriptableObject
    {
        [SerializeField] private string title = "New entry";

        [SerializeField, TextArea(5, 20)]
        private string body;

        [SerializeField] private Sprite image;
        [SerializeField] private JournalEntryCategory category;

        [SerializeField, Min(0)]
        private int sortOrder = 100;

        public string Title => title;
        public string Body => body;
        public Sprite Image => image;
        public JournalEntryCategory Category => category;
        public int SortOrder => sortOrder;
    }
}