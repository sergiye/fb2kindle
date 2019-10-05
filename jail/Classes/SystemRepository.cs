using jail.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace jail.Classes
{

    internal class SystemRepository : BaseRepository
    {
        #region Logging

        public static IList<SystemLog> GetErrorLogData(int count, string key = null,
            SystemLog.LogItemType searchType = SystemLog.LogItemType.All)
        {
            var sql = new StringBuilder(@"select EnteredDate, Level, Message, MachineName, UserName, 
Exception, CallerAddress from SystemLogs where 1=1");
            if (!string.IsNullOrWhiteSpace(key))
                sql.Append(" and (Message like @key or Exception like @key or UserName like @key or CallerAddress like @key)");
            switch (searchType)
            {
                case SystemLog.LogItemType.Full:
                    //no filter
                    break;
                case SystemLog.LogItemType.All:
                    //all except trace
                    sql.Append(" and Level<>'Trace'");
                    break;
                case SystemLog.LogItemType.Fatal:
                case SystemLog.LogItemType.Error:
                case SystemLog.LogItemType.Warn:
                case SystemLog.LogItemType.Info:
                case SystemLog.LogItemType.Debug:
                case SystemLog.LogItemType.Trace:
                default:
                    sql.Append(" and Level='").Append(searchType).Append("'");
                    break;
            }
            if (!Debugger.IsAttached)
            {
                sql.Append(" and MachineName='").Append(Environment.MachineName).Append("'");
            }
            sql.Append(" order by EnteredDate desc LIMIT @count ");
            var result = Db.Query<SystemLog>(sql.ToString(), new { count, key = string.Format("%{0}%", key) });
            //foreach (var item in result) item.EnteredDate = item.EnteredDate.ToLocalTime();
            return result;
        }

        public static int ClearByMessagePart(string selection)
        {
            return Db.Execute("delete from SystemLogs where Message like '%' || @selection || '%' or UserName=@selection or MachineName=@selection or CallerAddress like @selection",
                new { selection });
        }

        public static long CalcByMessagePart(string selection)
        {
            return Db.QueryOne<long>("select count(1) from SystemLogs where Message like '%' || @selection || '%' or UserName=@selection or MachineName=@selection or CallerAddress like @selection",
                new { selection });
        }

        #endregion
    }
}