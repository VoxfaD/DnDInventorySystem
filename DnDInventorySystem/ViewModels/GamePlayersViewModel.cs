using System.Collections.Generic;
using DnDInventorySystem.Models;

namespace DnDInventorySystem.ViewModels
{
    public class GamePlayersViewModel
    {
        public Game Game { get; set; }
        public IEnumerable<UserGameRole> Players { get; set; }
    }
}
