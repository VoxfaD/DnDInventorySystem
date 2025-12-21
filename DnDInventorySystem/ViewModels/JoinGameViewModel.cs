using System.ComponentModel.DataAnnotations;

namespace DnDInventorySystem.ViewModels
{
    public class JoinGameViewModel
    {
        [Required]
        [Display(Name = "Join code")]
        [MaxLength(40)]
        public string JoinCode { get; set; } = "";
    }
}
