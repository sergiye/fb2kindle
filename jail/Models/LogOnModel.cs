using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace jail.Models
{
    public class LogOnModel
    {
        [Required, DisplayName("Email Address")]
        public string UserName { get; set; }

        [DataType(DataType.Password), DisplayName("Password")]
        public string Password { get; set; }

        public string RedirectUrl { get; set; }

//        [DisplayName("Remember me")]
//        public bool RememberMe { get; set; }
    }
}