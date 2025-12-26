using System.ComponentModel.DataAnnotations;

namespace DnDInventorySystem.ViewModels
{
    public class UpdateProfileViewModel
    {
        [Required]
        [Display(Name = "Display name")]
        [MaxLength(100)]
        public string DisplayName { get; set; } = "";

        [EmailAddress]
        public string Email { get; set; } = "";

        [DataType(DataType.Password)]
        [Display(Name = "Current password")]
        public string? CurrentPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        [MinLength(6)]
        public string? NewPassword { get; set; }
    }
}
