using System;
using System.Data;
using System.Data.SQLite;
using System.Threading.Tasks;
using Simpl.Extensions.Database;

namespace jail.Classes {
    internal class SqLiteConnectionProvider<IdType> : BaseConnectionProvider<IdType> where IdType : IEquatable<IdType> {
        private readonly bool failIfMissing;

        protected string SqLiteConnectionString =>
            $"Data Source='{ConnectionString}';Version=3;FailIfMissing={failIfMissing};DateTimeKind=Utc;UTF8Encoding=True;synchronous = OFF;Journal Mode=Off;Page Size=4096;Cache Size=2000;";

        internal SqLiteConnectionProvider(string databasePath, bool failIfMissing = true)
            : base(databasePath) {
            this.failIfMissing = failIfMissing;
        }

        #region Basic Methods

        public override IDbConnection GetConnection() {
            var cn = new SQLiteConnection(SqLiteConnectionString);
            cn.Open();
            return cn;
        }

        public override async Task<IDbConnection> GetConnectionAsync() {
            var cn = new SQLiteConnection(SqLiteConnectionString);
            await cn.OpenAsync();
            return cn;
        }

        public override IDbTransaction GetTransaction(IDbConnection connection) {
            if (connection.State != ConnectionState.Open)
                connection.Open();
            return connection.BeginTransaction();
        }

        public override IDbCommand GetCommand(string sqlString, IDbConnection cn) {
            var connection = cn as SQLiteConnection;
            return connection == null ? null : new SQLiteCommand(sqlString, connection);
        }

        #endregion Basic Methods
    }
}