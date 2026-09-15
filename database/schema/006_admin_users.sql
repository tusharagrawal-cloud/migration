SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- =============================================================================
-- ONE77 Migration — Schema 006: Admin Users
-- =============================================================================
-- Minimum V1 admin auth model, approved at the Phase 3 Section 11 checkpoint
-- (Milestone 9): JWT bearer authentication, single/small-admin system, no
-- roles/permissions, no sessions table (JWT is stateless — no server-side
-- session state to persist), no refresh tokens, no audit/login-history
-- table, no MFA. PasswordHash stores a BCrypt hash only — passwords are
-- never stored plaintext or reversibly encrypted. IsActive lets an admin be
-- disabled without deleting the row (and without needing a status enum).
-- =============================================================================

DROP TABLE IF EXISTS dbo.AdminUsers;
GO

CREATE TABLE dbo.AdminUsers (
    AdminUserId    INT IDENTITY(1,1)  NOT NULL,
    Email          NVARCHAR(256)      NOT NULL,
    PasswordHash   NVARCHAR(256)      NOT NULL,
    Name           NVARCHAR(200)      NOT NULL,
    IsActive       BIT                NOT NULL DEFAULT (1),
    CreatedAt      DATETIME2(3)       NOT NULL DEFAULT (SYSUTCDATETIME()),
    UpdatedAt      DATETIME2(3)       NOT NULL DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT PK_AdminUsers PRIMARY KEY CLUSTERED (AdminUserId),
    CONSTRAINT UQ_AdminUsers_Email UNIQUE (Email)
);
GO
