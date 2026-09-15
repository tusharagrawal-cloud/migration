SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- =============================================================================
-- ONE77 Migration — Schema 003: Bundles
-- =============================================================================
-- V1 Bundles are curated "buy these together" groupings only. No price,
-- savings, inventory, or image data lives here — Shopify owns all of that,
-- and product presentation uses each item's own Shopify images. Composition
-- is product-level Shopify references only (no variant-level curation).
-- =============================================================================

DROP TABLE IF EXISTS dbo.BundleItems;
DROP TABLE IF EXISTS dbo.Bundles;
GO

CREATE TABLE dbo.Bundles (
    Id             INT IDENTITY(1,1)  NOT NULL,
    Name           NVARCHAR(200)       NOT NULL,
    Tagline        NVARCHAR(500)       NULL,

    -- New-system schema choice (not a literal reproduction of old Mongo
    -- values) — kept consistent with the PascalCase convention used for
    -- Learn/Webinar status columns below.
    Status         NVARCHAR(20)        NOT NULL DEFAULT (N'Draft'),

    SortPriority   INT                 NOT NULL DEFAULT (0),
    CreatedAt      DATETIME2(3)         NOT NULL DEFAULT (SYSUTCDATETIME()),
    UpdatedAt      DATETIME2(3)         NOT NULL DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT PK_Bundles PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT CK_Bundles_Status CHECK (Status IN (N'Draft', N'Published', N'Archived'))
);
GO

CREATE INDEX IX_Bundles_Status ON dbo.Bundles (Status);
GO

CREATE TABLE dbo.BundleItems (
    Id                  INT IDENTITY(1,1)  NOT NULL,
    BundleId            INT                 NOT NULL,
    ShopifyProductId    NVARCHAR(64)         NOT NULL,   -- Shopify reference only, product-level
    ItemRole             NVARCHAR(20)         NOT NULL,   -- 'Primary' | 'Pellet' | 'Accessory'
    SortOrder            INT                 NOT NULL DEFAULT (0),

    CONSTRAINT PK_BundleItems PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_BundleItems_Bundle FOREIGN KEY (BundleId) REFERENCES dbo.Bundles (Id) ON DELETE CASCADE,
    CONSTRAINT CK_BundleItems_ItemRole CHECK (ItemRole IN (N'Primary', N'Pellet', N'Accessory')),
    CONSTRAINT UQ_BundleItems_NoDuplicateLine UNIQUE (BundleId, ShopifyProductId)
);
GO

-- Preserves the existing business rule ("exactly one primary/airgun item per
-- bundle") as a real database constraint.
CREATE UNIQUE INDEX UQ_BundleItems_OnePrimaryPerBundle
    ON dbo.BundleItems (BundleId) WHERE ItemRole = N'Primary';

CREATE INDEX IX_BundleItems_BundleId_Sort ON dbo.BundleItems (BundleId, SortOrder);
GO
