using System;
using System.Collections.Generic;
using System.Globalization;

namespace jail.Models {
    public class BookInfo : LongIdContainer {
        public string Title { get; set; }
        public List<AuthorInfo> Authors { get; set; }
        public List<SequenceInfo> Sequences { get; set; }
        public long IdArchive { get; set; }
        public string FileName { get; set; }
        public string Md5sum { get; set; }
        public long FileSize { get; set; }
        public long Created { get; set; }
        public string Lang { get; set; }
        public int BookOrder { get; set; }
        public long FavoriteId { get; set; }

        public string FileSizeStr => FileSize.ToFileSizeStr();

        public string CreatedDate =>
            DateTime.TryParseExact(Created.ToString(), "yyMMdd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var dt)
                ? dt.ToString("yyyy-MM-dd")
                : null;

        public BookInfo() {
            Authors = new List<AuthorInfo>();
            Sequences = new List<SequenceInfo>();
        }
    }
}