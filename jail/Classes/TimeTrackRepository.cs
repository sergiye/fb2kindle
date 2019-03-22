using System.Collections.Generic;
using jail.Models;
using Simpl.Extensions.Database;

namespace jail.Classes
{
    internal class TimeTrackRepository
    {
        protected static BaseConnectionProvider<long> Db { get; set; }

        static TimeTrackRepository()
        {
            Db = new MsSqlConnectionProvider<long>(SettingsHelper.TimeTrackDatabase);
        }

        public static List<TimeUserInfo> GetAllUsers()
        {
            var result = Db.Query<TimeUserInfo>("select * from UserInfo order by Name");
            result.Insert(0, new TimeUserInfo{Name = "Empty", UserId = 0});
            return result;
        }

        public static List<CheckInOut> GetLastCheckInOut(long userId, int count = 49)
        {
            return Db.Query<CheckInOut>("select top(@count) * from CHECKINOUT c where c.USERID=@userId order by c.CHECKTIME desc"
                , new {count, userId});
        }

        public static int CheckIn(long userId)
        {
            return Db.Execute("INSERT INTO TimeTrack.dbo.CHECKINOUT (USERID, CHECKTIME, CHECKTYPE, VERIFYCODE, WorkCode, UserExtFmt) VALUES (@userId, GETDATE(), 'I', 1, 0, 0)"
                , new {userId});
        }

        public static int CheckOut(long userId)
        {
            return Db.Execute("INSERT INTO TimeTrack.dbo.CHECKINOUT (USERID, CHECKTIME, CHECKTYPE, VERIFYCODE, WorkCode, UserExtFmt) VALUES (@userId, GETDATE(), 'O', 1, 0, 0)"
                , new {userId});
        }
    }
}