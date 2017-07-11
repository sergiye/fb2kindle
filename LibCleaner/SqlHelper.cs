using System.Data;
using System.Data.SQLite;

namespace LibCleaner
{
    internal static class SqlHelper
    {
        public static string DataBasePath { get; set; }

        public static SQLiteConnection GetConnection()
        {
            var result = new SQLiteConnection(string.Format("Data Source={0};synchronous = OFF;journal_mode = MEMORY;Page Size=4096;Cache Size=2000;", DataBasePath));
            result.Open();
            return result;
        }

        public static IDbCommand GetCommand(string sqlString, IDbConnection cn)
        {
            var connection = cn as SQLiteConnection;
            return connection == null ? null : new SQLiteCommand(sqlString, connection);
        }

        #region Common methods

        public static void ExecuteNonQuery(string sqlString)
        {
            using (IDbConnection cn = GetConnection())
            {
                using (var cmd = GetCommand(sqlString, cn))
                    cmd.ExecuteNonQuery();
            }
        }

        public static object GetScalarFromQuery(string sql)
        {
            using (IDbConnection cn = GetConnection())
            {
                var cmd = GetCommand(sql, cn);
                return cmd.ExecuteScalar();
            }
        }

        public static string GetString(IDataReader dr, string fieldName)
        {
            return GetString(dr, fieldName, string.Empty);
        }

        private static string GetString(IDataReader dr, string fieldName, string defaultValue)
        {
            return GetString(dr, dr.GetOrdinal(fieldName), defaultValue);
        }

        public static string GetString(IDataReader dr, int columnIndex, string defaultValue)
        {
            return dr.IsDBNull(columnIndex) ? defaultValue : dr.GetString(columnIndex);
        }

        public static int GetInt(IDataReader dr, string fieldName)
        {
            return GetInt(dr, fieldName, 0);
        }

        private static int GetInt(IDataReader dr, string fieldName, int defaultValue)
        {
            return GetInt(dr, dr.GetOrdinal(fieldName), defaultValue);
        }

        public static int GetInt(IDataReader dr, int columnIndex, int defaultValue)
        {
            return dr.IsDBNull(columnIndex) ? defaultValue : dr.GetInt32(columnIndex);
//            return dr.IsDBNull(columnIndex) ? defaultValue : (int) dr.GetDecimal(columnIndex);
        }

        public static bool GetBoolean(IDataReader dr, string fieldName)
        {
            return GetBoolean(dr, fieldName, false);
        }

        private static bool GetBoolean(IDataReader dr, string fieldName, bool defaultValue)
        {
            return GetBoolean(dr, dr.GetOrdinal(fieldName), defaultValue);
        }

        private static bool GetBoolean(IDataReader dr, int columnIndex, bool defaultValue)
        {
            return dr.IsDBNull(columnIndex) ? defaultValue : dr.GetBoolean(columnIndex);
//            return dr.IsDBNull(columnIndex) ? defaultValue : dr.GetDecimal(columnIndex) == 1;
        }

        #endregion Common methods
    }
}