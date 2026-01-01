using System.ComponentModel.DataAnnotations;

namespace DnDInventorySystem.ViewModels
{
    public class UpdateProfileViewModel
    {
        [Required(ErrorMessage = "Display name is required!")]
        [Display(Name = "Display name")]
        [StringLength(50, MinimumLength = 1, ErrorMessage = "user does not match the specified number of symbols 1-50 symbols!")]
        public string DisplayName { get; set; } = "";

        [Required(ErrorMessage = "E-mail is required!")]
        [EmailAddress(ErrorMessage = "Email address does not match the format!")]
        [StringLength(100, MinimumLength = 5, ErrorMessage = "email does not match the specified number of symbols 5-100 symbols!")]
        public string Email { get; set; } = "";

        [DataType(DataType.Password)]
        [Display(Name = "Current password")]
        public string? CurrentPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        public string? NewPassword { get; set; }
    }
}
