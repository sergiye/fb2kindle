-- Database  : jail
-- Version   : Microsoft SQL Server  14.0.2002.14

CREATE DATABASE [jail]
COLLATE SQL_Latin1_General_CP1_CI_AS
GO

USE [jail]
GO

--
-- Definition for table SystemLogs : 
--

CREATE TABLE [dbo].[SystemLogs] (
  [Id] int IDENTITY(1, 1) NOT NULL,
  [EnteredDate] datetime CONSTRAINT [DF_system_logging_entered_date] DEFAULT getdate() NULL,
  [Level] varchar(100) NULL,
  [Message] nvarchar(2048) NULL,
  [MachineName] varchar(512) NULL,
  [UserName] nvarchar(255) NULL,
  [Exception] nvarchar(max) NULL,
  [CallerAddress] varchar(100) NULL
)
ON [PRIMARY]
GO

--
-- Definition for table Users : 
--

CREATE TABLE [dbo].[Users] (
  [Id] int IDENTITY(1, 1) NOT NULL,
  [Email] nvarchar(255) NULL,
  [Password] nvarchar(32) NULL,
  [UserType] int NOT NULL,
  [RegisteredTime] datetime CONSTRAINT [DF__Users__Registere__14270015] DEFAULT getdate() NOT NULL,
  [Active] bit NULL,
  [TimeTrackId] int NULL
)
ON [PRIMARY]
GO

--
-- Data for table dbo.Users  (LIMIT 0,500)
--

SET IDENTITY_INSERT [dbo].[Users] ON
GO

INSERT INTO [dbo].[Users] ([Id], [Email], [Password], [UserType], [RegisteredTime], [Active])
VALUES 
  (1, NULL, NULL, 0, getdate(), 0)
GO

INSERT INTO [dbo].[Users] ([Id], [Email], [Password], [UserType], [RegisteredTime], [Active])
VALUES 
  (2, N'egoshin.sergey@kindle.com', N'2a3dfa66c2d8e8c67b77f2a25886e3cf', 1, getdate(), 1)
GO


SET IDENTITY_INSERT [dbo].[Users] OFF
GO

--
-- Definition for indices : 
--

ALTER TABLE [dbo].[SystemLogs]
ADD CONSTRAINT [PK__SystemLogs] 
PRIMARY KEY CLUSTERED ([Id])
WITH (
  PAD_INDEX = OFF,
  IGNORE_DUP_KEY = OFF,
  STATISTICS_NORECOMPUTE = OFF,
  ALLOW_ROW_LOCKS = ON,
  ALLOW_PAGE_LOCKS = ON)
ON [PRIMARY]
GO

ALTER TABLE [dbo].[Users]
ADD CONSTRAINT [PK__Users__3214EC07B66BE62F] 
PRIMARY KEY CLUSTERED ([Id])
WITH (
  PAD_INDEX = OFF,
  IGNORE_DUP_KEY = OFF,
  STATISTICS_NORECOMPUTE = OFF,
  ALLOW_ROW_LOCKS = ON,
  ALLOW_PAGE_LOCKS = ON)
ON [PRIMARY]
GO

ALTER TABLE [dbo].[Users]
ADD CONSTRAINT [Users_uq] 
UNIQUE NONCLUSTERED ([Email])
WITH (
  PAD_INDEX = OFF,
  IGNORE_DUP_KEY = OFF,
  STATISTICS_NORECOMPUTE = OFF,
  ALLOW_ROW_LOCKS = ON,
  ALLOW_PAGE_LOCKS = ON)
ON [PRIMARY]
GO

