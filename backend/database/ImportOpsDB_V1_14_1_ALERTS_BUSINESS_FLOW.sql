USE [ImportOpsDB];
GO
SET XACT_ABORT ON;
GO

BEGIN TRY
    BEGIN TRAN;

    IF OBJECT_ID(N'cfg.ALERT_RULE', N'U') IS NULL
        THROW 50141, 'Debe ejecutar primero ImportOpsDB_V1_14_ALERTS_NOTIFICATIONS.sql.', 1;

    ;WITH R(alert_type,name,description,severity,audience,threshold_hours) AS (
        SELECT * FROM (VALUES
        ('REQUEST_PENDING_REVIEW',N'Nueva solicitud pendiente de revisión',N'Solicitud enviada por el cliente y pendiente de revisión interna.','INFO','INTERNAL',NULL),
        ('REQUEST_OBSERVED',N'Solicitud observada',N'Solicitud observada que requiere subsanación del cliente.','WARNING','CLIENT',NULL),
        ('QUOTATION_REJECTED',N'Cotización rechazada por el cliente',N'Cotización rechazada que requiere revisión comercial y posible nueva versión.','WARNING','INTERNAL',NULL),
        ('QUOTATION_APPROVED_OPERATION_PENDING',N'Cotización aprobada - operación pendiente',N'Cotización aprobada por el cliente que todavía no tiene operación creada.','WARNING','INTERNAL',NULL),
        ('OPERATION_ON_HOLD',N'Operación puesta en espera',N'Operación temporalmente en espera.','WARNING','BOTH',NULL),
        ('OPERATION_CANCELLED',N'Operación cancelada',N'Notificación de cancelación de operación; permanece activa durante el umbral configurado y luego pasa al historial.','WARNING','BOTH',72)
        )v(alert_type,name,description,severity,audience,threshold_hours)
    )
    INSERT cfg.ALERT_RULE(company_id,alert_type,name,description,severity,audience,threshold_hours,is_active)
    SELECT c.company_id,R.alert_type,R.name,R.description,R.severity,R.audience,R.threshold_hours,1
    FROM cfg.COMPANY c CROSS JOIN R
    WHERE NOT EXISTS(SELECT 1 FROM cfg.ALERT_RULE ar WHERE ar.company_id=c.company_id AND ar.alert_type=R.alert_type);

    COMMIT;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT>0 ROLLBACK;
    THROW;
END CATCH;
GO

SELECT 'ALERTS V1.14.1 BUSINESS FLOW OK' AS result,
       COUNT(*) AS business_flow_rules
FROM cfg.ALERT_RULE
WHERE alert_type IN ('REQUEST_PENDING_REVIEW','REQUEST_OBSERVED','QUOTATION_REJECTED','QUOTATION_APPROVED_OPERATION_PENDING','OPERATION_ON_HOLD','OPERATION_CANCELLED');
GO
