using System.Data;
using System.Data.SQLite;
using System.Threading.Tasks;

namespace LibraryCleaner {
    internal static class SqlHelper {
        public static string DataBasePath { get; set; }

        public static SQLiteConnection GetConnection() {
            var result = new SQLiteConnection(
                $"Data Source={DataBasePath};synchronous = OFF;journal_mode = MEMORY;Page Size=4096;Cache Size=2000;");
            result.Open();
            return result;
        }

        public static SQLiteCommand GetCommand(string sqlString, SQLiteConnection cn) {
            return cn == null ? null : new SQLiteCommand(sqlString, cn);
        }

        #region Common methods

        public static int ExecuteNonQuery(string sqlString, SQLiteConnection connection = null) {
            if (connection == null) {
                using (var cn = GetConnection()) {
                    using (var cmd = GetCommand(sqlString, cn))
                        return cmd.ExecuteNonQuery();
                }
            }

            using (var cmd = GetCommand(sqlString, connection))
                return cmd.ExecuteNonQuery();
        }

        public static async Task<object> GetScalarFromQuery(string sql) {
            using (var cn = GetConnection()) {
                using (var cmd = GetCommand(sql, cn))
                    return await cmd.ExecuteScalarAsync().ConfigureAwait(false);
            }
        }

        public static async Task<int> GetIntFromQuery(string sql, SQLiteConnection connection = null) {
            if (connection != null) {
                using (var cmd = GetCommand(sql, connection)) {
                    var result = await cmd.ExecuteScalarAsync().ConfigureAwait(false);
                    return result == null ? 0 : int.Parse(result.ToString());
                }
            }

            using (var cn = GetConnection()) {
                using (var cmd = GetCommand(sql, cn)) {
                    var result = await cmd.ExecuteScalarAsync().ConfigureAwait(false);
                    return result == null ? 0 : int.Parse(result.ToString());
                }
            }
        }

        public static string GetString(IDataReader dr, string fieldName) {
            return GetString(dr, fieldName, string.Empty);
        }

        private static string GetString(IDataReader dr, string fieldName, string defaultValue) {
            return GetString(dr, dr.GetOrdinal(fieldName), defaultValue);
        }

        public static string GetString(IDataReader dr, int columnIndex, string defaultValue) {
            return dr.IsDBNull(columnIndex) ? defaultValue : dr.GetString(columnIndex);
        }

        public static int GetInt(IDataReader dr, string fieldName) {
            return GetInt(dr, fieldName, 0);
        }

        private static int GetInt(IDataReader dr, string fieldName, int defaultValue) {
            return GetInt(dr, dr.GetOrdinal(fieldName), defaultValue);
        }

        public static int GetInt(IDataReader dr, int columnIndex, int defaultValue) {
            return dr.IsDBNull(columnIndex) ? defaultValue : dr.GetInt32(columnIndex);
//            return dr.IsDBNull(columnIndex) ? defaultValue : (int) dr.GetDecimal(columnIndex);
        }

        public static bool GetBoolean(IDataReader dr, string fieldName) {
            return GetBoolean(dr, fieldName, false);
        }

        private static bool GetBoolean(IDataReader dr, string fieldName, bool defaultValue) {
            return GetBoolean(dr, dr.GetOrdinal(fieldName), defaultValue);
        }

        private static bool GetBoolean(IDataReader dr, int columnIndex, bool defaultValue) {
            return dr.IsDBNull(columnIndex) ? defaultValue : dr.GetBoolean(columnIndex);
//            return dr.IsDBNull(columnIndex) ? defaultValue : dr.GetDecimal(columnIndex) == 1;
        }

        #endregion Common methods
    }
}