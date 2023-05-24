using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace jail.Models {
    public enum UserType {
        User = 0,
        Administrator = 1
    }

    [Serializable]
    [DebuggerDisplay("Id={Id}; {Email}")]
    public class UserProfile {
        public long Id { get; set; }

        [DataMember, DisplayName("Email"), DataType(DataType.EmailAddress), StringLength(255), Required]
        public string Email { get; set; }

        [DataType(DataType.Password)] public string Password { get; set; }

        public bool HasPassword => !string.IsNullOrEmpty(Password);

        [DataMember, DisplayName("User Type")] public UserType UserType { get; set; }

        [DataMember] public int FlibustaId { get; set; }

        [DisplayName("Registered Time")]
        [DataType(DataType.Date),
         DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm:ss}", ApplyFormatInEditMode = true)]
        public DateTime RegisteredTime { get; set; }

        [DataMember, DisplayName("Active")] public bool Active { get; set; }

        [DisplayName("Active")]
        public string ActiveText => Active ? "Yes" : "No";

        public void MergeWith(UserProfile source) {
            Email = source.Email;
            Password = source.Password;
            UserType = source.UserType;
            FlibustaId = source.FlibustaId;
            Active = source.Active;
        }
    }
}