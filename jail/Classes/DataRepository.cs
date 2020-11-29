using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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

        public static async Task<IEnumerable<BookInfo>> GetSearchData(string key, string searchLang, long? userId)
        {
            var key2 = $"%{string.Join(" ", key.Split(' ').Reverse()).ToLower().Replace(" ", "")}%";
            var sql = @"select b.id, b.title, b.id_archive, b.file_name, b.file_size, b.md5sum, 
b.created, b.lang, ";
            if (userId.HasValue && userId.Value > 0)
                sql += " f.Id FavoriteId,";
            else
                sql += " 0 FavoriteId,";
            sql += "s.*, bs.number BookOrder, a.id, a.full_name, a.first_name, a.middle_name, a.last_name from books b";
            if (userId.HasValue && userId.Value > 0)
                sql += " left join favorites f on f.bookid=b.id and f.UserId=@userId";
            sql += @" join authors a on a.id=b.id_author
join fts_book_content c on b.id=c.docid
join fts_auth_content ac on ac.docid=a.id
left join bookseq bs on bs.id_book=b.id
left join sequences s on s.id=bs.id_seq
where (REPLACE(b.title, ' ', '') like @key or REPLACE(a.search_name, ' ', '') like @key or REPLACE(c.c0content, ' ', '') like @key or REPLACE(ac.c0content, ' ', '') like @key)
   OR (REPLACE(b.title, ' ', '') like @key2 or REPLACE(a.search_name, ' ', '') like @key2 or REPLACE(c.c0content, ' ', '') like @key2 or REPLACE(ac.c0content, ' ', '') like @key2)";
            if (searchLang != "all") sql += " and b.lang=@lang";
            sql += @" order by CASE WHEN b.lang = 'ru' THEN '1'
              WHEN b.lang = 'en' THEN '2'
              WHEN b.lang = 'ua' THEN '3'
              WHEN b.lang = 'uk' THEN '4'
              ELSE b.lang END ASC, b.title, b.created DESC LIMIT 100";
            var info = await Db.QueryMultipleAsync<BookInfo, SequenceInfo, AuthorInfo, long>(sql, 
                b => b.Id, b => b.Sequences, b => b.Authors, 
                new { key = $"%{key.ToLower().Replace(" ", "")}%", key2, lang = searchLang, userId }).ConfigureAwait(false);
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

        public static SequenceData GetSequenceData(long id, long? userId)
        {
            var seq = Db.QueryOne<SequenceData>("select s.*, 0 BookOrder from sequences s where s.id=@id", new {id});
            if (seq == null) return null;

            var sql = @"select b.id, b.title, b.id_archive, b.file_name, b.file_size, b.md5sum,
  b.created, b.lang, bs.number BookOrder,";
            sql += userId.HasValue && userId.Value > 0 ? " f.Id FavoriteId," : " 0 FavoriteId,";
            sql += @" a.id, a.full_name, a.first_name, a.middle_name, a.last_name from books b";
            if (userId.HasValue && userId.Value > 0)
                sql += " left join favorites f on f.bookid=b.id and f.UserId=@userId";
            sql += @" join authors a on a.id=b.id_author
left join bookseq bs on bs.id_book=b.id
where bs.id_seq=@id
order by bs.number, b.title, b.created DESC";
            seq.Books = Db.QueryMultiple<BookInfo, AuthorInfo, long>(sql,
                b => b.Id, b => b.Authors, new { id, userId }).ToList();
            return seq;
        }

        public static AuthorData GetAuthorData(long id, long? userId)
        {
            var author = Db.QueryOne<AuthorData>("SELECT id, full_name, first_name, middle_name, last_name from authors where id=@id", new {id});
            if (author == null) return null;
            var sql = @"select b.id, b.title, b.id_archive, b.file_name, b.file_size, b.md5sum,
  b.created, b.lang, ";
            sql += userId.HasValue && userId.Value > 0 ? " f.Id FavoriteId," : " 0 FavoriteId,";
            sql += @" s.*, bs.number BookOrder, 
  a.id, a.full_name, a.first_name, a.middle_name, a.last_name from books b";
            if (userId.HasValue && userId.Value > 0)
                sql += " left join favorites f on f.bookid=b.id and f.UserId=@userId";
            sql+= @" join authors a on a.id=b.id_author
left join bookseq bs on bs.id_book=b.id
left join sequences s on s.id=bs.id_seq
where a.id=@id
order by CASE WHEN b.lang = 'ru' THEN '1'
              WHEN b.lang = 'en' THEN '2'
              WHEN b.lang = 'ua' THEN '3'
              WHEN b.lang = 'uk' THEN '4'
              ELSE b.lang END ASC, b.title, b.created DESC";
            var books = Db.QueryMultiple<BookInfo, SequenceInfo, AuthorInfo, long>(sql,
                b => b.Id, b => b.Sequences, b => b.Authors, new { id, userId }).ToList();
            author.Books = new Dictionary<string, List<BookInfo>>();
            //var groupedList = books.GroupBy(u => u.Lang, (key, group) =>  new KeyValuePair<string, List<BookInfo>>(key, group.ToList())).ToList();
            var groupedList = books.GroupBy(u => u.Lang).Select(grp => grp.ToList()).ToList();
            foreach (var langList in groupedList)
            {
                author.Books.Add(langList[0].Lang, langList);
            }
            return author;
        }

        public static async Task<IEnumerable<BookHistoryInfo>> GetHistory(IEnumerable<BookHistoryInfo> data)
        {
            if (data == null) return null;
            var books = data.ToArray();
            if (books.Length == 0) return null;

            var ids = books.Select(b => b.Id).Distinct().ToArray();
            var sql = @"select b.id, b.title, b.id_archive, b.file_name, b.file_size, b.md5sum, 
b.created, b.lang, s.*, bs.number BookOrder, 
a.id, a.full_name, a.first_name, a.middle_name, a.last_name from books b
join authors a on a.id=b.id_author
left join bookseq bs on bs.id_book=b.id
left join sequences s on s.id=bs.id_seq
where b.id in @ids";
            var info = (await Db.QueryMultipleAsync<BookHistoryInfo, SequenceInfo, AuthorInfo, long>(sql, 
                b => b.Id, b => b.Sequences, b => b.Authors, new { ids }).ConfigureAwait(false)).ToList();
            foreach (var bookInfo in info)
            {
                bookInfo.GeneratedTime = books.First(i => i.Id == bookInfo.Id).GeneratedTime;
            }
            info.AddRange(books.Where(b=>b.Id == 0));
            return info.OrderByDescending(i=>i.GeneratedTime);
        }

        public static async Task<BookFavoritesViewModel> GetFavorites(long userId, int page, int pageSize, string key)
        {
            var mainSql = @"from books b
    join favorites f on b.id = f.BookId
    join authors a on a.id = b.id_author
    join fts_book_content c on b.id=c.docid
    join fts_auth_content ac on ac.docid=a.id
    left join bookseq bs on bs.id_book = b.id
    left join sequences s on s.id = bs.id_seq
where 1=1";
            
            if (userId > 0)
                mainSql += " AND f.UserId=@userId ";
            key = key ?? "";
            if (!string.IsNullOrWhiteSpace(key))
                mainSql += @" AND ( (REPLACE(b.title, ' ', '') LIKE @key OR REPLACE(a.search_name, ' ', '') LIKE @key OR REPLACE(c.c0content, ' ', '') LIKE @key OR REPLACE(ac.c0content, ' ', '') LIKE @key)
 OR (REPLACE(b.title, ' ', '') LIKE @key2 OR REPLACE(a.search_name, ' ', '') LIKE @key2 OR REPLACE(c.c0content, ' ', '') LIKE @key2 OR REPLACE(ac.c0content, ' ', '') LIKE @key2) ) ";
            var skipped = page * pageSize;
            var filterObject = new
            {
                userId, skip = skipped, take = pageSize,
                key = $"%{key.ToLower().Replace(" ", "")}%",
                key2 = $"%{string.Join(" ", key.Split(' ').Reverse()).ToLower().Replace(" ", "")}%"
            };

            //fetch total count
            var sql = "select count(*) " + mainSql;
            var totalRows = await Db.QueryOneAsync<int>(sql, filterObject).ConfigureAwait(false);

            //Fetch books
            sql = @"select b.id, b.title, b.id_archive, b.file_name, b.file_size,
            b.md5sum, b.created, b.lang, f.id FavoriteId, f.UserId, f.DateAdded,
            s.*,
            bs.number BookOrder,
                a.id, a.full_name, a.first_name, a.middle_name, a.last_name ";
            sql += mainSql;
            sql += " order by f.DateAdded desc limit @skip, @take";
            
            var data = (await Db.QueryMultipleAsync<BookFavoritesInfo, SequenceInfo, AuthorInfo, long>(sql, 
                b => b.Id, b => b.Sequences, b => b.Authors, filterObject).ConfigureAwait(false)).ToList();

            return new BookFavoritesViewModel(data, page, Convert.ToInt32(Math.Ceiling((double)totalRows / pageSize)), totalRows, skipped);
        }

        public static async Task<long> GetFavoriteId(long bookId, long userId, DateTime? dateAdded)
        {
            return dateAdded.HasValue
                ? await Db.QueryOneAsync<long>(
                    "select Id from favorites where BookId=@bookId and UserId=@userId and DateAdded=@dateAdded"
                    , new {bookId, userId, dateAdded}).ConfigureAwait(false)
                : await Db.QueryOneAsync<long>("select Id from favorites where BookId=@bookId and UserId=@userId"
                    , new {bookId, userId}).ConfigureAwait(false);
        }

        public static async Task<long> SaveFavorite(long bookId, long userId, DateTime dateAdded)
        {
            return await Db.QueryOneAsync<long>("INSERT INTO favorites (BookId, UserId, DateAdded) VALUES (@bookId, @userId, @dateAdded); SELECT last_insert_rowid();"
                , new {bookId, userId, dateAdded}).ConfigureAwait(false);
        }

        public static async Task<long> DeleteFavorite(long id)
        {
            return await Db.ExecuteAsync("delete from favorites where Id=@id", new {id}).ConfigureAwait(false);
        }
    }
}