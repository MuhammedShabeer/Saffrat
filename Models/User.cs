using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Saffrat.Models
{
    public partial class User
    {
        [Key]
        public int? Id { get; set; }
        [Required]
        [RegularExpression(@"^\S*$", ErrorMessage = "Enter User Name without white space.")]
        public string UserName { get; set; }
        [Required]
        public string FullName { get; set; }
        [Required]
        [EmailAddress(ErrorMessage = "Enter valid email address.")]
        [StringLength(150, ErrorMessage = "Email maximum length is 150 characters.", MinimumLength = 1)]
        public string Email { get; set; }
        [StringLength(30, ErrorMessage = "The password must be at least 8-30 characters long.", MinimumLength = 8)]
        [RegularExpression("^((?=.*?[A-Z])(?=.*?[a-z])(?=.*?[0-9])|(?=.*?[A-Z])(?=.*?[a-z])(?=.*?[^a-zA-Z0-9])|(?=.*?[A-Z])(?=.*?[0-9])(?=.*?[^a-zA-Z0-9])|(?=.*?[a-z])(?=.*?[0-9])(?=.*?[^a-zA-Z0-9])).{8,}$", ErrorMessage = "Password must be at least 8 characters and contain at 3 of 4 of the following: upper case (A-Z), lower case (a-z), number (0-9) and special character (e.g. !@#$%^&*)")]
        [JsonIgnore]
        public string Password { get; set; }
        [Required]
        public string Role { get; set; }
        [JsonIgnore]
        public DateTime? LastLogin { get; set; }
        [Required]
        public int Status { get; set; }
        [JsonIgnore]
        public string UpdatedBy { get; set; }
        [JsonIgnore]
        public DateTime? UpdatedAt { get; set; }
        public string PermittedPriceTypes { get; set; }
        public string PermittedOrderTypes { get; set; }
        public int? VanCashAccountId { get; set; }
        public bool IsVanSales { get; set; }
    }
}
