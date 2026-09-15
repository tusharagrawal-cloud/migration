SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- =============================================================================
-- ONE77 Migration — Schema 007: Homepage Configuration
-- =============================================================================
-- Smallest possible model for one admin-controlled homepage content field:
-- the hero photograph. A true singleton — Id is fixed at 1 by a CHECK
-- constraint, and the one row is seeded here at deploy time, so no runtime
-- "ensure a row exists" logic is needed anywhere (unlike AdminUsers'
-- seed-on-boot, which exists because its content is environment-supplied
-- secrets; this row's initial content is always the same empty state).
--
-- HeroImagePath stores a URL-relative path only (e.g. "/media/hero-<guid>.jpg")
-- — never a physical filesystem path, and never the image binary itself. The
-- physical file lives on the server's file system, outside SQL Server
-- entirely; see PRODUCTION_CONFIGURATION.md for where.
--
-- Deliberately does NOT store: hero text, layout, CSS, overlay strength, or
-- any other styling/positioning value — those remain frontend-owned and
-- locked, per the approved Admin-Managed Homepage Hero Image milestone.
-- =============================================================================

DROP TABLE IF EXISTS dbo.HomepageConfig;
GO

CREATE TABLE dbo.HomepageConfig (
    Id             INT            NOT NULL,
    HeroImagePath  NVARCHAR(400)  NULL,
    UpdatedAt      DATETIME2(3)   NOT NULL DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT PK_HomepageConfig PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT CK_HomepageConfig_SingletonId CHECK (Id = 1)
);
GO

INSERT INTO dbo.HomepageConfig (Id, HeroImagePath) VALUES (1, NULL);
GO
