using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using jail.Models;
using Simpl.Extensions.Database;

namespace jail.Classes
{
    internal class DataRepository
    {
        protected static BaseConnectionProvider<Guid> Db { get; set; }

        static DataRepository()
        {
            Db = new SqLiteConnectionProvider<Guid>(SettingsHelper.DatabasePath);
        }

        private static string _archivesPath;
        public static string ArchivesPath
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_archivesPath))
                {
                    _archivesPath = Db.QueryOne<string>("select text from params where id=9");
                    if (!string.IsNullOrWhiteSpace(_archivesPath))
                    {
                        var dbFolder = Path.GetDirectoryName(SettingsHelper.DatabasePath);
                        if (dbFolder != null)
                            _archivesPath = Path.Combine(dbFolder, _archivesPath);
                    }
                }
                return _archivesPath;
            }
        }

        public static IEnumerable<BookInfo> GetSearchData(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return new List<BookInfo>();
            var info = Db.QueryMultiple<BookInfo, AuthorInfo, long>(@"select b.*, ar.file_name ArchiveFileName, a.* from books b
join fts_book_content c on b.id=c.docid
join authors a on a.id=b.id_author
join fts_auth_content ac on ac.docid=a.id
join archives ar on ar.id=b.id_archive
where c.c0content like @key or ac.c0content like @key
order by b.title LIMIT 100", 
                b => b.Id, b => b.Authors, new { key = "%%" + key + "%%" });
            return info;
        }

        public static BookInfo GetBook(long id)
        {
            var info = Db.QueryMultiple<BookInfo, AuthorInfo, long>(@"select b.*, ar.file_name ArchiveFileName, a.* from books b
join authors a on a.id=b.id_author
join archives ar on ar.id=b.id_archive
where b.id=@id
order by b.title LIMIT 100", 
                b => b.Id, b => b.Authors, new { id }).FirstOrDefault();
            FillBookSequences(info);
            return info;
        }

        public static void FillBookSequences(BookInfo book)
        {
            if (book == null) return;
            var info = Db.Query<SequenceInfo>(@"select s.*, bs.number BookOrder from bookseq bs
  join sequences s on s.id = bs.id_seq where bs.id_book=@id
order by s.value LIMIT 100", new { id = book.Id });
            book.Sequences = info;
        }
    }
}