using System.Collections.Generic;
using DnDInventorySystem.Models;

namespace DnDInventorySystem.ViewModels
{
    public class CharacterInventoryViewModel
    {
        public Character Character { get; set; }
        public IReadOnlyList<ItemCharacter> Inventory { get; set; } = new List<ItemCharacter>();
        public List<InventoryUpdateRow> Updates { get; set; } = new();
    }

    public class InventoryUpdateRow
    {
        public int EntryId { get; set; }
        public int Quantity { get; set; }
        public bool IsEquipped { get; set; }
    }
}
