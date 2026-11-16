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
        public bool Has(ItemId item)
        {
            return GameSession.GetOrCreate().HasItem(item);
        }

        public void Add(ItemId item)
        {
            if (GameSession.GetOrCreate().AddItem(item))
            {
                Debug.Log(
                    $"[Inventory] Collected {item}", this);
            }
        }
    }
}