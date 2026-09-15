SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- =============================================================================
-- ONE77 Migration — Schema 004: Learn
-- =============================================================================
-- Plain-text knowledge base. No Shopify involvement in this domain at all.
--
-- STATUS VOCABULARY, PER EXPLICIT INSTRUCTION: stored directly as
-- 'Draft' / 'Live' / 'Hidden' — the feature's real-world meaning — with NO
-- Published/Archived translation layer in between. This differs deliberately
-- from Phase 1's ground truth of the OLD Mongo system (which stored
-- draft/published/archived internally and only showed Draft/Live/Hidden as
-- admin-facing labels): the new schema removes that indirection entirely,
-- per the explicit instruction not to introduce it here.
-- =============================================================================

DROP TABLE IF EXISTS dbo.LearnEntries;
DROP TABLE IF EXISTS dbo.LearnCategories;
GO

CREATE TABLE dbo.LearnCategories (
    Id             INT IDENTITY(1,1)  NOT NULL,
    Name           NVARCHAR(200)       NOT NULL,
    Slug           NVARCHAR(200)       NOT NULL,
    Description    NVARCHAR(1000)      NULL,
    SortPriority   INT                 NOT NULL DEFAULT (0),
    Status         NVARCHAR(20)        NOT NULL DEFAULT (N'Draft'),
    CreatedAt      DATETIME2(3)         NOT NULL DEFAULT (SYSUTCDATETIME()),
    UpdatedAt      DATETIME2(3)         NOT NULL DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT PK_LearnCategories PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_LearnCategories_Slug UNIQUE (Slug),
    CONSTRAINT CK_LearnCategories_Status CHECK (Status IN (N'Draft', N'Live', N'Hidden'))
);
GO

CREATE INDEX IX_LearnCategories_Status_Sort ON dbo.LearnCategories (Status, SortPriority);
GO

CREATE TABLE dbo.LearnEntries (
    Id                 INT IDENTITY(1,1)  NOT NULL,
    LearnCategoryId     INT                 NOT NULL,
    Title              NVARCHAR(300)       NOT NULL,
    Body               NVARCHAR(MAX)       NOT NULL,   -- plain text only; no HTML/markup
    SortPriority       INT                 NOT NULL DEFAULT (0),
    Status             NVARCHAR(20)        NOT NULL DEFAULT (N'Draft'),
    CreatedAt          DATETIME2(3)         NOT NULL DEFAULT (SYSUTCDATETIME()),
    UpdatedAt          DATETIME2(3)         NOT NULL DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT PK_LearnEntries PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_LearnEntries_Category FOREIGN KEY (LearnCategoryId) REFERENCES dbo.LearnCategories (Id),
    CONSTRAINT CK_LearnEntries_Status CHECK (Status IN (N'Draft', N'Live', N'Hidden'))
);
GO

CREATE INDEX IX_LearnEntries_Category_Sort ON dbo.LearnEntries (LearnCategoryId, SortPriority);
CREATE INDEX IX_LearnEntries_Status ON dbo.LearnEntries (Status);
GO
