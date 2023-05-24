using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Dapper;
using jail.Classes;
using jail.Models;

namespace jail {
    internal class BaseRepository {
        
        private static readonly string connectionString;

        static BaseRepository() {
            
            var dbPath = SettingsHelper.DatabasePath;
            var local = Path.Combine(HttpRuntime.AppDomainAppPath, dbPath);
            if (dbPath == Path.GetFileName(dbPath))
                dbPath = local;

            connectionString = $"Data Source='{dbPath}';Version=3;FailIfMissing={true};DateTimeKind=Utc;UTF8Encoding=True;synchronous = OFF;Journal Mode=Off;Page Size=4096;Cache Size=2000;";

            //SqlMapper.AddTypeHandler(new UtcTimeHandler());
            CheckDatabaseInitialized();
        }

        protected static SQLiteConnection GetConnection() {
            return new SQLiteConnection(connectionString);
        }

        public static IEnumerable<TParent> QueryMultiple<TParent, TChild, TParentKey>(string sql,
            Func<TParent, TParentKey> parentKeySelector,
            Func<TParent, ICollection<TChild>> childSelector,
            object param = null) {
            using (var cn = GetConnection()) {
                var cache = new Dictionary<TParentKey, TParent>();
                cn.Query<TParent, TChild, TParent>(
                    sql,
                    (parent, child) => {
                        if (!cache.ContainsKey(parentKeySelector(parent)))
                            cache.Add(parentKeySelector(parent), parent);
                        var cachedParent = cache[parentKeySelector(parent)];
                        var children = childSelector(cachedParent);
                        if (child != null) children.Add(child);
                        return cachedParent;
                    }, param);
                return cache.Values;
            }
        }

        protected static IEnumerable<TParent> QueryMultiple<TParent, TFirstChild, TSecondChild, TParentKey>(string sql,
            Func<TParent, TParentKey> parentKeySelector,
            Func<TParent, IList<TFirstChild>> firstChildSelector,
            Func<TParent, IList<TSecondChild>> secondChildSelector,
            object param = null)
            where TFirstChild : LongIdContainer
            where TSecondChild : LongIdContainer {
            using (var cn = GetConnection()) {
                var cache = new Dictionary<TParentKey, TParent>();
                cn.Query<TParent, TFirstChild, TSecondChild, TParent>(
                    sql,
                    (parent, firstChild, secondChild) => {
                        if (!cache.ContainsKey(parentKeySelector(parent)))
                            cache.Add(parentKeySelector(parent), parent);
                        var cachedParent = cache[parentKeySelector(parent)];
                        if (firstChild != null) {
                            if (firstChildSelector(cachedParent).All(f => !f.Id.Equals(firstChild.Id)))
                                firstChildSelector(cachedParent).Add(firstChild);
                        }

                        if (secondChild != null) {
                            if (secondChildSelector(cachedParent).All(f => !f.Id.Equals(secondChild.Id)))
                                secondChildSelector(cachedParent).Add(secondChild);
                        }

                        return cachedParent;
                    }, param);
                return cache.Values;
            }
        }

        protected static async Task<IEnumerable<TParent>> QueryMultipleAsync<TParent, TFirstChild, TSecondChild, TParentKey>(
            string sql,
            Func<TParent, TParentKey> parentKeySelector,
            Func<TParent, IList<TFirstChild>> firstChildSelector,
            Func<TParent, IList<TSecondChild>> secondChildSelector,
            object param = null)
            where TFirstChild : LongIdContainer
            where TSecondChild : LongIdContainer {
            using (var cn = GetConnection()){
                var cache = new Dictionary<TParentKey, TParent>();
                await cn.QueryAsync<TParent, TFirstChild, TSecondChild, TParent>(
                    sql,
                    (parent, firstChild, secondChild) => {
                        if (!cache.ContainsKey(parentKeySelector(parent)))
                            cache.Add(parentKeySelector(parent), parent);
                        var cachedParent = cache[parentKeySelector(parent)];
                        if (firstChild != null) {
                            if (firstChildSelector(cachedParent).All(f => !f.Id.Equals(firstChild.Id)))
                                firstChildSelector(cachedParent).Add(firstChild);
                        }

                        if (secondChild != null) {
                            if (secondChildSelector(cachedParent).All(f => !f.Id.Equals(secondChild.Id)))
                                secondChildSelector(cachedParent).Add(secondChild);
                        }

                        return cachedParent;
                    }, param);
                return cache.Values;
            }
        }

        public static void CheckDatabaseInitialized() {
            GetConnection().Execute(@"create table IF NOT EXISTS SystemLogs (
    Id            integer not null primary key autoincrement,
    EnteredDate   timestamp default (datetime('now','localtime')) not null,
    Level         varchar(100),
    Message       nvarchar(2048),
    MachineName   varchar(512),
    UserName      nvarchar(255),
    Exception     nvarchar(4092),
    CallerAddress varchar(100)
); 
create table IF NOT EXISTS Users (
    Id             integer not null primary key autoincrement,
    Email          nvarchar(255),
    Password       nvarchar(32),
    UserType       int     not null,
    RegisteredTime timestamp default current_timestamp not null,
    Active         bit,
    FlibustaId     int
);");
        }
    }
}