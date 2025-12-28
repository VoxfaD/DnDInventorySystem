using System.ComponentModel.DataAnnotations;

namespace DnDInventorySystem.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Please fill in all required fields!")]
        [StringLength(50, MinimumLength = 1, ErrorMessage = "user does not match the specified number of symbols 1-50 symbols!")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please fill in all required fields!")]
        [EmailAddress(ErrorMessage = "Email address does not match the format!")]
        [StringLength(100, MinimumLength = 5, ErrorMessage = "email does not match the specified number of symbols 5-100 symbols!")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please fill in all required fields!")]
        [DataType(DataType.Password)]
        [StringLength(128, MinimumLength = 8, ErrorMessage = "Password does not meet security requirements! Must contain a number and a unique symbol, such as #, must be at least an uppercase letter and the number of symbols is 8-128 symbols.")]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{8,128}$", ErrorMessage = "Password does not meet security requirements! Must contain a number and a unique symbol, such as #, must be at least an uppercase letter and the number of symbols is 8-128 symbols.")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please fill in all required fields!")]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "Password fields do not match!")]
        public string ConfirmPassword { get; set; } = string.Empty;

        public string? ReturnUrl { get; set; }
    }
}
