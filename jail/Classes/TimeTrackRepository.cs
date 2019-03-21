using System;
using System.Collections.Generic;
using Simpl.Extensions.Database;

namespace jail.Classes
{
    public class UserInfo
    {
        public long UserId { get; set; }
        public string Name { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }

    public enum CheckTypes
    {
        I,
        O
    }

    public class CheckInOut
    {
        public long UserId { get; set; }
        public DateTime CheckTime { get; set; }
        public CheckTypes CheckType { get; set; }
        public int VerifyCode { get; set; }
        public int WorkCode { get; set; }
    }
    
    internal class TimeTrackRepository
    {
        protected static BaseConnectionProvider<long> Db { get; set; }

        static TimeTrackRepository()
        {
            Db = new MsSqlConnectionProvider<long>(SettingsHelper.TimeTrackDatabase);
        }

        public static List<UserInfo> GetAllUsers()
        {
            return Db.Query<UserInfo>("select * from UserInfo order by Name");
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