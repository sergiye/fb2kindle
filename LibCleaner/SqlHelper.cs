using System;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;

namespace LibCleaner
{
    public class SqlHelper
    {
        public static string DataBasePath { get; set; }

        public static SQLiteConnection GetConnection()
        {
            var result = new SQLiteConnection(string.Format("Data Source={0}", DataBasePath));
            result.Open();
            return result;
        }

        public static IDbCommand GetCommand(string sqlString, IDbConnection cn)
        {
            var connection = cn as SQLiteConnection;
            if (connection == null)
                return null;
            return new SQLiteCommand(sqlString, connection);
        }

        public static DbParameter AddParameterToCommand(IDbCommand cmd, string parameterName, object parameter)
        {
            var command = cmd as SQLiteCommand;
            if (command == null) return null;
            if (parameter == null)
                return command.Parameters.AddWithValue(parameterName, DBNull.Value);

            if (parameter is DateTime)
            {
                var dt = (DateTime) parameter;
                if (dt == DateTime.MinValue)
                    return command.Parameters.AddWithValue(parameterName, DBNull.Value);
                var param = new SQLiteParameter(parameterName, DbType.Date) {Value = dt};
                command.Parameters.Add(param);
                return param;
            }
            return command.Parameters.AddWithValue(parameterName, parameter);
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

        public static object GetScalarFromQuery(string sql, QueryParameter parameters)
        {
            return GetScalarFromQuery(sql, new[] {parameters});
        }

        public static object GetScalarFromQuery(string sql, QueryParameter[] parameters = null)
        {
            using (IDbConnection cn = GetConnection())
            {
                var cmd = GetCommand(sql, cn);
                if (parameters != null && parameters.Length > 0)
                    foreach (var _parameter in parameters)
                        AddParameterToCommand(cmd, _parameter.Name, _parameter.Value);
                return cmd.ExecuteScalar();
            }
        }

        #endregion Common methods

        public class QueryParameter
        {
            public string Name { get; set; }
            public object Value { get; set; }
            
            public QueryParameter(string parameterName, object parameterValue)
            {
                Name = parameterName;
                if (parameterValue is DateTime)
                    Value = (DateTime) parameterValue == DateTime.MinValue ? DBNull.Value : parameterValue;
                else
                    Value = parameterValue;
            }
        }
    }
}