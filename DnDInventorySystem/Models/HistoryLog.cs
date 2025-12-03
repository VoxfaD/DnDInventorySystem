using System;
using System.ComponentModel.DataAnnotations;

namespace DnDInventorySystem.Models
{
    public class HistoryLog
    {
        public int Id { get; set; }

        [Required]
        public int GameId { get; set; }
        public Game Game { get; set; }

        public int? ItemId { get; set; }
        public Item Item { get; set; }

        public int? CharacterId { get; set; }
        public Character Character { get; set; }

        public int? CategoryId { get; set; }
        public Category Category { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }

        [Required, MaxLength(40)]
        public string Action { get; set; } = ""; // e.g., Created, Edited, Deleted, Transferred

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // Optional extra info for diffs like "qty 2 → 5" or human-readable message
        public string Details { get; set; } = "";
    }
}
