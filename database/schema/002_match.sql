SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- =============================================================================
-- ONE77 Migration — Schema 002: Match
-- =============================================================================
-- ONE77's core differentiated feature. Preserves the exact three-state-plus-
-- priority model and soft-delete semantics from the audited reference
-- behavior (Phase 1). Relationships reference Shopify products directly
-- (product-level only — no variant-level Match).
--
-- Completeness (complete / needs_review / no_matches) is deliberately NOT
-- stored anywhere in this file — it stays computed at read time by
-- application/query logic, exactly as in the reference system. The indexes
-- below exist to make that computation efficient, not to cache its result.
-- =============================================================================

DROP TABLE IF EXISTS dbo.MatchRelationshipUseCases;
DROP TABLE IF EXISTS dbo.MatchRelationships;
GO

CREATE TABLE dbo.MatchRelationships (
    Id                         INT IDENTITY(1,1)  NOT NULL,
    SourceShopifyProductId     NVARCHAR(64)         NOT NULL,   -- the airgun; Shopify reference only
    TargetShopifyProductId     NVARCHAR(64)         NOT NULL,   -- pellet or accessory; Shopify reference only
    TargetCategory             NVARCHAR(20)         NOT NULL,
    Status                     NVARCHAR(20)         NOT NULL DEFAULT (N'compatible'),
    Priority                   TINYINT              NULL,        -- 1=Best Match, 2=Recommended, 3=Alternative
    Reason                     NVARCHAR(500)         NULL,
    AdminNotes                  NVARCHAR(1000)        NULL,
    CalibreOverride              BIT                  NOT NULL DEFAULT (0),
    IsActive                   BIT                  NOT NULL DEFAULT (1),   -- SOFT DELETE flag — see below
    CreatedAt                  DATETIME2(3)          NOT NULL DEFAULT (SYSUTCDATETIME()),
    UpdatedAt                  DATETIME2(3)          NOT NULL DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT PK_MatchRelationships PRIMARY KEY CLUSTERED (Id),

    -- Vocabulary preserved EXACTLY as the reference system stores it
    -- (lowercase, underscored) — this is a parity requirement, not a new
    -- schema design choice, unlike Learn/Webinar's status columns below.
    CONSTRAINT CK_MatchRelationships_Status
        CHECK (Status IN (N'compatible', N'recommended', N'not_recommended')),

    CONSTRAINT CK_MatchRelationships_TargetCategory
        CHECK (TargetCategory IN (N'pellet', N'accessory')),

    CONSTRAINT CK_MatchRelationships_Priority
        CHECK (Priority IS NULL OR Priority IN (1, 2, 3)),

    -- Priority is meaningful (and only ever set) when Status = 'recommended'.
    -- A 'compatible' or 'not_recommended' row with a non-null priority is
    -- rejected at the database level, not left to application discipline.
    CONSTRAINT CK_MatchRelationships_PriorityOnlyWhenRecommended
        CHECK ((Status <> N'recommended' AND Priority IS NULL) OR (Status = N'recommended')),

    CONSTRAINT CK_MatchRelationships_NotSelf
        CHECK (SourceShopifyProductId <> TargetShopifyProductId)
);
GO

-- SOFT DELETE, not hard delete: a relationship is retired by setting
-- IsActive = 0, never by removing the row. This filtered unique index
-- enforces "only one ACTIVE relationship per (source, target) pair" while
-- explicitly allowing a soft-deleted row and a new active row for the same
-- pair to coexist — which is exactly what "remove, then add again" produces.
CREATE UNIQUE INDEX UQ_MatchRelationships_ActivePair
    ON dbo.MatchRelationships (SourceShopifyProductId, TargetShopifyProductId)
    WHERE IsActive = 1;
GO

-- Supports the completeness query (grouped by source, filtered to active
-- non-excluded rows) and the public Match resolver (all active relationships
-- for one airgun).
CREATE INDEX IX_MatchRelationships_Source_Active
    ON dbo.MatchRelationships (SourceShopifyProductId, IsActive, Status, TargetCategory);

CREATE INDEX IX_MatchRelationships_Target
    ON dbo.MatchRelationships (TargetShopifyProductId) WHERE IsActive = 1;
GO

-- Relationship-owned use cases. Not a taxonomy/master table — UseCase is a
-- plain string, exactly as in the reference system.
CREATE TABLE dbo.MatchRelationshipUseCases (
    MatchRelationshipId   INT             NOT NULL,
    UseCase               NVARCHAR(30)    NOT NULL,
    CONSTRAINT PK_MatchRelationshipUseCases PRIMARY KEY CLUSTERED (MatchRelationshipId, UseCase),
    CONSTRAINT FK_MatchRelationshipUseCases_Relationship FOREIGN KEY (MatchRelationshipId)
        REFERENCES dbo.MatchRelationships (Id) ON DELETE CASCADE
);
GO
