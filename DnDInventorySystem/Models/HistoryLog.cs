using System;
using System.ComponentModel.DataAnnotations;

namespace DnDInventorySystem.Models
{
    public class HistoryLog
    {
        public int Id { get; set; }

        public int ItemId { get; set; }
        public Item Item { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }

        // Nullable: not every action targets a specific character
        public int? CharacterId { get; set; }
        public Character Character { get; set; }

        [Required, MaxLength(40)]
        public string Action { get; set; } = ""; // e.g., Created, Edited, Deleted, Transferred

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // Optional extra info for diffs like "qty 2 → 5"
        public string Details { get; set; } = "";
    }
}
