using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace DnDInventorySystem.Models
{
    [Table("Spele")]
    public class Game
    {
        public int Id { get; set; }

        [Column("Nosaukums")]
        [MaxLength(100)]
        public string Name { get; set; } = "";

        [Column("Apraksts")]
        [MaxLength(2000)]
        public string? Description { get; set; }

        [Column("IzveidesDatums")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("LietotajsID")]
        public int CreatedByUserId { get; set; }
        public User CreatedByUser { get; set; }

        [MaxLength(13)]
        [Column("PievienosanasKods")]
        public string? JoinCode { get; set; }
        [Column("PievienosanasKodsAktivs")]
        public bool JoinCodeActive { get; set; }

        public ICollection<Character> Characters { get; set; } = new List<Character>();
        public ICollection<Category> Categories { get; set; } = new List<Category>();
        public ICollection<Item> Items { get; set; } = new List<Item>();
        public ICollection<UserGameRole> UserGameRoles { get; set; } = new List<UserGameRole>();
        public ICollection<HistoryLog> HistoryLogs { get; set; } = new List<HistoryLog>();
    }
}
