using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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

        public static IEnumerable<BookInfo> GetSearchData(string key, string searchLang)
        {
            var sql = new StringBuilder(@"select b.id, b.title, b.id_archive, b.file_name, b.file_size, b.md5sum, 
b.created, b.lang, a.id, a.full_name, a.first_name, a.middle_name, a.last_name from books b
join authors a on a.id=b.id_author
join fts_book_content c on b.id=c.docid
join fts_auth_content ac on ac.docid=a.id
where (b.title like @key or c.c0content like @key or ac.c0content like @key)");
            if (searchLang != "all")
            {
                sql.Append(" and b.lang=@lang");
            }
            sql.Append(" order by b.title, b.created DESC LIMIT 100");
            var info = Db.QueryMultiple<BookInfo, AuthorInfo, long>(sql.ToString(), 
                b => b.Id, b => b.Authors, new { key = "%" + key + "%", lang = searchLang });
            return info;
        }

        public static BookDetailedInfo GetBook(long id)
        {
            var info = Db.QueryMultiple<BookDetailedInfo, AuthorInfo, long>(@"select b.id, b.title, b.id_archive, b.file_name, 
b.file_size, b.md5sum, b.created, b.lang, b.description, ar.file_name ArchiveFileName, 
a.id, a.full_name, a.first_name, a.middle_name, a.last_name from books b
join authors a on a.id=b.id_author
join archives ar on ar.id=b.id_archive
where b.id=@id order by b.title, b.created DESC LIMIT 100", 
                b => b.Id, b => b.Authors, new { id }).FirstOrDefault();
            FillBookSequences(info);
            return info;
        }

        public static void FillBookSequences(BookDetailedInfo book)
        {
            if (book == null) return;
            var info = Db.Query<SequenceInfo>(@"select s.*, bs.number BookOrder from bookseq bs
  join sequences s on s.id = bs.id_seq where bs.id_book=@id
order by s.value LIMIT 100", new { id = book.Id });
            book.Sequences = info;
        }

        public static SequenceData GetSequenceData(long id)
        {
            var seq = Db.QueryOne<SequenceData>("select s.*, 0 BookOrder from sequences s where s.id=@id", new {id});
            if (seq == null) return null;

            var sql = new StringBuilder(@"select b.id, b.title, b.id_archive, b.file_name, b.file_size, b.md5sum,
  b.created, b.lang, bs.number BookOrder,
  a.id, a.full_name, a.first_name, a.middle_name, a.last_name
from books b
join authors a on a.id=b.id_author
left join bookseq bs on bs.id_book=b.id
where bs.id_seq=@id
order by bs.number, b.title, b.created DESC LIMIT 1000");
            seq.Books = Db.QueryMultiple<BookInfo, AuthorInfo, long>(sql.ToString(),
                b => b.Id, b => b.Authors, new { id }).ToList();
            return seq;
        }
    }
}