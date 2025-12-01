using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DnDInventorySystem.Models
{
    public class Character
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = "";

        public string Description { get; set; } = "";
        public string PhotoUrl { get; set; } = "";

        public int GameId { get; set; }
        public Game? Game { get; set; }

        public int CreatedByUserId { get; set; }

        [ForeignKey(nameof(CreatedByUserId))]
        public User? CreatedByUser { get; set; }

        public int OwnerUserId { get; set; }

        [ForeignKey(nameof(OwnerUserId))]
        public User? Owner { get; set; }

        public ICollection<ItemCharacter> ItemCharacters { get; set; } = new List<ItemCharacter>();
    }
}
