using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Runtime.Serialization;
using Simpl.Extensions.Database;

namespace jail.Models
{
    public enum UserType
    {
        User = 0,
        Administrator = 1
    }

    [Serializable]
    [DebuggerDisplay("Id={Id}; {Email}")]
    [DapperTable("Users")]
    public class UserProfile : LongIdContainer
    {
        [DataMember, DisplayName("Email"), DataType(DataType.EmailAddress), StringLength(255), Required]
        public virtual string Email { get; set; }

        [DapperReadOnly, DataType(DataType.Password)]
        public virtual string Password { get; set; }

        [DapperIgnore]
        public virtual bool HasPassword { get { return !string.IsNullOrEmpty(Password); } }

        [DataMember, DisplayName("User Type")]
        public virtual UserType UserType { get; set; }

        [DataMember]
        public int TimeTrackId { get; set; }

        [DapperReadOnly, DisplayName("Registered Time")]
        [DataType(DataType.Date), DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm:ss}", ApplyFormatInEditMode = true)]
        public virtual DateTime RegisteredTime { get; set; }

        [DataMember, DisplayName("Active")]
        public virtual bool Active { get; set; }

        [DapperIgnore, DisplayName("Active")]
        public string ActiveText { get { return Active ? "Yes" : "No"; } }
    }
}