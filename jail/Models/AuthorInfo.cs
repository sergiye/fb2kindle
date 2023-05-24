using System.Collections.Generic;

namespace jail.Models {
    public class AuthorInfo : LongIdContainer {
        public string FullName { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }

        public string DisplayName => string.Join(" ", FirstName, MiddleName, LastName).Trim();

        public override string ToString() {
            return FullName.Trim();
        }
    }

    public class AuthorData : AuthorInfo {
        public Dictionary<string, List<BookInfo>> Books { get; set; }

        public AuthorData() {
            Books = new Dictionary<string, List<BookInfo>>();
        }
    }
}