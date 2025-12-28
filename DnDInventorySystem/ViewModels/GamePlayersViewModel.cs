using System.Collections.Generic;
using DnDInventorySystem.Models;

namespace DnDInventorySystem.ViewModels
{
    public class GamePlayersViewModel
    {
        public Game Game { get; set; } = null!;
        public IEnumerable<UserGameRole> Players { get; set; } = new List<UserGameRole>();
    }
}
