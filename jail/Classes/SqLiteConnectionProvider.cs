using System;
using System.Data;
using System.Data.SQLite;
using System.Threading.Tasks;
using DapperExtensions.Sql;
using Simpl.Extensions.Database;

namespace jail.Classes
{
    internal class SqLiteConnectionProvider<IdType> : BaseConnectionProvider<IdType> where IdType : IEquatable<IdType>
    {
        protected string SqLiteConnectionString
        {
            get { return string.Format("Data Source='{0}';Version=3;FailIfMissing=True;DateTimeKind=Utc;UTF8Encoding=True;synchronous = OFF;journal_mode = MEMORY;Page Size=4096;Cache Size=2000;", ConnectionString); }
        }

        internal SqLiteConnectionProvider(string databasePath)
            : base(databasePath)
        {
            DapperExtensions.DapperExtensions.SqlDialect = new SqliteDialect();
        }

        #region Basic Methods

        public override IDbConnection GetConnection()
        {
            var cn = new SQLiteConnection(SqLiteConnectionString);            
            cn.Open();                        
            return cn;
        }

        public override async Task<IDbConnection> GetConnectionAsync()
        {
            var cn = new SQLiteConnection(SqLiteConnectionString);            
            await cn.OpenAsync();                        
            return cn;
        }

        public override IDbTransaction GetTransaction(IDbConnection connection)
        {
            if (connection.State != ConnectionState.Open)
                connection.Open();
            return connection.BeginTransaction();
        }

        public override IDbCommand GetCommand(string sqlString, IDbConnection cn)
        {
            var connection = cn as SQLiteConnection;
            return connection == null ? null : new SQLiteCommand(sqlString, connection);
        }

        #endregion Basic Methods
    }
}