using System.Collections.Generic;
using DnDInventorySystem.Models;

namespace DnDInventorySystem.ViewModels
{
    public class GameDetailsViewModel
    {
        public Game Game { get; set; }
        public IReadOnlyList<Character> Characters { get; set; } = new List<Character>();
        public IReadOnlyList<Item> Items { get; set; } = new List<Item>();
        public IReadOnlyList<Category> Categories { get; set; } = new List<Category>();
        public int CharacterCount { get; set; }
        public int ItemCount { get; set; }
        public int CategoryCount { get; set; }
    }
}
