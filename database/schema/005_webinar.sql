SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- =============================================================================
-- ONE77 Migration — Schema 005: Webinar
-- =============================================================================
-- Deliberately simple, per the approved V1 redesign: admin-authored events,
-- plain customer registrations. No capacity, no seat counts, no reminder/
-- delivery-status tracking, no generated session tables — Tushar's separate
-- WhatsApp integration owns confirmations/reminders, keyed against
-- EventDateTime; this schema only needs to retain the event and registration
-- data that integration will read later.
--
-- Duplicate-registration policy: deliberately NOT enforced at the database
-- level in this milestone. A UNIQUE(EventId, Email) constraint was
-- considered and rejected for now — nothing in the approved V1 requirement
-- asks for it, and adding it preemptively would be exactly the kind of
-- "overly clever" constraint the instruction warned against. Left to
-- application logic if/when actually needed.
-- =============================================================================

DROP TABLE IF EXISTS dbo.WebinarRegistrations;
DROP TABLE IF EXISTS dbo.WebinarEvents;
GO

CREATE TABLE dbo.WebinarEvents (
    EventId         INT IDENTITY(1,1)  NOT NULL,
    Title           NVARCHAR(300)       NOT NULL,
    EventDateTime    DATETIME2(3)         NOT NULL,
    JoinLink         NVARCHAR(500)        NULL,
    Status          NVARCHAR(20)        NOT NULL DEFAULT (N'Active'),
    CreatedAt       DATETIME2(3)         NOT NULL DEFAULT (SYSUTCDATETIME()),
    UpdatedAt       DATETIME2(3)         NOT NULL DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT PK_WebinarEvents PRIMARY KEY CLUSTERED (EventId),
    CONSTRAINT CK_WebinarEvents_Status CHECK (Status IN (N'Active', N'Cancelled', N'Completed'))
);
GO

CREATE INDEX IX_WebinarEvents_Status_DateTime ON dbo.WebinarEvents (Status, EventDateTime);
GO

CREATE TABLE dbo.WebinarRegistrations (
    RegistrationId   INT IDENTITY(1,1)  NOT NULL,
    EventId          INT                 NOT NULL,
    Name             NVARCHAR(200)       NOT NULL,
    Email            NVARCHAR(320)       NOT NULL,
    Phone            NVARCHAR(30)        NOT NULL,
    RegisteredAt     DATETIME2(3)         NOT NULL DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT PK_WebinarRegistrations PRIMARY KEY CLUSTERED (RegistrationId),
    CONSTRAINT FK_WebinarRegistrations_Event FOREIGN KEY (EventId) REFERENCES dbo.WebinarEvents (EventId)
);
GO

CREATE INDEX IX_WebinarRegistrations_EventId ON dbo.WebinarRegistrations (EventId);
GO
