/*
  KMC City Events Platform - Database Setup
  Target: SQL Server
  Database: KMC_CityEventsDB
*/

IF DB_ID('KMC_CityEventsDB') IS NULL
BEGIN
    CREATE DATABASE KMC_CityEventsDB;
END
GO

USE KMC_CityEventsDB;
GO

IF OBJECT_ID('dbo.EventRegistrations', 'U') IS NOT NULL
    DROP TABLE dbo.EventRegistrations;
GO

IF OBJECT_ID('dbo.Events', 'U') IS NOT NULL
    DROP TABLE dbo.Events;
GO

IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL
    DROP TABLE dbo.Users;
GO

CREATE TABLE dbo.Users
(
    UserID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Username NVARCHAR(80) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    Role NVARCHAR(30) NOT NULL,
    DisplayName NVARCHAR(120) NULL,
    Email NVARCHAR(160) NULL,
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT CK_Users_Role CHECK (Role IN ('Organizer', 'Participant', 'Public', 'Admin'))
);
GO

CREATE TABLE dbo.Events
(
    EventID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Title NVARCHAR(200) NOT NULL,
    EventType NVARCHAR(80) NOT NULL,
    Description NVARCHAR(800) NULL,
    Venue NVARCHAR(180) NOT NULL,
    EventDate DATETIME2 NOT NULL,
    TicketPrice DECIMAL(10,2) NOT NULL CONSTRAINT DF_Events_TicketPrice DEFAULT (0),
    Capacity INT NOT NULL CONSTRAINT DF_Events_Capacity DEFAULT (100),
    CreatedByUserID INT NOT NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_Events_IsActive DEFAULT (1),
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Events_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NULL,
    CONSTRAINT FK_Events_Users_CreatedBy FOREIGN KEY (CreatedByUserID) REFERENCES dbo.Users(UserID),
    CONSTRAINT CK_Events_Capacity CHECK (Capacity > 0),
    CONSTRAINT CK_Events_TicketPrice CHECK (TicketPrice >= 0)
);
GO

CREATE TABLE dbo.EventRegistrations
(
    RegistrationID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    EventID INT NOT NULL,
    ParticipantUserID INT NOT NULL,
    SeatCount INT NOT NULL CONSTRAINT DF_EventRegistrations_SeatCount DEFAULT (1),
    RegisteredAt DATETIME2 NOT NULL CONSTRAINT DF_EventRegistrations_RegisteredAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_EventRegistrations_Events FOREIGN KEY (EventID) REFERENCES dbo.Events(EventID),
    CONSTRAINT FK_EventRegistrations_Users FOREIGN KEY (ParticipantUserID) REFERENCES dbo.Users(UserID),
    CONSTRAINT UQ_EventRegistrations_EventParticipant UNIQUE (EventID, ParticipantUserID),
    CONSTRAINT CK_EventRegistrations_SeatCount CHECK (SeatCount > 0)
);
GO

CREATE INDEX IX_Events_EventDate ON dbo.Events(EventDate);
CREATE INDEX IX_Events_EventType ON dbo.Events(EventType);
CREATE INDEX IX_Events_CreatedByUserID ON dbo.Events(CreatedByUserID);
CREATE INDEX IX_EventRegistrations_Participant ON dbo.EventRegistrations(ParticipantUserID);
GO

INSERT INTO dbo.Users (Username, PasswordHash, Role, DisplayName, Email)
VALUES
    ('organizer1', '1234', 'Organizer', 'KMC Culture Office', 'organizer1@kmc.lk'),
    ('organizer2', '1234', 'Organizer', 'Green Kandy Foundation', 'organizer2@kmc.lk'),
    ('participant1', '1234', 'Participant', 'Nuwan Perera', 'participant1@example.com'),
    ('participant2', '1234', 'Participant', 'Ishara Silva', 'participant2@example.com'),
    ('public1', '1234', 'Public', 'Guest User', 'guest@example.com');
GO

DECLARE @Organizer1 INT = (SELECT UserID FROM dbo.Users WHERE Username = 'organizer1');
DECLARE @Organizer2 INT = (SELECT UserID FROM dbo.Users WHERE Username = 'organizer2');

INSERT INTO dbo.Events
    (Title, EventType, Description, Venue, EventDate, TicketPrice, Capacity, CreatedByUserID, IsActive)
VALUES
    ('Kandy Esala Cultural Night', 'Festival', 'Traditional dance and drumming performances.', 'Kandy City Grounds', DATEADD(DAY, 14, SYSUTCDATETIME()), 0, 500, @Organizer1, 1),
    ('City Volunteer Clean-Up Drive', 'Community', 'Community clean-up with school clubs and NGOs.', 'Kandy Lake Round', DATEADD(DAY, 7, SYSUTCDATETIME()), 0, 300, @Organizer2, 1),
    ('Local Food and Crafts Fair', 'Exhibition', 'Small business booths, crafts, and local cuisine.', 'KMC Public Hall', DATEADD(DAY, 21, SYSUTCDATETIME()), 250, 400, @Organizer1, 1),
    ('Public Safety Awareness Session', 'Workshop', 'Traffic, emergency, and city safety guidance.', 'KMC Training Center', DATEADD(DAY, 10, SYSUTCDATETIME()), 0, 120, @Organizer2, 1);
GO

DECLARE @Participant1 INT = (SELECT UserID FROM dbo.Users WHERE Username = 'participant1');
DECLARE @Participant2 INT = (SELECT UserID FROM dbo.Users WHERE Username = 'participant2');
DECLARE @Event1 INT = (SELECT EventID FROM dbo.Events WHERE Title = 'Kandy Esala Cultural Night');
DECLARE @Event2 INT = (SELECT EventID FROM dbo.Events WHERE Title = 'City Volunteer Clean-Up Drive');

INSERT INTO dbo.EventRegistrations (EventID, ParticipantUserID, SeatCount)
VALUES
    (@Event1, @Participant1, 2),
    (@Event2, @Participant2, 1);
GO
