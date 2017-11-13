using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace jail.Models
{
    public class ChangePasswordModel
    {
        [Required]
        public long Id { get; set; }

        [Required]
        public string Username { get; set; }

        [Required]
        public bool HasPassword { get; set; }

        [DataType(DataType.Password), DisplayName("Current password")]
        public string OldPassword { get; set; }

        [Required, StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 3)]
        [DataType(DataType.Password), DisplayName("New password")]
        public string NewPassword { get; set; }

        [Required, DataType(DataType.Password), DisplayName("Confirm new password")]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }
    }
}