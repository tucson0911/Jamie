using System.Collections.Generic;
using UnityEngine;
using System;

namespace Jamie.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class GameSession : MonoBehaviour
    {
        private static GameSession instance;

        private readonly HashSet<ItemId> items = new();

        private readonly List<JournalEntry> journalEntries = new();

        public IReadOnlyList<JournalEntry> JournalEntries => journalEntries;

        private readonly HashSet<JournalEntry> collectedProps = new();

        public event Action JournalChanged;

        public bool HasFlashlight { get; private set; }

        public static GameSession GetOrCreate()
        {
            if (instance == null)
            {
                var sessionObject = new GameObject("GameSession");
                sessionObject.AddComponent<GameSession>();
            }

            return instance;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        public bool AddJournalEntry(JournalEntry entry)
        {
            if (entry == null || journalEntries.Contains(entry))
                return false;

            journalEntries.Add(entry);

            journalEntries.Sort((a, b) =>
            {
                int order = a.SortOrder.CompareTo(b.SortOrder);

                return order != 0
                    ? order
                    : string.Compare(
                        a.Title, b.Title,
                        StringComparison.OrdinalIgnoreCase);
            });

            Debug.Log($"[Journal] Collected: {entry.Title}", this);

            JournalChanged?.Invoke();
            return true;
        }

        public bool HasCollectedProp(JournalEntry entry)
        {
            return entry != null && collectedProps.Contains(entry);
        }

        public bool MarkPropCollected(JournalEntry entry)
        {
            if (entry == null || !collectedProps.Add(entry))
                return false;

            Debug.Log($"[Collection] Picked up: {entry.Title}", this);
            return true;
        }

        public bool HasItem(ItemId item)
        {
            return items.Contains(item);
        }

        public bool AddItem(ItemId item)
        {
            return items.Add(item);
        }

        public void CollectFlashlight()
        {
            HasFlashlight = true;
        }

        private string pendingScene;
        private float resumeTimeScale = 1f;

        public void PrepareSceneChange(string sceneName, float previousTimeScale)
        {
            pendingScene = sceneName;
            resumeTimeScale = previousTimeScale;
        }

        public bool TryTakeSceneArrival(string sceneName, out float previousTimeScale)
        {
            previousTimeScale = resumeTimeScale;

            if (pendingScene != sceneName)
                return false;

            pendingScene = null;
            return true;
        }

        [ContextMenu("Log Session State")]
        private void LogSessionState()
        {
            Debug.Log(
                $"[Session] Flashlight: {HasFlashlight}; " +
                $"items: {string.Join(", ", items)}",
                this);
        }
    }
}