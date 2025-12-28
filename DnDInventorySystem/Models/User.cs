using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DnDInventorySystem.Models
{
    [Table("Lietotajs")]
    public class User
    {
        public int Id { get; set; }

        [MaxLength(50)]
        [Column("LietotajVards")]
        public string Name { get; set; } = "";

        [MaxLength(100)]
        [Column("Epasts")]
        public string Email { get; set; } = "";

        [MaxLength(128)]
        [Column("Parole")]
        public string PasswordHash { get; set; } = "";

        public ICollection<Game> CreatedGames { get; set; }
    }
}
