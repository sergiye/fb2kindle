-- Database  : jail
create table SystemLogs
(
    Id            integer not null
        primary key autoincrement,
    EnteredDate   timestamp default CURRENT_TIMESTAMP not null,
    Level         varchar(100),
    Message       nvarchar(2048),
    MachineName   varchar(512),
    UserName      nvarchar(255),
    Exception     nvarchar(4092),
    CallerAddress varchar(100)
);

create table Users
(
    Id             integer not null
        primary key autoincrement,
    Email          nvarchar(255),
    Password       nvarchar(32),
    UserType       int     not null,
    RegisteredTime timestamp default current_timestamp not null,
    Active         bit,
    TimeTrackId    int
);


INSERT INTO Users (Email, Password, UserType, Active, TimeTrackId) VALUES (null, null, 0, 0, null);
INSERT INTO Users (Email, Password, UserType, Active, TimeTrackId) VALUES ('egoshin.sergey@kindle.com', '2a3dfa66c2d8e8c67b77f2a25886e3cf', 1, 1, 0);
