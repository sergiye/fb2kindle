using Simpl.Extensions.Database;

namespace jail.Classes
{
    internal class BaseRepository
    {
        protected static BaseConnectionProvider<long> Db { get; set; }

        static BaseRepository()
        {
            Db = new SqLiteConnectionProvider<long>(SettingsHelper.StatisticDatabase, false);
            //SqlMapper.AddTypeHandler(new UtcTimeHandler());
            CheckDatabaseInitialized();
        }

        public static void CheckDatabaseInitialized()
        {
            Db.Execute(@"create table IF NOT EXISTS SystemLogs (
    Id            integer not null primary key autoincrement,
    EnteredDate   timestamp default CURRENT_TIMESTAMP not null,
    Level         varchar(100),
    Message       nvarchar(2048),
    MachineName   varchar(512),
    UserName      nvarchar(255),
    Exception     nvarchar(4092),
    CallerAddress varchar(100)
); 
create table IF NOT EXISTS Users (
    Id             integer not null primary key autoincrement,
    Email          nvarchar(255),
    Password       nvarchar(32),
    UserType       int     not null,
    RegisteredTime timestamp default current_timestamp not null,
    Active         bit,
    TimeTrackId    int
);");
        }
    }
}