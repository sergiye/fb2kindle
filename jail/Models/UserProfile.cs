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
        [DataMember, DisplayName("Email"), DataType(DataType.EmailAddress), StringLength(255)]
        public virtual string Email { get; set; }

        [DataMember, DisplayName("User Type")]
        public virtual UserType UserType { get; set; }

        [DapperReadOnly, DisplayName("Registered Time")]
        [DataType(DataType.Date), DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm:ss}", ApplyFormatInEditMode = true)]
        public virtual DateTime RegisteredTime { get; set; }

        [DataMember, DisplayName("Active")]
        public virtual bool Active { get; set; }
    }
}