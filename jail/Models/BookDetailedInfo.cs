using System.Collections.Generic;
using Simpl.Extensions.Database;

namespace jail.Models
{
    public class BookDetailedInfo: BookInfo
    {
        public string Description { get; set; }
        public List<SequenceInfo> Sequences { get; set; }
        public string ArchiveFileName { get; set; }

        [DapperIgnore]
        public string BookContent { get; set; }

        public BookDetailedInfo()
        {
            Sequences = new List<SequenceInfo>();
        }
    }
}