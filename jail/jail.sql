-- SQL Manager 2008 for SQL Server 3.3.0.1
-- ---------------------------------------
-- Database  : jail
-- Version   : Microsoft SQL Server  11.0.6251.0


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
  [EnteredDate] datetime CONSTRAINT [DF_system_logging_entered_date] DEFAULT getutcdate() NULL,
  [Level] varchar(100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
  [Message] nvarchar(2048) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
  [MachineName] varchar(512) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
  [UserName] varchar(1024) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
  [Exception] nvarchar(max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
  [CallerAddress] varchar(100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL
)
ON [PRIMARY]
GO

--
-- Definition for table Users : 
--

CREATE TABLE [dbo].[Users] (
  [Id] int IDENTITY(1, 1) NOT NULL,
  [Email] varchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
  [Password] varchar(32) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
  [UserType] int NOT NULL,
  [RegisteredTime] datetime CONSTRAINT [DF__Users__Registere__14270015] DEFAULT getutcdate() NOT NULL,
  [Active] bit NULL
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
  (2, NULL, NULL, 0, '20171113 15:03:45.537', 0)
GO

INSERT INTO [dbo].[Users] ([Id], [Email], [Password], [UserType], [RegisteredTime], [Active])
VALUES 
  (4, N'egoshin.sergey@kindle.com', N'2a3dfa66c2d8e8c67b77f2a25886e3cf', 1, '20171113 15:24:13.790', 1)
GO

INSERT INTO [dbo].[Users] ([Id], [Email], [Password], [UserType], [RegisteredTime], [Active])
VALUES 
  (5, N'MwdnDevice@kindle.com', N'2a3dfa66c2d8e8c67b77f2a25886e3cf', 0, '20171229 10:53:54.517', 1)
GO

INSERT INTO [dbo].[Users] ([Id], [Email], [Password], [UserType], [RegisteredTime], [Active])
VALUES 
  (6, N'simpl2000@kindle.com', N'2a3dfa66c2d8e8c67b77f2a25886e3cf', 0, '20171229 13:43:50.130', 1)
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

