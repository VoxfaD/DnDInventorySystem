using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DnDInventorySystem.Models
{
    [Table("Kategorija")]
    public class Category
    {
        public int Id { get; set; }

        [Column("Nosaukums")]
        [MaxLength(200)]
        public string Name { get; set; } = "";

        [Column("SpeleID")]
        public int GameId { get; set; }
        public Game? Game { get; set; }

        [Column("LietotajsID")]
        public int CreatedByUserId { get; set; }
        public User CreatedByUser { get; set; }

        public ICollection<Item> Items { get; set; } = new List<Item>();
    }
}
