USE [ImportOpsDB];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    /* =========================================================
       1. Nombre funcional de la etapa final previa al cierre
       ========================================================= */
    UPDATE cfg.WORKFLOW_STAGE
       SET name = N'Rendición y cierre'
     WHERE code = 'SETTLEMENT'
       AND is_active = 1
       AND name <> N'Rendición y cierre';

    /* =========================================================
       2. Recalcular estado documental de operaciones existentes
          según requisitos OBLIGATORIOS de su etapa actual.

          OBSERVED:
            existe documento OBSERVED de un tipo obligatorio
            para la etapa actual.

          PENDING:
            falta al menos un documento VALID no vencido
            de un tipo obligatorio.

          COMPLETE:
            no existen requisitos obligatorios, o todos están
            cubiertos por documentos VALID no vencidos.
       ========================================================= */
    IF OBJECT_ID('tempdb..#DOC_STATUS_FIX') IS NOT NULL
        DROP TABLE #DOC_STATUS_FIX;

    CREATE TABLE #DOC_STATUS_FIX
    (
        operation_id BIGINT NOT NULL PRIMARY KEY,
        old_status VARCHAR(30) NOT NULL,
        new_status VARCHAR(30) NOT NULL,
        changed_by BIGINT NOT NULL
    );

    INSERT INTO #DOC_STATUS_FIX(operation_id, old_status, new_status, changed_by)
    SELECT
        o.operation_id,
        o.documentary_status,
        CASE
            WHEN EXISTS
            (
                SELECT 1
                FROM cfg.STAGE_DOCUMENT_REQUIREMENT r
                JOIN doc.DOCUMENT d
                  ON d.company_id = o.company_id
                 AND d.operation_id = o.operation_id
                 AND d.document_type_id = r.document_type_id
                 AND d.is_deleted = 0
                 AND d.document_status = 'OBSERVED'
                WHERE r.company_id = o.company_id
                  AND r.workflow_stage_id = o.current_stage_id
                  AND r.is_active = 1
                  AND r.is_mandatory = 1
            )
            THEN 'OBSERVED'

            WHEN EXISTS
            (
                SELECT 1
                FROM cfg.STAGE_DOCUMENT_REQUIREMENT r
                WHERE r.company_id = o.company_id
                  AND r.workflow_stage_id = o.current_stage_id
                  AND r.is_active = 1
                  AND r.is_mandatory = 1
                  AND NOT EXISTS
                  (
                      SELECT 1
                      FROM doc.DOCUMENT d
                      WHERE d.company_id = o.company_id
                        AND d.operation_id = o.operation_id
                        AND d.document_type_id = r.document_type_id
                        AND d.is_deleted = 0
                        AND d.document_status = 'VALID'
                        AND (
                            d.expiration_date IS NULL
                            OR d.expiration_date >= CONVERT(date, GETDATE())
                        )
                  )
            )
            THEN 'PENDING'

            ELSE 'COMPLETE'
        END,
        COALESCE(o.modified_by, o.created_by)
    FROM ops.IMPORT_OPERATION o
    WHERE o.is_deleted = 0;

    INSERT INTO ops.OPERATION_STATUS_HISTORY
    (
        operation_id,
        status_dimension,
        old_status,
        new_status,
        comment,
        changed_at,
        changed_by
    )
    SELECT
        f.operation_id,
        'DOCUMENTARY',
        f.old_status,
        f.new_status,
        N'Corrección V1.13: recalculado según requisitos documentales obligatorios de la etapa actual.',
        SYSUTCDATETIME(),
        f.changed_by
    FROM #DOC_STATUS_FIX f
    WHERE f.old_status <> f.new_status;

    UPDATE o
       SET o.documentary_status = f.new_status,
           o.modified_at = SYSUTCDATETIME(),
           o.modified_by = f.changed_by
      FROM ops.IMPORT_OPERATION o
      JOIN #DOC_STATUS_FIX f
        ON f.operation_id = o.operation_id
     WHERE o.documentary_status <> f.new_status;

    COMMIT TRANSACTION;

    SELECT
        ws.code,
        ws.name,
        ws.display_order,
        ws.is_final
    FROM cfg.WORKFLOW_STAGE ws
    WHERE ws.code IN ('SETTLEMENT', 'CLOSED')
    ORDER BY ws.display_order;

    SELECT
        o.operation_id,
        o.operation_no,
        ws.code AS current_stage_code,
        ws.name AS current_stage_name,
        o.operational_status,
        o.documentary_status,
        o.financial_status
    FROM ops.IMPORT_OPERATION o
    JOIN cfg.WORKFLOW_STAGE ws
      ON ws.workflow_stage_id = o.current_stage_id
    WHERE o.is_deleted = 0
    ORDER BY o.operation_id DESC;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;
GO
