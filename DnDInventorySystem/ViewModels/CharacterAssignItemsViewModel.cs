using System.Collections.Generic;
using DnDInventorySystem.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace DnDInventorySystem.ViewModels
{
    public class CharacterAssignItemsViewModel
    {
        public int CharacterId { get; set; }

        [ValidateNever]
        public Character Character { get; set; }
        public List<AssignItemRow> Assignments { get; set; } = new();
        public int Page { get; set; }
        public int TotalPages { get; set; }
    }

    public class AssignItemRow
    {
        public int ItemId { get; set; }
        public string ItemName { get; set; }
        public string CategoryName { get; set; }
        public string Description { get; set; }
        public int Quantity { get; set; } = 1;
        public bool IsEquipped { get; set; } = true;
        public bool Selected { get; set; } = false;
    }
}
