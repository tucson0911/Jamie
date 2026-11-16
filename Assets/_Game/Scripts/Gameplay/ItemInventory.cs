using System.Collections.Generic;
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
        private readonly HashSet<ItemId> items =
            new HashSet<ItemId>();

        public bool Has(ItemId item)
        {
            return items.Contains(item);
        }

        public void Add(ItemId item)
        {
            if (items.Add(item))
            {
                Debug.Log(
                    $"[Inventory] Collected {item}", this);
            }
        }
    }
}