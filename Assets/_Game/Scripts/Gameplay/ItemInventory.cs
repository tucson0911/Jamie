using UnityEngine;

namespace Jamie.Gameplay
{
    public enum ItemId
    {
        ReceptionKey = 1
    }

    [DisallowMultipleComponent]
    public sealed class ItemInventory : MonoBehaviour
    {
        [SerializeField]
        private JournalEntry receptionKeyJournalEntry;

        private void Start()
        {
            GameSession session = GameSession.GetOrCreate();

            if (session.HasItem(ItemId.ReceptionKey))
                session.AddJournalEntry(receptionKeyJournalEntry);
        }

        public bool Has(ItemId item)
        {
            return GameSession.GetOrCreate().HasItem(item);
        }

        public void Add(ItemId item)
        {
            GameSession session = GameSession.GetOrCreate();

            if (session.AddItem(item))
            {
                Debug.Log(
                    $"[Inventory] Collected {item}", this);
            }

            if (item == ItemId.ReceptionKey)
                session.AddJournalEntry(receptionKeyJournalEntry);
        }
    }
}