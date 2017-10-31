using System;
using System.Collections.Generic;
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

        public static IEnumerable<BookInfo> GetSearchData(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return new List<BookInfo>();
            var info = Db.QueryMultiple<BookInfo, AuthorInfo, long>(@"select b.*, a.* from books b
join fts_book_content c on b.id=c.docid
join authors a on a.id=b.id_author
join fts_auth_content ac on ac.docid=a.id
where c.c0content like @key or ac.c0content like @key
order by b.title LIMIT 100", 
                b => b.Id, b => b.Authors, new { key = "%%" + key + "%%" });
            return info;
        }
    }
}