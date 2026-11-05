USE ImportOpsDB;
GO

SET XACT_ABORT ON;
GO

/* ============================================================================
   QUOTATION MODULE PATCH
============================================================================ */

IF COL_LENGTH('sales.QUOTATION_ITEM', 'tax_rate') IS NULL
BEGIN
    ALTER TABLE sales.QUOTATION_ITEM
        ADD tax_rate DECIMAL(9,4) NOT NULL
            CONSTRAINT DF_QUOTE_ITEM_TAX_RATE DEFAULT (0);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.check_constraints
    WHERE name = N'CK_QUOTE_ITEM_TAX_RATE'
)
BEGIN
    ALTER TABLE sales.QUOTATION_ITEM
    ADD CONSTRAINT CK_QUOTE_ITEM_TAX_RATE
        CHECK (tax_rate >= 0 AND tax_rate <= 100);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'sales.QUOTATION_STATUS_HISTORY')
      AND name = N'IX_QUOTE_STATUS_HISTORY_QUOTE_DATE'
)
BEGIN
    CREATE INDEX IX_QUOTE_STATUS_HISTORY_QUOTE_DATE
        ON sales.QUOTATION_STATUS_HISTORY(quotation_id, changed_at DESC);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'sales.QUOTATION')
      AND name = N'IX_QUOTATION_COMPANY_STATUS_DATE'
)
BEGIN
    CREATE INDEX IX_QUOTATION_COMPANY_STATUS_DATE
        ON sales.QUOTATION(company_id, status, issue_date DESC)
        INCLUDE(request_id, client_id, version_no, total_amount, valid_until);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'sales.QUOTATION')
      AND name = N'IX_QUOTATION_REQUEST_VERSION'
)
BEGIN
    CREATE INDEX IX_QUOTATION_REQUEST_VERSION
        ON sales.QUOTATION(request_id, version_no DESC)
        INCLUDE(status, quotation_no, total_amount, valid_until);
END;
GO

/* Protección adicional: una sola cotización APPROVED por solicitud. */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'sales.QUOTATION')
      AND name = N'UX_QUOTATION_ONE_APPROVED_PER_REQUEST'
)
BEGIN
    CREATE UNIQUE INDEX UX_QUOTATION_ONE_APPROVED_PER_REQUEST
        ON sales.QUOTATION(request_id)
        WHERE status = 'APPROVED' AND is_deleted = 0;
END;
GO
