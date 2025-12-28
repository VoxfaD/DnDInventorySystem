using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DnDInventorySystem.Models
{
    [Table("VesturesIeraksti")]
    public class HistoryLog
    {
        public int Id { get; set; }

        [Required]
        [Column("SpeleID")]
        public int GameId { get; set; }
        public Game Game { get; set; }

        [Column("InventarsID")]
        public int? ItemId { get; set; }
        public Item Item { get; set; }

        [Column("TelsID")]
        public int? CharacterId { get; set; }
        public Character Character { get; set; }

        [Column("KategorijaID")]
        public int? CategoryId { get; set; }
        public Category Category { get; set; }

        [Column("LietotajsID")]
        public int UserId { get; set; }
        public User User { get; set; }

        [Required, MaxLength(40)]
        [Column("Darbiba")]
        public string Action { get; set; } = ""; // e.g., Created, Edited, Deleted, Transferred

        [Column("Laiks")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // Optional extra info for diffs like "qty 2 → 5" or human-readable message
        [Column("Detalas")]
        [StringLength(2000)]
        public string Details { get; set; } = "";
    }
}
