using System.ComponentModel.DataAnnotations;

namespace DnDInventorySystem.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Please fill in all required fields!")]
        [EmailAddress(ErrorMessage = "Email address does not match the format!")]
        [StringLength(100, MinimumLength = 5, ErrorMessage = "email does not match the specified number of symbols 5-100 symbols!")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please fill in all required fields!")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public string? ReturnUrl { get; set; }
    }
}
