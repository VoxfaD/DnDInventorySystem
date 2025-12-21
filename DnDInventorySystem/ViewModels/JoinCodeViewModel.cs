using System.ComponentModel.DataAnnotations;

namespace DnDInventorySystem.ViewModels
{
    public class JoinCodeViewModel
    {
        public int GameId { get; set; }

        [Display(Name = "Game")]
        public string GameName { get; set; } = "";

        [Display(Name = "Join code")]
        public string? JoinCode { get; set; }

        public bool JoinCodeActive { get; set; }

        public bool HasJoinCode => !string.IsNullOrWhiteSpace(JoinCode);
    }
}
