SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- =============================================================================
-- ONE77 Migration — Schema 001: Product Enrichment
-- =============================================================================
-- Shopify owns product identity, name, brand, description, images, variants,
-- price, compare-at price, inventory, and ecommerce lifecycle. NONE of that
-- is duplicated here. This file stores only:
--   (a) a lightweight reference to a Shopify product,
--   (b) the handful of fields ONE77's own Match logic must query numerically
--       or structurally (see reasoning below — not a full spec system), and
--   (c) a flexible key/value table for the long tail of purely descriptive
--       specifications that Match never queries.
--
-- MATCH-CRITICAL FIELD REASONING (documented before finalizing, per instruction)
-- -------------------------------------------------------------------------
-- The existing Match derived-fallback resolver (the logic that runs when no
-- curated relationship exists for a category) compares:
--   - an airgun's Calibre against a pellet's Calibre (exact string equality)
--   - a pellet's WeightGrains against an airgun's recommended weight RANGE
--     (a numeric BETWEEN comparison)
--   - an airgun's PowerplantType against an accessory's list of compatible
--     powerplants (structural set membership)
--
-- A generic SpecKey/SpecValue string table cannot do a reliable numeric
-- BETWEEN comparison or an efficient equality/membership match without
-- CAST/CONVERT on every read and no usable index — that would make the one
-- query this whole feature exists to answer both slow and fragile. These five
-- fields are therefore promoted to real typed columns on ProductEnrichment
-- (small, fixed, genuinely reused by business logic), and compatible
-- powerplants get one small child table (the same one-to-many pattern already
-- used for MatchRelationshipUseCases) because an accessory can be compatible
-- with more than one powerplant and that is a structural membership fact, not
-- a descriptive string.
--
-- Every other specification (barrel length, stock material, sights, product
-- highlights, etc.) is purely descriptive, never queried by business logic,
-- and stays in the flexible ProductSpecifications table below.
--
-- MILESTONE 6 CORRECTION — ProductUseCases (added when implementing Match)
-- -------------------------------------------------------------------------
-- Reading the actual old Match resolver (backend/match_routes.py
-- resolve_public_matches, plus backend/models.py to_storefront_airgun/
-- to_storefront_pellet) surfaced a real gap: the DERIVED pellet fallback
-- (used when an airgun has no curated pellet relationships) requires a
-- third condition beyond Calibre and the pellet-weight range that Milestone
-- 2 already captured — the pellet's "use_case" tag (exactly "target_10m" or
-- "plinking") must appear in the airgun's own list of match use cases:
--     p.use_case == airgun.calibre-independent use-case tag
--     AND p.use_case in airgun.match_use_cases
-- The airgun side is genuinely multi-valued (0-2 of the same two tags,
-- derived from its admin-set primary/secondary use cases), while the pellet
-- side is always exactly one tag. Following the exact pattern already used
-- for ProductCompatiblePowerplants (small structural multi-valued fact,
-- child table, no taxonomy/CRUD system), one small child table covers both:
-- an airgun row gets 0-2 rounds, a pellet row gets exactly 1. No CHECK
-- constraint is added, for the same reason MatchRelationshipUseCases has
-- none — this is a plain relationship-owned string tag, not a controlled
-- vocabulary service.
-- =============================================================================

DROP TABLE IF EXISTS dbo.ProductUseCases;
DROP TABLE IF EXISTS dbo.ProductCompatiblePowerplants;
DROP TABLE IF EXISTS dbo.ProductSpecifications;
DROP TABLE IF EXISTS dbo.ProductEnrichment;
GO

CREATE TABLE dbo.ProductEnrichment (
    Id                          INT IDENTITY(1,1)  NOT NULL,
    ShopifyProductId            NVARCHAR(64)        NOT NULL,   -- Shopify reference only; sized for GraphQL GID strings
    Category                    NVARCHAR(20)        NOT NULL,

    -- V1 visibility toggle only — NOT a duplicate of Shopify's product
    -- lifecycle. Lets an admin exclude an enrichment record from Match/
    -- Bundle resolution without deleting curated data. Deliberately a
    -- single boolean, not a Draft/Published/Archived state machine.
    IsActive                    BIT                 NOT NULL DEFAULT (1),

    -- Match-critical typed attributes (see reasoning above). Nullable
    -- because they are only meaningful for some categories (e.g. Calibre
    -- applies to airgun/pellet, not accessory; the weight-range pair and
    -- PowerplantType apply to airgun only; WeightGrains applies to pellet
    -- only). Category-appropriateness is enforced by application logic,
    -- not by the schema, to avoid a combinatorial CHECK constraint.
    Calibre                      NVARCHAR(20)        NULL,
    PowerplantType                NVARCHAR(30)        NULL,
    WeightGrains                  DECIMAL(6,2)        NULL,
    RecommendedPelletWeightMin     DECIMAL(6,2)        NULL,
    RecommendedPelletWeightMax     DECIMAL(6,2)        NULL,

    CreatedAt                   DATETIME2(3)         NOT NULL DEFAULT (SYSUTCDATETIME()),
    UpdatedAt                   DATETIME2(3)         NOT NULL DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT PK_ProductEnrichment PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_ProductEnrichment_ShopifyProductId UNIQUE (ShopifyProductId),
    CONSTRAINT CK_ProductEnrichment_Category CHECK (Category IN (N'airgun', N'pellet', N'accessory'))
);
GO

CREATE INDEX IX_ProductEnrichment_Category ON dbo.ProductEnrichment (Category);
GO

-- One-to-many: which powerplants an accessory (Category='accessory') is
-- compatible with. Same shape/justification as MatchRelationshipUseCases —
-- a small, genuinely multi-valued structural fact, not a taxonomy system.
CREATE TABLE dbo.ProductCompatiblePowerplants (
    ProductEnrichmentId   INT             NOT NULL,
    Powerplant            NVARCHAR(30)    NOT NULL,
    CONSTRAINT PK_ProductCompatiblePowerplants PRIMARY KEY CLUSTERED (ProductEnrichmentId, Powerplant),
    CONSTRAINT FK_ProductCompatiblePowerplants_Enrichment FOREIGN KEY (ProductEnrichmentId)
        REFERENCES dbo.ProductEnrichment (Id) ON DELETE CASCADE
);
GO

-- One-to-many: which "match use case" tags apply to this product (airgun:
-- 0-2 rows; pellet: exactly 1 row). See MILESTONE 6 CORRECTION note above.
CREATE TABLE dbo.ProductUseCases (
    ProductEnrichmentId   INT             NOT NULL,
    UseCase               NVARCHAR(30)    NOT NULL,
    CONSTRAINT PK_ProductUseCases PRIMARY KEY CLUSTERED (ProductEnrichmentId, UseCase),
    CONSTRAINT FK_ProductUseCases_Enrichment FOREIGN KEY (ProductEnrichmentId)
        REFERENCES dbo.ProductEnrichment (Id) ON DELETE CASCADE
);
GO

-- Flexible long-tail specifications. Never queried structurally by Match —
-- purely for display. Multi-valued keys (e.g. "sights", "product_highlights")
-- are modelled as multiple rows sharing the same SpecKey, ordered by
-- SortOrder — this is why SpecKey is deliberately NOT unique per product.
CREATE TABLE dbo.ProductSpecifications (
    Id                    INT IDENTITY(1,1)  NOT NULL,
    ProductEnrichmentId    INT                 NOT NULL,
    SpecKey                NVARCHAR(100)       NOT NULL,
    SpecValue              NVARCHAR(500)       NOT NULL,
    SortOrder              INT                 NOT NULL DEFAULT (0),
    CONSTRAINT PK_ProductSpecifications PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_ProductSpecifications_Enrichment FOREIGN KEY (ProductEnrichmentId)
        REFERENCES dbo.ProductEnrichment (Id) ON DELETE CASCADE
);
GO

CREATE INDEX IX_ProductSpecifications_Enrichment_Sort ON dbo.ProductSpecifications (ProductEnrichmentId, SortOrder);
CREATE INDEX IX_ProductSpecifications_Enrichment_Key ON dbo.ProductSpecifications (ProductEnrichmentId, SpecKey);
GO
