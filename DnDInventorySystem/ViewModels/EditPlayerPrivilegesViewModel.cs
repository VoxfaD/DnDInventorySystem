using System.Collections.Generic;
using DnDInventorySystem.Models;

namespace DnDInventorySystem.ViewModels
{
    public class EditPlayerPrivilegesViewModel
    {
        public int GameId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public GamePrivilege Privileges { get; set; }
        public List<GamePrivilege> SelectedPrivileges { get; set; } = new();
    }
}
