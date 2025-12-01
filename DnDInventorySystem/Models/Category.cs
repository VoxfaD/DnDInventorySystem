using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DnDInventorySystem.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required, MaxLength(80)]
        public string Name { get; set; } = "";

        public int GameId { get; set; }
        public Game? Game { get; set; }

        public int? CreatedByUserId { get; set; }
        public User? CreatedByUser { get; set; }

        public ICollection<Item> Items { get; set; } = new List<Item>();
    }
}
