using System;
using System.Collections;
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
            var info = Db.Query<BookInfo>(@"select a.full_name Author, b.* from books b
  join fts_book_content c on b.id=c.docid
  join authors a on a.id=b.id_author
  where c.c0content like @key order by b.title LIMIT 100", 
                new { key = "%%" + key + "%%" });
            return info;
        }
    }
}