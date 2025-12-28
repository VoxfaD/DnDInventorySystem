using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DnDInventorySystem.Models
{
    [Table("Inventars")]
    public class Item
    {
        public int Id { get; set; }

        [Column("Nosaukums")]
        [MaxLength(200)]
        public string Name { get; set; } = "";

        [Column("Apraksts")]
        [MaxLength(2000)]
        public string? Description { get; set; }
        [Column("FotoUrl")]
        [MaxLength(2000)]
        public string? PhotoUrl { get; set; }
        [Column("RedzamsSpeletajiem")]
        public bool ViewableToPlayers { get; set; } = true;

        [Column("SpeleID")]
        public int GameId { get; set; }
        public Game? Game { get; set; }

        [Column("LietotajsID")]
        public int CreatedByUserId { get; set; }
        public User? CreatedByUser { get; set; }

        [Column("KategorijaID")]
        public int? CategoryId { get; set; }
        public Category? Category { get; set; }

        public ICollection<ItemCharacter> ItemCharacters { get; set; } = new List<ItemCharacter>();
        public ICollection<HistoryLog> HistoryLogs { get; set; } = new List<HistoryLog>();
    }
}
