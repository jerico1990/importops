-- Ejecutar una sola vez en Azure SQL / SQL Server, antes de publicar el backend.
IF COL_LENGTH('ops.IMPORT_OPERATION', 'financing_days') IS NULL
    ALTER TABLE ops.IMPORT_OPERATION ADD financing_days int NULL;
GO
IF COL_LENGTH('fin.ISSUED_LIQUIDATION', 'financing_days') IS NULL
    ALTER TABLE fin.ISSUED_LIQUIDATION ADD financing_days int NOT NULL CONSTRAINT DF_ISSUED_LIQUIDATION_FINANCING_DAYS DEFAULT (0);
GO
IF COL_LENGTH('fin.ISSUED_LIQUIDATION', 'sent_at') IS NULL
    ALTER TABLE fin.ISSUED_LIQUIDATION ADD sent_at datetime2 NULL;
IF COL_LENGTH('fin.ISSUED_LIQUIDATION', 'sent_by') IS NULL
    ALTER TABLE fin.ISSUED_LIQUIDATION ADD sent_by bigint NULL;
IF COL_LENGTH('fin.ISSUED_LIQUIDATION', 'sent_via') IS NULL
    ALTER TABLE fin.ISSUED_LIQUIDATION ADD sent_via nvarchar(40) NULL;
IF COL_LENGTH('fin.ISSUED_LIQUIDATION', 'sent_to') IS NULL
    ALTER TABLE fin.ISSUED_LIQUIDATION ADD sent_to nvarchar(250) NULL;
IF COL_LENGTH('fin.ISSUED_LIQUIDATION', 'due_at') IS NULL
    ALTER TABLE fin.ISSUED_LIQUIDATION ADD due_at datetime2 NULL;
GO
-- No se infieren fechas de envío para liquidaciones anteriores.
-- Los registros ya emitidos reciben 0 días hasta su revisión manual.
