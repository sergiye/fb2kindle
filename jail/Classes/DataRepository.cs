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
        protected static BaseConnectionProvider<long> Db { get; set; }

        static DataRepository()
        {
            Db = new SqLiteConnectionProvider<long>(SettingsHelper.DatabasePath);
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
b.created, b.lang, s.*, bs.number BookOrder, 
a.id, a.full_name, a.first_name, a.middle_name, a.last_name from books b
join authors a on a.id=b.id_author
join fts_book_content c on b.id=c.docid
join fts_auth_content ac on ac.docid=a.id
left join bookseq bs on bs.id_book=b.id
left join sequences s on s.id=bs.id_seq
where (REPLACE(b.title, ' ', '') like @key or REPLACE(a.search_name, ' ', '') like @key or REPLACE(c.c0content, ' ', '') like @key or REPLACE(ac.c0content, ' ', '') like @key)");
            if (searchLang != "all")
            {
                sql.Append(" and b.lang=@lang");
            }
            sql.Append(@" order by CASE WHEN b.lang = 'ru' THEN '1'
              WHEN b.lang = 'en' THEN '2'
              WHEN b.lang = 'ua' THEN '3'
              WHEN b.lang = 'uk' THEN '4'
              ELSE b.lang END ASC, b.title, b.created DESC LIMIT 100");
            var info = Db.QueryMultiple<BookInfo, SequenceInfo, AuthorInfo, long>(sql.ToString(), 
                b => b.Id, b => b.Sequences, b => b.Authors, new { key = "%" + key.ToLower().Replace(" ", "") + "%", lang = searchLang });
            return info;
        }

        public static BookDetailedInfo GetBook(long id)
        {
            var info = Db.QueryMultiple<BookDetailedInfo, SequenceInfo, AuthorInfo, long>(@"select b.id, b.title, b.id_archive, b.file_name, 
b.file_size, b.md5sum, b.created, b.lang, b.description, ar.file_name ArchiveFileName, s.*, bs.number BookOrder, 
a.id, a.full_name, a.first_name, a.middle_name, a.last_name from books b
join authors a on a.id=b.id_author
join archives ar on ar.id=b.id_archive
left join bookseq bs on bs.id_book=b.id
left join sequences s on s.id=bs.id_seq
where b.id=@id order by s.value", 
                b => b.Id, b => b.Sequences, b => b.Authors, new { id }).FirstOrDefault();
            return info;
        }

        public static List<SequenceInfo> GetBookSequences(long bookId)
        {
            return Db.Query<SequenceInfo>(@"select s.*, bs.number BookOrder from bookseq bs
  join sequences s on s.id = bs.id_seq where bs.id_book=@id
order by s.value LIMIT 100", new { id = bookId });
        }

        public static SequenceData GetSequenceData(long id)
        {
            var seq = Db.QueryOne<SequenceData>("select s.*, 0 BookOrder from sequences s where s.id=@id", new {id});
            if (seq == null) return null;

            seq.Books = Db.QueryMultiple<BookInfo, AuthorInfo, long>(@"select b.id, b.title, b.id_archive, b.file_name, b.file_size, b.md5sum,
  b.created, b.lang, bs.number BookOrder,
  a.id, a.full_name, a.first_name, a.middle_name, a.last_name
from books b
join authors a on a.id=b.id_author
left join bookseq bs on bs.id_book=b.id
where bs.id_seq=@id
order by bs.number, b.title, b.created DESC",
                b => b.Id, b => b.Authors, new { id }).ToList();
            return seq;
        }

        public static AuthorData GetAuthorData(long id)
        {
            var author = Db.QueryOne<AuthorData>("SELECT id, full_name, first_name, middle_name, last_name from authors where id=@id", new {id});
            if (author == null) return null;
            var books = Db.QueryMultiple<BookInfo, SequenceInfo, AuthorInfo, long>(@"select b.id, b.title, b.id_archive, b.file_name, b.file_size, b.md5sum,
  b.created, b.lang, s.*, bs.number BookOrder, 
  a.id, a.full_name, a.first_name, a.middle_name, a.last_name
from books b
join authors a on a.id=b.id_author
left join bookseq bs on bs.id_book=b.id
left join sequences s on s.id=bs.id_seq
where a.id=@id
order by CASE WHEN b.lang = 'ru' THEN '1'
              WHEN b.lang = 'en' THEN '2'
              WHEN b.lang = 'ua' THEN '3'
              WHEN b.lang = 'uk' THEN '4'
              ELSE b.lang END ASC, b.title, b.created DESC",
                b => b.Id, b => b.Sequences, b => b.Authors, new { id }).ToList();
            author.Books = new Dictionary<string, List<BookInfo>>();
            //var groupedList = books.GroupBy(u => u.Lang, (key, group) =>  new KeyValuePair<string, List<BookInfo>>(key, group.ToList())).ToList();
            var groupedList = books.GroupBy(u => u.Lang).Select(grp => grp.ToList()).ToList();
            foreach (var langList in groupedList)
            {
                author.Books.Add(langList[0].Lang, langList);
            }
            return author;
        }
    }
}