-- Milestone 1 connectivity check only. Not part of the domain schema.
-- Proves DDL/DML can be executed end-to-end against the real SQL Server 2019
-- instance from this pipeline. Milestone 2 owns the actual ONE77 schema.
--
-- Safe to run repeatedly (idempotent): drops its own throwaway table first.

IF OBJECT_ID('dbo.__One77_ConnectivityCheck', 'U') IS NOT NULL
    DROP TABLE dbo.__One77_ConnectivityCheck;

CREATE TABLE dbo.__One77_ConnectivityCheck (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    CheckedAt DATETIME2(3) NOT NULL DEFAULT (SYSUTCDATETIME())
);

INSERT INTO dbo.__One77_ConnectivityCheck DEFAULT VALUES;

SELECT TOP (1) Id, CheckedAt FROM dbo.__One77_ConnectivityCheck ORDER BY Id DESC;

DROP TABLE dbo.__One77_ConnectivityCheck;
