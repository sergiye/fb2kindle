using Simpl.Extensions.Database;

namespace jail.Models
{
    public class AuthorInfo: LongIdContainer
    {
        public string Letter { get; set; }
        public string FullName { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public string SearchName { get; set; }
        public long Number { get; set; }

        public override string ToString()
        {
            return FullName;
        }
    }
}