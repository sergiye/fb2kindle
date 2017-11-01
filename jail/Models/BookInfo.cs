using System;
using System.Collections.Generic;
using System.Globalization;
using Simpl.Extensions;
using Simpl.Extensions.Database;

namespace jail.Models
{
    public class BookInfo: LongIdContainer
    {
        public string Title { get; set; }
        public List<AuthorInfo> Authors { get; set; }
        public long IdArchive { get; set; }
        public string FileName { get; set; }
        public string Md5sum { get; set; }
        public long FileSize { get; set; }
        public long Created { get; set; }
        public string Lang { get; set; }
        public int BookOrder { get; set; }

        [DapperIgnore]
        public string FileSizeStr { get { return StringHelper.FileSizeStr(FileSize); } }

        [DapperIgnore]
        public string CreatedDate {
            get
            {
                DateTime dt;
                return DateTime.TryParseExact(Created.ToString(), "yyMMdd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out dt) ? dt.ToString("yyyy-MM-dd") : null;
            } 
        }

        public BookInfo()
        {
            Authors = new List<AuthorInfo>();
        }
    }
}