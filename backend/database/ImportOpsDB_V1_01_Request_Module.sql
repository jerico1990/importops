USE ImportOpsDB;
GO

SET XACT_ABORT ON;
GO

/*
  Usuario técnico temporal para desarrollo.
  La autenticación real sustituirá App:DefaultUserId / X-User-Id.
*/
DECLARE @company_id BIGINT =
(
    SELECT TOP (1) company_id
    FROM cfg.COMPANY
    WHERE is_deleted = 0
    ORDER BY company_id
);

IF @company_id IS NULL
    THROW 50001, 'No existe una empresa configurada en cfg.COMPANY.', 1;

IF NOT EXISTS
(
    SELECT 1
    FROM sec.APP_USER
    WHERE company_id = @company_id
      AND username = N'system.dev'
      AND is_deleted = 0
)
BEGIN
    INSERT INTO sec.APP_USER
    (
        company_id,
        username,
        email,
        first_name,
        last_name,
        is_active,
        created_at,
        is_deleted
    )
    VALUES
    (
        @company_id,
        N'system.dev',
        N'dev@local',
        N'System',
        N'Development',
        1,
        SYSUTCDATETIME(),
        0
    );
END;

/* Índice útil para leer el timeline de estados. */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'sales.REQUEST_STATUS_HISTORY')
      AND name = N'IX_REQUEST_STATUS_HISTORY_REQUEST_DATE'
)
BEGIN
    CREATE INDEX IX_REQUEST_STATUS_HISTORY_REQUEST_DATE
        ON sales.REQUEST_STATUS_HISTORY(request_id, changed_at DESC);
END;

SELECT
    user_id AS DefaultUserId,
    company_id AS DefaultCompanyId,
    username
FROM sec.APP_USER
WHERE company_id = @company_id
  AND username = N'system.dev'
  AND is_deleted = 0;
GO
