using System.Collections.Generic;
using DnDInventorySystem.Models;

namespace DnDInventorySystem.ViewModels
{
    public class HistorySidebarViewModel
    {
        public int GameId { get; set; }
        public List<HistoryLog> Logs { get; set; } = new();
    }
}
