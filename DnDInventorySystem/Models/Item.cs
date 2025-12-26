using Microsoft.EntityFrameworkCore.Migrations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DnDInventorySystem.Models
{
    public class Item
    {
        public int Id { get; set; }

        [Required, MaxLength(120)]
        public string Name { get; set; } = "";

        public string Description { get; set; } = "";
        public string? PhotoUrl { get; set; }
        public bool ViewableToPlayers { get; set; } = true;

        public int GameId { get; set; }
        public Game? Game { get; set; }

        public int CreatedByUserId { get; set; }
        public User? CreatedByUser { get; set; }

        public int? CategoryId { get; set; }
        public Category? Category { get; set; }

        public ICollection<ItemCharacter> ItemCharacters { get; set; } = new List<ItemCharacter>();
        public ICollection<HistoryLog> HistoryLogs { get; set; } = new List<HistoryLog>();
    }
}
