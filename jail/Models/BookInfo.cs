using Simpl.Extensions.Database;

namespace jail.Models
{
    public class BookInfo: LongIdContainer
    {
        public string Title { get; set; }
        public string Author { get; set; }
        public long IdAuthor { get; set; }
        public long IdArchive { get; set; }
        public string FileName { get; set; }
        public long FileSize { get; set; }
        public string Annotation { get; set; }
        public string Description { get; set; }
        public string Lang { get; set; }
    }
}