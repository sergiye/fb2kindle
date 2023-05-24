using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using jail.Classes;
using jail.Models;

namespace jail {
    internal class DataRepository : BaseRepository {

        private static string archivesPath;
        public static string ArchivesPath {
            get {
                if (string.IsNullOrWhiteSpace(archivesPath)) {
                    archivesPath = GetConnection().ExecuteScalar<string>("select text from params where id=9");
                    if (!string.IsNullOrWhiteSpace(archivesPath)) {
                        var dbFolder = Path.GetDirectoryName(SettingsHelper.DatabasePath);
                        if (dbFolder != null)
                            archivesPath = Path.Combine(dbFolder, archivesPath);
                    }
                }
                return archivesPath;
            }
        }

        public static async Task<IEnumerable<BookInfo>> GetRandomData(int count, long? userId, string searchLang) {
            var sql = @"select b.id, b.title, b.id_archive IdArchive, b.file_name FileName, b.file_size FileSize, b.md5sum, 
b.created, b.lang, ";
            if (userId.HasValue && userId.Value > 0)
                sql += " f.Id FavoriteId,";
            else
                sql += " 0 FavoriteId,";
            sql += "s.*, bs.number BookOrder, a.id, a.full_name FullName, a.first_name FirstName, a.middle_name MiddleName, a.last_name LastName from books b";
            if (userId.HasValue && userId.Value > 0)
                sql += " left join favorites f on f.bookid=b.id and f.UserId=@userId";
            sql += @" join authors a on a.id=b.id_author
left join bookseq bs on bs.id_book=b.id
left join sequences s on s.id=bs.id_seq ";
            if (searchLang != "all") sql += " WHERE b.lang=@lang ";               
            sql += " order by RANDOM() LIMIT @count";
            var info = await QueryMultipleAsync<BookInfo, SequenceInfo, AuthorInfo, long>(sql,
                b => b.Id, b => b.Sequences, b => b.Authors,
                new {count, userId, lang = searchLang}).ConfigureAwait(false);
            return info;
        }

        public static async Task<IEnumerable<BookInfo>> GetSearchData(string key, string searchLang, long? userId) {
            var sql = @"select b.id, b.title, b.id_archive IdArchive, b.file_name FileName, b.file_size FileSize, b.md5sum, 
b.created, b.lang, ";
            if (userId.HasValue && userId.Value > 0)
                sql += " f.Id FavoriteId,";
            else
                sql += " 0 FavoriteId,";
            sql += "s.*, bs.number BookOrder, a.id, a.full_name FullName, a.first_name FirstName, a.middle_name MiddleName, a.last_name LastName from books b";
            if (userId.HasValue && userId.Value > 0)
                sql += " left join favorites f on f.bookid=b.id and f.UserId=@userId";
            sql += @" join authors a on a.id=b.id_author
join fts_book_content c on b.id=c.docid
join fts_auth_content ac on ac.docid=a.id
left join bookseq bs on bs.id_book=b.id
left join sequences s on s.id=bs.id_seq
where ";
            key = key.ToLower();
            var keyParts = key.Split(' ');
            if (keyParts.Length == 1) {
                sql += " (REPLACE(b.title, ' ', '') like @key or REPLACE(a.search_name, ' ', '') like @key or REPLACE(c.c0content, ' ', '') like @key or REPLACE(ac.c0content, ' ', '') like @key)";
            }
            else {
                for (var i = 0; i < keyParts.Length; i++) {
                    var keyPart = keyParts[i];
                    sql += $" (b.title like '%{keyPart}%' or a.search_name like '%{keyPart}%' or c.c0content like '%{keyPart}%' or ac.c0content like '%{keyPart}%')";
                    if (i != keyParts.Length - 1) {
                        sql += " AND ";
                    }
                }
            }

            if (searchLang != "all") sql += " and b.lang=@lang";

            sql += @" order by CASE WHEN b.lang = 'ru' THEN '1'
              WHEN b.lang = 'en' THEN '2'
              WHEN b.lang = 'ua' THEN '3'
              WHEN b.lang = 'uk' THEN '4'
              ELSE b.lang END ASC, b.title, b.created DESC LIMIT 100";
            var info = await QueryMultipleAsync<BookInfo, SequenceInfo, AuthorInfo, long>(sql,
                b => b.Id, b => b.Sequences, b => b.Authors,
                new {key = $"%{key.Replace(" ", "")}%", lang = searchLang, userId}).ConfigureAwait(false);
            return info;
        }

        public static BookDetailedInfo GetBook(long id) {
            var info = QueryMultiple<BookDetailedInfo, SequenceInfo, AuthorInfo, long>(
                @"select b.id, b.title, b.id_archive IdArchive, b.file_name FileName, 
b.file_size FileSize, b.md5sum, b.created, b.lang, b.description, ar.file_name ArchiveFileName, s.*, bs.number BookOrder, 
a.id, a.full_name FullName, a.first_name FirstName, a.middle_name MiddleName, a.last_name LastName from books b
join authors a on a.id=b.id_author
join archives ar on ar.id=b.id_archive
left join bookseq bs on bs.id_book=b.id
left join sequences s on s.id=bs.id_seq
where b.id=@id order by s.value",
                b => b.Id, b => b.Sequences, b => b.Authors, new {id}).FirstOrDefault();
            return info;
        }

        public static IEnumerable<SequenceInfo> GetBookSequences(long bookId) {
            return GetConnection().Query<SequenceInfo>(@"select s.*, bs.number BookOrder from bookseq bs
  join sequences s on s.id = bs.id_seq where bs.id_book=@id
order by s.value LIMIT 100", new {id = bookId});
        }

        public static SequenceData GetSequenceData(long id, long? userId) {
            var seq = GetConnection().QueryFirstOrDefault<SequenceData>("select s.*, 0 BookOrder from sequences s where s.id=@id", new {id});
            if (seq == null) return null;

            var sql = @"select b.id, b.title, b.id_archive IdArchive, b.file_name FileName, b.file_size FileSize, b.md5sum,
  b.created, b.lang, bs.number BookOrder,";
            sql += userId.HasValue && userId.Value > 0 ? " f.Id FavoriteId," : " 0 FavoriteId,";
            sql += @" a.id, a.full_name FullName, a.first_name FirstName, a.middle_name MiddleName, a.last_name LastName from books b";
            if (userId.HasValue && userId.Value > 0)
                sql += " left join favorites f on f.bookid=b.id and f.UserId=@userId";
            sql += @" join authors a on a.id=b.id_author
left join bookseq bs on bs.id_book=b.id
where bs.id_seq=@id
order by bs.number, b.title, b.created DESC";
            seq.Books = QueryMultiple<BookInfo, AuthorInfo, long>(sql,
                b => b.Id, b => b.Authors, new {id, userId}).ToList();
            return seq;
        }

        public static AuthorData GetAuthorData(long id, long? userId) {
            var author = GetConnection().QueryFirstOrDefault<AuthorData>(
                    "SELECT id, full_name FullName, first_name FirstName, middle_name MiddleName, last_name LastName from authors where id=@id", new {id});
            if (author == null) return null;
            var sql = @"select b.id, b.title, b.id_archive IdArchive, b.file_name FileName, b.file_size FileSize, b.md5sum,
  b.created, b.lang, ";
            sql += userId.HasValue && userId.Value > 0 ? " f.Id FavoriteId," : " 0 FavoriteId,";
            sql += @" s.*, bs.number BookOrder, 
  a.id, a.full_name FullName, a.first_name FirstName, a.middle_name MiddleName, a.last_name LastName from books b";
            if (userId.HasValue && userId.Value > 0)
                sql += " left join favorites f on f.bookid=b.id and f.UserId=@userId";
            sql += @" join authors a on a.id=b.id_author
left join bookseq bs on bs.id_book=b.id
left join sequences s on s.id=bs.id_seq
where a.id=@id
order by CASE WHEN b.lang = 'ru' THEN '1'
              WHEN b.lang = 'en' THEN '2'
              WHEN b.lang = 'ua' THEN '3'
              WHEN b.lang = 'uk' THEN '4'
              ELSE b.lang END ASC, b.title, b.created DESC";
            var books = QueryMultiple<BookInfo, SequenceInfo, AuthorInfo, long>(sql,
                b => b.Id, b => b.Sequences, b => b.Authors, new {id, userId}).ToList();
            author.Books = new Dictionary<string, List<BookInfo>>();
            //var groupedList = books.GroupBy(u => u.Lang, (key, group) =>  new KeyValuePair<string, List<BookInfo>>(key, group.ToList())).ToList();
            var groupedList = books.GroupBy(u => u.Lang).Select(grp => grp.ToList()).ToList();
            foreach (var langList in groupedList) {
                author.Books.Add(langList[0].Lang, langList);
            }

            return author;
        }

        public static async Task<IEnumerable<BookHistoryInfo>> GetHistory(List<BookHistoryInfo> books,
            string sortBy, bool asc, int take, int skip = 0) {

            if (books == null || books.Count == 0) return null;

            var ids = books.Select(b => b.Id).Distinct().ToArray();
            var sql = @"select b.id, b.title, b.id_archive IdArchive, b.file_name FileName, b.file_size FileSize, b.md5sum, 
b.created, b.lang, s.*, bs.number BookOrder, 
a.id, a.full_name FullName, a.first_name FirstName, a.middle_name MiddleName, a.last_name LastName from books b
join authors a on a.id=b.id_author
left join bookseq bs on bs.id_book=b.id
left join sequences s on s.id=bs.id_seq
where b.id in @ids";

            if (!string.IsNullOrWhiteSpace(sortBy)) {
                sql += " ORDER BY " + sortBy;
                if (!asc)
                    sql += " desc";
            }

            sql += " LIMIT @skip, @take";

            var info = (await QueryMultipleAsync<BookHistoryInfo, SequenceInfo, AuthorInfo, long>(sql,
                b => b.Id, b => b.Sequences, b => b.Authors, new {ids, skip, take}).ConfigureAwait(false)).ToList();
            foreach (var bookInfo in info) {
                bookInfo.GeneratedTime = books.First(i => i.Id == bookInfo.Id).GeneratedTime;
            }

            var booksNotInDatabase = books.Where(b => b.Id == 0 || info.All(i => i.Id != b.Id)).ToArray();
            info.AddRange(booksNotInDatabase);

            if (string.IsNullOrWhiteSpace(sortBy))
                return (asc ? info.OrderBy(i => i.GeneratedTime) : info.OrderByDescending(i => i.GeneratedTime));
            return info;
        }

        public static async Task<BookFavoritesViewModel> GetFavorites(long userId, int page, int pageSize, string key) {
            var mainSql = @"from books b
    join favorites f on b.id = f.BookId
    join authors a on a.id = b.id_author
    left join bookseq bs on bs.id_book = b.id
    left join sequences s on s.id = bs.id_seq";

            key = key ?? "";
            if (!string.IsNullOrWhiteSpace(key))
                mainSql += @" join fts_book_content c on b.id=c.docid join fts_auth_content ac on ac.docid=a.id";
            mainSql += " where 1=1";

            if (userId > 0)
                mainSql += " AND f.UserId=@userId ";
            if (!string.IsNullOrWhiteSpace(key))
                mainSql += @" AND ( (REPLACE(b.title, ' ', '') LIKE @key OR REPLACE(a.search_name, ' ', '') LIKE @key OR REPLACE(c.c0content, ' ', '') LIKE @key OR REPLACE(ac.c0content, ' ', '') LIKE @key)
 OR (REPLACE(b.title, ' ', '') LIKE @key2 OR REPLACE(a.search_name, ' ', '') LIKE @key2 OR REPLACE(c.c0content, ' ', '') LIKE @key2 OR REPLACE(ac.c0content, ' ', '') LIKE @key2) ) ";
            var skipped = page * pageSize;
            var filterObject = new {
                userId, skip = skipped, take = pageSize,
                key = $"%{key.ToLower().Replace(" ", "")}%",
                key2 = $"%{string.Join(" ", key.Split(' ').Reverse()).ToLower().Replace(" ", "")}%"
            };

            //fetch total count
            var sql = "select count(*) " + mainSql;
            var totalRows = await GetConnection().QueryFirstOrDefaultAsync<int>(sql, filterObject).ConfigureAwait(false);

            //Fetch books
            sql = @"select b.id, b.title, b.id_archive IdArchive, b.file_name FileName, b.file_size FileSize,
            b.md5sum, b.created, b.lang, f.id FavoriteId, f.UserId, f.DateAdded,
            s.*,
            bs.number BookOrder,
                a.id, a.full_name FullName, a.first_name FirstName, a.middle_name MiddleName, a.last_name LastName ";
            sql += mainSql;
            sql += " order by f.DateAdded desc limit @skip, @take";

            var data = (await QueryMultipleAsync<BookFavoritesInfo, SequenceInfo, AuthorInfo, long>(sql,
                b => b.Id, b => b.Sequences, b => b.Authors, filterObject).ConfigureAwait(false)).ToList();

            return new BookFavoritesViewModel(data, page, Convert.ToInt32(Math.Ceiling((double) totalRows / pageSize)),
                totalRows, skipped);
        }

        public static async Task<long> GetFavoriteId(long bookId, long userId, DateTime? dateAdded) {
            return dateAdded.HasValue
                ? await GetConnection().QueryFirstOrDefaultAsync<long>(
                    "select Id from favorites where BookId=@bookId and UserId=@userId and DateAdded=@dateAdded"
                    , new {bookId, userId, dateAdded}).ConfigureAwait(false)
                : await GetConnection().QueryFirstOrDefaultAsync<long>("select Id from favorites where BookId=@bookId and UserId=@userId"
                    , new {bookId, userId}).ConfigureAwait(false);
        }

        public static async Task<long> SaveFavorite(long bookId, long userId, DateTime dateAdded) {
            return await GetConnection().QueryFirstOrDefaultAsync<long>(
                "INSERT INTO favorites (BookId, UserId, DateAdded) VALUES (@bookId, @userId, @dateAdded); SELECT last_insert_rowid();"
                , new {bookId, userId, dateAdded}).ConfigureAwait(false);
        }

        public static async Task<long> DeleteFavorite(long id) {
            return await GetConnection().ExecuteAsync("delete from favorites where Id=@id", new {id}).ConfigureAwait(false);
        }

        public static async Task<long> DeleteBookById(long id) {
            return await GetConnection().ExecuteAsync("delete from books where Id=@id", new {id}).ConfigureAwait(false);
        }

        public static async Task<IEnumerable<string>> GetAvailableLanguages() {
            return await GetConnection().QueryAsync<string>("select distinct lang from books WHERE lang <> '' AND lang is not null ORDER BY lang").ConfigureAwait(false);
        }

        public static async Task<long> SetBookLanguages(long id, string lang) {
            return await GetConnection().ExecuteAsync("UPDATE books SET lang = @lang WHERE Id = @id", new {id, lang}).ConfigureAwait(false);
        }
    }
}