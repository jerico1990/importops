USE [ImportOpsDB];
GO
SET XACT_ABORT ON;
GO

BEGIN TRY
    BEGIN TRAN;

    IF OBJECT_ID(N'cfg.ALERT_RULE', N'U') IS NULL
    BEGIN
        CREATE TABLE cfg.ALERT_RULE(
            alert_rule_id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_ALERT_RULE PRIMARY KEY,
            company_id bigint NOT NULL,
            alert_type varchar(60) NOT NULL,
            name nvarchar(140) NOT NULL,
            description nvarchar(500) NULL,
            severity varchar(20) NOT NULL,
            audience varchar(20) NOT NULL,
            threshold_hours int NULL,
            is_active bit NOT NULL CONSTRAINT DF_ALERT_RULE_ACTIVE DEFAULT(1),
            created_at datetime2(0) NOT NULL CONSTRAINT DF_ALERT_RULE_CREATED DEFAULT(SYSUTCDATETIME()),
            modified_at datetime2(0) NULL,
            CONSTRAINT CK_ALERT_RULE_SEVERITY CHECK (severity IN ('INFO','WARNING','CRITICAL')),
            CONSTRAINT CK_ALERT_RULE_AUDIENCE CHECK (audience IN ('INTERNAL','CLIENT','BOTH')),
            CONSTRAINT CK_ALERT_RULE_THRESHOLD CHECK (threshold_hours IS NULL OR threshold_hours >= 0),
            CONSTRAINT FK_ALERT_RULE_COMPANY FOREIGN KEY(company_id) REFERENCES cfg.COMPANY(company_id),
            CONSTRAINT UQ_ALERT_RULE_COMPANY_TYPE UNIQUE(company_id, alert_type)
        );
    END;

    IF OBJECT_ID(N'ops.ALERT', N'U') IS NULL
    BEGIN
        CREATE TABLE ops.ALERT(
            alert_id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_ALERT PRIMARY KEY,
            company_id bigint NOT NULL,
            alert_rule_id int NOT NULL,
            alert_type varchar(60) NOT NULL,
            severity varchar(20) NOT NULL,
            audience varchar(20) NOT NULL,
            operation_id bigint NULL,
            client_id bigint NULL,
            responsible_user_id bigint NULL,
            required_permission varchar(80) NULL,
            title nvarchar(180) NOT NULL,
            message nvarchar(1000) NOT NULL,
            source_type varchar(40) NOT NULL,
            source_id bigint NULL,
            action_path nvarchar(300) NULL,
            dedup_key varchar(220) NOT NULL,
            status varchar(20) NOT NULL CONSTRAINT DF_ALERT_STATUS DEFAULT('ACTIVE'),
            triggered_at datetime2(0) NOT NULL,
            last_detected_at datetime2(0) NOT NULL,
            resolved_at datetime2(0) NULL,
            created_at datetime2(0) NOT NULL CONSTRAINT DF_ALERT_CREATED DEFAULT(SYSUTCDATETIME()),
            modified_at datetime2(0) NULL,
            CONSTRAINT CK_ALERT_SEVERITY CHECK (severity IN ('INFO','WARNING','CRITICAL')),
            CONSTRAINT CK_ALERT_AUDIENCE CHECK (audience IN ('INTERNAL','CLIENT','BOTH')),
            CONSTRAINT CK_ALERT_STATUS CHECK (status IN ('ACTIVE','RESOLVED')),
            CONSTRAINT FK_ALERT_COMPANY FOREIGN KEY(company_id) REFERENCES cfg.COMPANY(company_id),
            CONSTRAINT FK_ALERT_RULE FOREIGN KEY(alert_rule_id) REFERENCES cfg.ALERT_RULE(alert_rule_id),
            CONSTRAINT FK_ALERT_OPERATION FOREIGN KEY(operation_id) REFERENCES ops.IMPORT_OPERATION(operation_id),
            CONSTRAINT FK_ALERT_CLIENT FOREIGN KEY(client_id) REFERENCES crm.CLIENT(client_id),
            CONSTRAINT FK_ALERT_RESP_USER FOREIGN KEY(responsible_user_id) REFERENCES sec.APP_USER(user_id)
        );
        CREATE UNIQUE INDEX UX_ALERT_ACTIVE_DEDUP ON ops.ALERT(company_id, dedup_key) WHERE status='ACTIVE';
        CREATE INDEX IX_ALERT_COMPANY_STATUS_DATE ON ops.ALERT(company_id, status, triggered_at DESC);
        CREATE INDEX IX_ALERT_OPERATION ON ops.ALERT(operation_id, status) WHERE operation_id IS NOT NULL;
        CREATE INDEX IX_ALERT_CLIENT ON ops.ALERT(client_id, status) WHERE client_id IS NOT NULL;
        CREATE INDEX IX_ALERT_RESPONSIBLE ON ops.ALERT(responsible_user_id, status) WHERE responsible_user_id IS NOT NULL;
    END;

    IF OBJECT_ID(N'ops.ALERT_USER_STATE', N'U') IS NULL
    BEGIN
        CREATE TABLE ops.ALERT_USER_STATE(
            alert_id bigint NOT NULL,
            user_id bigint NOT NULL,
            is_read bit NOT NULL CONSTRAINT DF_ALERT_USER_READ DEFAULT(0),
            read_at datetime2(0) NULL,
            is_dismissed bit NOT NULL CONSTRAINT DF_ALERT_USER_DISMISSED DEFAULT(0),
            dismissed_at datetime2(0) NULL,
            CONSTRAINT PK_ALERT_USER_STATE PRIMARY KEY(alert_id, user_id),
            CONSTRAINT FK_ALERT_USER_STATE_ALERT FOREIGN KEY(alert_id) REFERENCES ops.ALERT(alert_id) ON DELETE CASCADE,
            CONSTRAINT FK_ALERT_USER_STATE_USER FOREIGN KEY(user_id) REFERENCES sec.APP_USER(user_id)
        );
        CREATE INDEX IX_ALERT_USER_STATE_USER ON ops.ALERT_USER_STATE(user_id, is_read, is_dismissed);
    END;

    IF NOT EXISTS (SELECT 1 FROM sec.PERMISSION WHERE code='ALERTS.VIEW')
        INSERT sec.PERMISSION(code,name,module_code,is_active) VALUES('ALERTS.VIEW',N'Ver alertas y notificaciones','ALERTS',1);
    IF NOT EXISTS (SELECT 1 FROM sec.PERMISSION WHERE code='ALERTS.MANAGE')
        INSERT sec.PERMISSION(code,name,module_code,is_active) VALUES('ALERTS.MANAGE',N'Configurar reglas de alertas','ALERTS',1);

    DECLARE @pView int=(SELECT permission_id FROM sec.PERMISSION WHERE code='ALERTS.VIEW');
    DECLARE @pManage int=(SELECT permission_id FROM sec.PERMISSION WHERE code='ALERTS.MANAGE');

    INSERT sec.ROLE_PERMISSION(role_id,permission_id,can_view,can_create,can_update,can_delete,can_approve)
    SELECT r.role_id,@pView,1,0,0,0,0 FROM sec.ROLE r
    WHERE r.is_active=1 AND NOT EXISTS(SELECT 1 FROM sec.ROLE_PERMISSION rp WHERE rp.role_id=r.role_id AND rp.permission_id=@pView);

    INSERT sec.ROLE_PERMISSION(role_id,permission_id,can_view,can_create,can_update,can_delete,can_approve)
    SELECT r.role_id,@pManage,1,1,1,0,0 FROM sec.ROLE r
    WHERE r.is_active=1 AND r.code='ADMIN'
      AND NOT EXISTS(SELECT 1 FROM sec.ROLE_PERMISSION rp WHERE rp.role_id=r.role_id AND rp.permission_id=@pManage);

    ;WITH R(alert_type,name,description,severity,audience,threshold_hours) AS (
        SELECT * FROM (VALUES
        ('REQUEST_PENDING_REVIEW',N'Nueva solicitud pendiente de revisión',N'Solicitud enviada por el cliente y pendiente de revisión interna.','INFO','INTERNAL',NULL),
        ('REQUEST_OBSERVED',N'Solicitud observada',N'Solicitud observada que requiere subsanación del cliente.','WARNING','CLIENT',NULL),
        ('QUOTATION_PENDING_APPROVAL',N'Cotización pendiente de aprobación',N'Cotización enviada al cliente pendiente de decisión.','WARNING','BOTH',24),
        ('QUOTATION_REJECTED',N'Cotización rechazada por el cliente',N'Cotización rechazada que requiere revisión comercial y posible nueva versión.','WARNING','INTERNAL',NULL),
        ('QUOTATION_APPROVED_OPERATION_PENDING',N'Cotización aprobada - operación pendiente',N'Cotización aprobada por el cliente que todavía no tiene operación creada.','WARNING','INTERNAL',NULL),
        ('OPERATION_ON_HOLD',N'Operación puesta en espera',N'Operación temporalmente en espera.','WARNING','BOTH',NULL),
        ('OPERATION_CANCELLED',N'Operación cancelada',N'Notificación de cancelación de operación; permanece activa durante el umbral configurado y luego pasa al historial.','WARNING','BOTH',72),
        ('TASK_DUE_SOON',N'Tarea próxima a vencer',N'Tarea operativa que vencerá dentro del umbral configurado.','WARNING','INTERNAL',24),
        ('TASK_OVERDUE',N'Tarea vencida',N'Tarea operativa pendiente cuya fecha límite ya venció.','CRITICAL','INTERNAL',NULL),
        ('SHIPMENT_ETA_SOON',N'Embarque próximo a arribar',N'Embarque sin arribo real cuya ETA está próxima.','WARNING','BOTH',48),
        ('SHIPMENT_ETA_OVERDUE',N'ETA vencida',N'Embarque cuya ETA venció y todavía no registra arribo real.','CRITICAL','BOTH',NULL),
        ('SHIPMENT_ARRIVED',N'Arribo confirmado',N'El embarque registró arribo real.','INFO','BOTH',NULL),
        ('DOCUMENT_OBSERVED',N'Documento observado',N'Documento con observación abierta.','WARNING','BOTH',NULL),
        ('DOCUMENT_REQUIRED_MISSING',N'Documento obligatorio pendiente',N'Requisito documental obligatorio de la etapa actual sin documento válido.','WARNING','BOTH',NULL),
        ('CUSTOMS_OBSERVATION_OPEN',N'Observación aduanera abierta',N'Existe una observación aduanera pendiente de resolver.','WARNING','INTERNAL',NULL),
        ('CUSTOMS_RELEASE_PENDING',N'Levante pendiente',N'Proceso aduanero sin levante luego del umbral configurado.','WARNING','INTERNAL',48),
        ('CUSTOMS_RELEASED',N'Levante autorizado',N'El proceso aduanero registró levante.','INFO','BOTH',NULL),
        ('SETTLEMENT_PENDING',N'Rendición pendiente',N'Operación entregada con rendición todavía no aprobada.','WARNING','INTERNAL',24),
        ('OPERATION_READY_TO_CLOSE',N'Operación lista para cierre',N'La operación cumple las condiciones de cierre y permanece abierta.','INFO','INTERNAL',NULL)
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

SELECT 'ALERTS V1.14 OK' AS result,
       (SELECT COUNT(*) FROM cfg.ALERT_RULE) AS rules,
       (SELECT COUNT(*) FROM sec.PERMISSION WHERE code IN ('ALERTS.VIEW','ALERTS.MANAGE')) AS permissions;
GO
