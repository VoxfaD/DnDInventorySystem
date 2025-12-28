using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DnDInventorySystem.Models
{
    [Table("Tels")]
    public class Character
    {
        public int Id { get; set; }

        [MaxLength(100)]
        [Column("Vards")]
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

        [ForeignKey(nameof(CreatedByUserId))]
        public User? CreatedByUser { get; set; }

        [Column("PiederLietotajamID")]
        public int? OwnerUserId { get; set; }

        [ForeignKey(nameof(OwnerUserId))]
        public User? Owner { get; set; }

        public ICollection<ItemCharacter> ItemCharacters { get; set; } = new List<ItemCharacter>();
    }
}
