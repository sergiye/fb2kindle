using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.Text;

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

        protected static void AddSqlCondition(StringBuilder strSQL, bool addWhere)
        {
            strSQL.Append(addWhere ? " WHERE " : " and ");
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

        public static long ProcessInsert(string tableName, List<QueryParameter> values)
        {
            return ProcessInsert(tableName, values.ToArray());
        }

        public static long ProcessInsert(string tableName, QueryParameter[] values)
        {
            if (values == null || string.IsNullOrEmpty(tableName))
                return -1;
            using (IDbConnection cn = GetConnection())
            {
                var stringBuilder = new StringBuilder();
                stringBuilder.AppendFormat("INSERT INTO {0} (", tableName);
                for (var i = 0; i < values.Length; i++)
                {
                    if (i != 0)
                        stringBuilder.Append(", ");
                    stringBuilder.Append(values[i].Name);
                }
                stringBuilder.Append(") VALUES (");
                for (var i = 0; i < values.Length; i++)
                {
                    if (i != 0)
                        stringBuilder.Append(", ");
                    stringBuilder.AppendFormat("@{0}", values[i].Name);
                }
                stringBuilder.Append(")");
                using (var cmd = GetCommand(stringBuilder.ToString(), cn))
                {
                    for (var i = 0; i < values.Length; i++)
                        AddParameterToCommand(cmd, string.Format("@{0}", values[i].Name), values[i].Value);
                    cmd.ExecuteNonQuery();
                }
                return -1;
            }
        }

        public static bool ProcessUpdate(string tableName, List<QueryParameter> values, string whereCondition)
        {
            return ProcessUpdate(tableName, values.ToArray(), whereCondition);
        }

        public static bool ProcessUpdate(string tableName, QueryParameter[] values, string whereCondition)
        {
            if (values == null || string.IsNullOrEmpty(tableName))
                return false;
            using (IDbConnection cn = GetConnection())
            {
                var stringBuilder = new StringBuilder();
                stringBuilder.Append(string.Format("UPDATE {0} SET ", tableName));
                for (var i = 0; i < values.Length; i++)
                {
                    if (i != 0)
                        stringBuilder.Append(", ");
                    stringBuilder.AppendFormat("{1}.{0}=@{0}", values[i].Name, tableName);
                }
                stringBuilder.AppendFormat(" WHERE {0}", whereCondition);
                using (var cmd = GetCommand(stringBuilder.ToString(), cn))
                {
                    for (var i = 0; i < values.Length; i++)
                        AddParameterToCommand(cmd, string.Format("@{0}", values[i].Name), values[i].Value);
                    cmd.ExecuteNonQuery();
                }
            }
            return true;
        }

        public static bool ProcessUpdate(string tableName, QueryParameter[] values, string whereCondition, string connectionString)
        {
            if (values == null || string.IsNullOrEmpty(tableName))
                return false;
            using (IDbConnection cn = GetConnection())
            {
                var stringBuilder = new StringBuilder();
                stringBuilder.Append(string.Format("UPDATE {0} SET ", tableName));
                for (var i = 0; i < values.Length; i++)
                {
                    if (i != 0)
                        stringBuilder.Append(", ");
                    stringBuilder.AppendFormat("{1}.{0}=@{0}", values[i].Name, tableName);
                }
                stringBuilder.AppendFormat(" WHERE {0}", whereCondition);
                using (var cmd = GetCommand(stringBuilder.ToString(), cn))
                {
                    for (var i = 0; i < values.Length; i++)
                        AddParameterToCommand(cmd, string.Format("@{0}", values[i].Name), values[i].Value);
                    cmd.ExecuteNonQuery();
                }
            }
            return true;
        }

        public static void DeleteRecordByID(string tableName, long id, string idField)
        {
            ExecuteNonQuery(string.Format("DELETE FROM {0} WHERE ({1}={2})", tableName, idField, id));
        }

        public static void DeleteRecordByID(string tableName, long id)
        {
            DeleteRecordByID(tableName, id, "ID");
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

        public struct QueryParameter
        {
            private string _name;
            private object _value;

            public QueryParameter(string parameterName, object parameterValue)
            {
                _name = parameterName;
                if (parameterValue is DateTime)
                    _value = (DateTime) parameterValue == DateTime.MinValue ? DBNull.Value : parameterValue;
                else
                    _value = parameterValue;
            }

            public string Name
            {
                get { return _name; }
                set { _name = value; }
            }

            public object Value
            {
                get { return _value; }
                set { _value = value; }
            }
        }
    }
}