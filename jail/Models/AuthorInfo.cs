using Simpl.Extensions.Database;

namespace jail.Models
{
    public class AuthorInfo: LongIdContainer
    {
        public string FullName { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }

        public string DisplayName
        {
            get { return string.Join(" ", FirstName, MiddleName, LastName).Trim(); }
        }

        public override string ToString()
        {
            return FullName.Trim();
        }
    }
}