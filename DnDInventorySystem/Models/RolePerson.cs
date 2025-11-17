using System.ComponentModel.DataAnnotations.Schema;

namespace DnDInventorySystem.Models
{
    // Links User ↔ Role, optionally within a Game context
    public class RolePerson
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }

        public int RoleId { get; set; }
        public Role Role { get; set; }

        // If roles are global, this can be nullable
        public int? GameId { get; set; }
        public Game Game { get; set; }
    }
}
