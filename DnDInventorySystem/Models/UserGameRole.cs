namespace DnDInventorySystem.Models
{
    public class UserGameRole
    {
        public int Id { get; set; }

        public int GameId { get; set; }
        public Game Game { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }

        public bool IsOwner { get; set; }

        public GamePrivilege Privileges { get; set; }
    }
}
