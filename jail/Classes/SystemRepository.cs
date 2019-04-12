using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using jail.Models;
using Simpl.Extensions.Database;

namespace jail.Classes
{
    internal class SystemRepository
    {
        protected static BaseConnectionProvider<long> Db { get; set; }

        static SystemRepository()
        {
            Db = new MsSqlConnectionProvider<long>(SettingsHelper.StatisticDatabase);
//            SqlMapper.AddTypeHandler(new UtcTimeHandler());
        }

        #region Logging

        public static IList<SystemLog> GetErrorLogData(int count, string key = null, 
            SystemLog.LogItemType searchType = SystemLog.LogItemType.All, bool hideTrace = true)
        {
            var sql = new StringBuilder(@"select top(@count) EnteredDate, Level, Message, MachineName, UserName, 
Exception, CallerAddress from SystemLogs where 1=1");
            if (!string.IsNullOrWhiteSpace(key))
                sql.Append(" and (Message like @key or Exception like @key or UserName like @key or CallerAddress like @key)");
            if (searchType != SystemLog.LogItemType.All)
            {
                sql.Append(" and Level='").Append(searchType).Append("'");
            }
            else
            {
                if (hideTrace)
                    sql.Append(" and Level<>'Trace'");
            }
            if (!Debugger.IsAttached)
            {
                sql.Append(" and MachineName='").Append(Environment.MachineName).Append("'");
            }
            sql.Append(" order by EnteredDate desc");
            var result = Db.Query<SystemLog>(sql.ToString(), new { count, key = string.Format("%{0}%", key) });
            //foreach (var item in result) item.EnteredDate = item.EnteredDate.ToLocalTime();
            return result;
        }

        public static int ClearByMessagePart(string selection)
        {
            return Db.Execute("delete from SystemLogs where [message] like '%'+@selection+'%' or UserName=@selection or MachineName=@selection or CallerAddress like @selection", 
                new { selection });
        }

        public static int CalcByMessagePart(string selection)
        {
            return Db.QueryInt("select count(1) from SystemLogs where [message] like '%'+@selection+'%' or UserName=@selection or MachineName=@selection or CallerAddress like @selection", 
                new { selection });
        }

        public static int ClearByDays(int days)
        {
            return Db.Execute("delete from SystemLogs where EnteredDate<DATEADD(day,-@days,getutcdate()) and (Level='Trace' or Level='Debug')", new { days });
        }

        #endregion
    }
}