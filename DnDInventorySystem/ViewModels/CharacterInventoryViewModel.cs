using System.Collections.Generic;
using DnDInventorySystem.Models;

namespace DnDInventorySystem.ViewModels
{
    public class CharacterInventoryViewModel
    {
        public Character Character { get; set; }
        public IReadOnlyList<ItemCharacter> Inventory { get; set; } = new List<ItemCharacter>();
    }
}
