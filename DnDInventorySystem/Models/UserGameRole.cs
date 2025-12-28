using System.ComponentModel.DataAnnotations.Schema;

namespace DnDInventorySystem.Models
{
    [Table("SpeleLietotajsLoma")]
    public class UserGameRole
    {
        public int Id { get; set; }

        [Column("SpeleID")]
        public int GameId { get; set; }
        public Game Game { get; set; }

        [Column("LietotajsID")]
        public int UserId { get; set; }
        public User User { get; set; }

        [Column("IrVaditajs")]
        public bool IsOwner { get; set; }

        [Column("Privilegijas")]
        public GamePrivilege Privileges { get; set; }
    }
}
