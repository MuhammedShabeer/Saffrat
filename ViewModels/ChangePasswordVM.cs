using System.ComponentModel.DataAnnotations;

namespace Saffrat.ViewModels
{
    public class ChangePasswordVM
    {
        [Required(ErrorMessage = "Old Password is required.")]
        public string OldPassword { get; set; }
        [StringLength(30, ErrorMessage = "The password must be at least 8-30 characters long.", MinimumLength = 8)]
        [RegularExpression("^((?=.*?[A-Z])(?=.*?[a-z])(?=.*?[0-9])|(?=.*?[A-Z])(?=.*?[a-z])(?=.*?[^a-zA-Z0-9])|(?=.*?[A-Z])(?=.*?[0-9])(?=.*?[^a-zA-Z0-9])|(?=.*?[a-z])(?=.*?[0-9])(?=.*?[^a-zA-Z0-9])).{8,}$", ErrorMessage = "Passwords must be at least 8 characters and contain at 3 of 4 of the following: upper case (A-Z), lower case (a-z), number (0-9) and special character (e.g. !@#$%^&*)")]
        public string NewPassword { get; set; }
        [Compare("NewPassword", ErrorMessage = "The passwords do not match.")]
        public string ConfirmPassword { get; set; }
    }
}
