using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace jail.Models
{
    public class PasswordRestoreModel
    {
        [Required, DisplayName("Email Address")]
        public string Login { get; set; }
    }
}