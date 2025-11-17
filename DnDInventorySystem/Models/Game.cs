using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DnDInventorySystem.Models
{
    public class Game
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = "";

        public string Description { get; set; } = "";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int CreatedByUserId { get; set; }
        public User CreatedByUser { get; set; }

        public ICollection<Character> Characters { get; set; } = new List<Character>();
        public ICollection<Category> Categories { get; set; } = new List<Category>();
        public ICollection<Item> Items { get; set; } = new List<Item>();
        public ICollection<RolePerson> RolePersons { get; set; } = new List<RolePerson>();
    }
}
