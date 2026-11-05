USE ImportOpsDB;
GO

SET XACT_ABORT ON;
GO

/* ============================================================================
   V7 - CUSTOMS
============================================================================ */

IF COL_LENGTH('ops.CUSTOMS_PROCESS', 'shipment_id') IS NULL
    ALTER TABLE ops.CUSTOMS_PROCESS ADD shipment_id BIGINT NULL;
GO

IF COL_LENGTH('ops.CUSTOMS_PROCESS', 'duties_exchange_rate') IS NULL
    ALTER TABLE ops.CUSTOMS_PROCESS ADD duties_exchange_rate DECIMAL(18,8) NULL;
GO

IF COL_LENGTH('ops.CUSTOMS_PROCESS', 'duties_base_amount') IS NULL
    ALTER TABLE ops.CUSTOMS_PROCESS ADD duties_base_amount DECIMAL(19,4) NULL;
GO

IF NOT EXISTS
(
    SELECT 1 FROM sys.foreign_keys
    WHERE name = N'FK_CUSTOMS_PROCESS_SHIPMENT'
)
BEGIN
    ALTER TABLE ops.CUSTOMS_PROCESS
    ADD CONSTRAINT FK_CUSTOMS_PROCESS_SHIPMENT
        FOREIGN KEY(shipment_id) REFERENCES ops.SHIPMENT(shipment_id);
END;
GO

IF NOT EXISTS
(
    SELECT 1 FROM sys.foreign_keys
    WHERE name = N'FK_CUSTOMS_PROCESS_CREATED_BY'
)
BEGIN
    ALTER TABLE ops.CUSTOMS_PROCESS
    ADD CONSTRAINT FK_CUSTOMS_PROCESS_CREATED_BY
        FOREIGN KEY(created_by) REFERENCES sec.APP_USER(user_id);
END;
GO

IF NOT EXISTS
(
    SELECT 1 FROM sys.foreign_keys
    WHERE name = N'FK_CUSTOMS_PROCESS_MODIFIED_BY'
)
BEGIN
    ALTER TABLE ops.CUSTOMS_PROCESS
    ADD CONSTRAINT FK_CUSTOMS_PROCESS_MODIFIED_BY
        FOREIGN KEY(modified_by) REFERENCES sec.APP_USER(user_id);
END;
GO

IF NOT EXISTS
(
    SELECT 1 FROM sys.check_constraints
    WHERE name = N'CK_CUSTOMS_PROCESS_EXCHANGE_RATE'
)
BEGIN
    ALTER TABLE ops.CUSTOMS_PROCESS
    ADD CONSTRAINT CK_CUSTOMS_PROCESS_EXCHANGE_RATE
        CHECK(duties_exchange_rate IS NULL OR duties_exchange_rate > 0);
END;
GO

IF OBJECT_ID(N'ops.CUSTOMS_OBSERVATION', N'U') IS NULL
BEGIN
    CREATE TABLE ops.CUSTOMS_OBSERVATION
    (
        customs_observation_id BIGINT IDENTITY(1,1) PRIMARY KEY,
        customs_process_id     BIGINT NOT NULL,
        observation_type       VARCHAR(30) NOT NULL,
        severity               VARCHAR(10) NOT NULL,
        description            NVARCHAR(1500) NOT NULL,
        observation_status     VARCHAR(20) NOT NULL DEFAULT('OPEN'),
        observed_at            DATETIME2(0) NOT NULL,
        observed_by            BIGINT NOT NULL,
        resolved_at            DATETIME2(0) NULL,
        resolved_by            BIGINT NULL,
        resolution_note        NVARCHAR(1500) NULL,
        operation_task_id      BIGINT NULL,
        created_at             DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
        modified_at            DATETIME2(0) NULL,
        row_ver                ROWVERSION,

        CONSTRAINT FK_CUSTOMS_OBS_PROCESS
            FOREIGN KEY(customs_process_id) REFERENCES ops.CUSTOMS_PROCESS(customs_process_id),
        CONSTRAINT FK_CUSTOMS_OBS_USER
            FOREIGN KEY(observed_by) REFERENCES sec.APP_USER(user_id),
        CONSTRAINT FK_CUSTOMS_OBS_RESOLVED_USER
            FOREIGN KEY(resolved_by) REFERENCES sec.APP_USER(user_id),
        CONSTRAINT FK_CUSTOMS_OBS_TASK
            FOREIGN KEY(operation_task_id) REFERENCES ops.OPERATION_TASK(operation_task_id),

        CONSTRAINT CK_CUSTOMS_OBS_TYPE CHECK(observation_type IN(
            'DOCUMENTARY','VALUATION','CLASSIFICATION','INSPECTION','AUTHORITY_REQUIREMENT','OTHER')),
        CONSTRAINT CK_CUSTOMS_OBS_SEVERITY CHECK(severity IN(
            'LOW','NORMAL','HIGH','CRITICAL')),
        CONSTRAINT CK_CUSTOMS_OBS_STATUS CHECK(observation_status IN(
            'OPEN','RESOLVED','CANCELLED'))
    );
END;
GO

IF OBJECT_ID(N'ops.CUSTOMS_TAX', N'U') IS NULL
BEGIN
    CREATE TABLE ops.CUSTOMS_TAX
    (
        customs_tax_id     BIGINT IDENTITY(1,1) PRIMARY KEY,
        customs_process_id BIGINT NOT NULL,
        tax_type           VARCHAR(30) NOT NULL,
        description        NVARCHAR(300) NOT NULL,
        amount             DECIMAL(19,4) NOT NULL,
        currency_code      CHAR(3) NOT NULL,
        exchange_rate      DECIMAL(18,8) NULL,
        base_amount        DECIMAL(19,4) NOT NULL,
        payment_status     VARCHAR(20) NOT NULL DEFAULT('PENDING'),
        payment_reference  NVARCHAR(120) NULL,
        paid_at            DATETIME2(0) NULL,
        notes              NVARCHAR(1000) NULL,
        created_at         DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
        created_by         BIGINT NOT NULL,
        modified_at        DATETIME2(0) NULL,
        modified_by        BIGINT NULL,
        row_ver            ROWVERSION,

        CONSTRAINT FK_CUSTOMS_TAX_PROCESS
            FOREIGN KEY(customs_process_id) REFERENCES ops.CUSTOMS_PROCESS(customs_process_id),
        CONSTRAINT FK_CUSTOMS_TAX_CURR
            FOREIGN KEY(currency_code) REFERENCES cfg.CURRENCY(currency_code),
        CONSTRAINT FK_CUSTOMS_TAX_CREATED
            FOREIGN KEY(created_by) REFERENCES sec.APP_USER(user_id),
        CONSTRAINT FK_CUSTOMS_TAX_MODIFIED
            FOREIGN KEY(modified_by) REFERENCES sec.APP_USER(user_id),

        CONSTRAINT CK_CUSTOMS_TAX_TYPE CHECK(tax_type IN(
            'AD_VALOREM','IGV','IPM','ISC','PERCEPTION','ANTIDUMPING','OTHER')),
        CONSTRAINT CK_CUSTOMS_TAX_PAYMENT CHECK(payment_status IN(
            'PENDING','PAID','EXEMPT','VOID')),
        CONSTRAINT CK_CUSTOMS_TAX_AMOUNT CHECK(amount >= 0 AND base_amount >= 0),
        CONSTRAINT CK_CUSTOMS_TAX_EXCHANGE CHECK(exchange_rate IS NULL OR exchange_rate > 0)
    );
END;
GO

IF OBJECT_ID(N'ops.CUSTOMS_STATUS_HISTORY', N'U') IS NULL
BEGIN
    CREATE TABLE ops.CUSTOMS_STATUS_HISTORY
    (
        customs_status_history_id BIGINT IDENTITY(1,1) PRIMARY KEY,
        customs_process_id        BIGINT NOT NULL,
        old_status                VARCHAR(30) NULL,
        new_status                VARCHAR(30) NOT NULL,
        comment                   NVARCHAR(1000) NULL,
        changed_at                DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
        changed_by                BIGINT NOT NULL,

        CONSTRAINT FK_CUSTOMS_HIST_PROCESS
            FOREIGN KEY(customs_process_id) REFERENCES ops.CUSTOMS_PROCESS(customs_process_id),
        CONSTRAINT FK_CUSTOMS_HIST_USER
            FOREIGN KEY(changed_by) REFERENCES sec.APP_USER(user_id)
    );
END;
GO

/* Un embarque tiene un único expediente aduanero V1. */
IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'ops.CUSTOMS_PROCESS')
      AND name = N'UX_CUSTOMS_PROCESS_SHIPMENT'
)
BEGIN
    CREATE UNIQUE INDEX UX_CUSTOMS_PROCESS_SHIPMENT
        ON ops.CUSTOMS_PROCESS(shipment_id)
        WHERE shipment_id IS NOT NULL;
END;
GO

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'ops.CUSTOMS_PROCESS')
      AND name = N'IX_CUSTOMS_PROCESS_OPERATION_STATUS'
)
BEGIN
    CREATE INDEX IX_CUSTOMS_PROCESS_OPERATION_STATUS
        ON ops.CUSTOMS_PROCESS(company_id, operation_id, customs_status)
        INCLUDE(shipment_id, declaration_no, customs_channel, release_date);
END;
GO

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'ops.CUSTOMS_OBSERVATION')
      AND name = N'IX_CUSTOMS_OBSERVATION_OPEN'
)
BEGIN
    CREATE INDEX IX_CUSTOMS_OBSERVATION_OPEN
        ON ops.CUSTOMS_OBSERVATION(customs_process_id, observation_status, observed_at DESC);
END;
GO

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'ops.CUSTOMS_TAX')
      AND name = N'IX_CUSTOMS_TAX_PROCESS'
)
BEGIN
    CREATE INDEX IX_CUSTOMS_TAX_PROCESS
        ON ops.CUSTOMS_TAX(customs_process_id, payment_status, tax_type);
END;
GO

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'ops.CUSTOMS_STATUS_HISTORY')
      AND name = N'IX_CUSTOMS_STATUS_HISTORY_PROCESS_DATE'
)
BEGIN
    CREATE INDEX IX_CUSTOMS_STATUS_HISTORY_PROCESS_DATE
        ON ops.CUSTOMS_STATUS_HISTORY(customs_process_id, changed_at DESC);
END;
GO

/* Permisos */
IF NOT EXISTS(SELECT 1 FROM sec.PERMISSION WHERE code='CUSTOMS.VIEW')
    INSERT INTO sec.PERMISSION(code,name,module_code,is_active)
    VALUES('CUSTOMS.VIEW',N'Ver gestión aduanera','CUSTOMS',1);

IF NOT EXISTS(SELECT 1 FROM sec.PERMISSION WHERE code='CUSTOMS.MANAGE')
    INSERT INTO sec.PERMISSION(code,name,module_code,is_active)
    VALUES('CUSTOMS.MANAGE',N'Gestionar proceso aduanero','CUSTOMS',1);

IF NOT EXISTS(SELECT 1 FROM sec.PERMISSION WHERE code='CUSTOMS.RELEASE')
    INSERT INTO sec.PERMISSION(code,name,module_code,is_active)
    VALUES('CUSTOMS.RELEASE',N'Registrar levante aduanero','CUSTOMS',1);
GO

DECLARE @company_id BIGINT =
(
    SELECT TOP (1) company_id
    FROM cfg.COMPANY
    WHERE is_deleted = 0
    ORDER BY company_id
);

DECLARE @admin INT=(SELECT role_id FROM sec.ROLE WHERE company_id=@company_id AND code='ADMIN');
DECLARE @operations INT=(SELECT role_id FROM sec.ROLE WHERE company_id=@company_id AND code='OPERATIONS');

DECLARE @p_view INT=(SELECT permission_id FROM sec.PERMISSION WHERE code='CUSTOMS.VIEW');
DECLARE @p_manage INT=(SELECT permission_id FROM sec.PERMISSION WHERE code='CUSTOMS.MANAGE');
DECLARE @p_release INT=(SELECT permission_id FROM sec.PERMISSION WHERE code='CUSTOMS.RELEASE');

IF @admin IS NOT NULL
BEGIN
    IF NOT EXISTS(SELECT 1 FROM sec.ROLE_PERMISSION WHERE role_id=@admin AND permission_id=@p_view)
        INSERT INTO sec.ROLE_PERMISSION(role_id,permission_id,can_view,can_create,can_update,can_delete,can_approve)
        VALUES(@admin,@p_view,1,1,1,1,1);

    IF NOT EXISTS(SELECT 1 FROM sec.ROLE_PERMISSION WHERE role_id=@admin AND permission_id=@p_manage)
        INSERT INTO sec.ROLE_PERMISSION(role_id,permission_id,can_view,can_create,can_update,can_delete,can_approve)
        VALUES(@admin,@p_manage,1,1,1,1,1);

    IF NOT EXISTS(SELECT 1 FROM sec.ROLE_PERMISSION WHERE role_id=@admin AND permission_id=@p_release)
        INSERT INTO sec.ROLE_PERMISSION(role_id,permission_id,can_view,can_create,can_update,can_delete,can_approve)
        VALUES(@admin,@p_release,1,1,1,1,1);
END;

IF @operations IS NOT NULL
BEGIN
    IF NOT EXISTS(SELECT 1 FROM sec.ROLE_PERMISSION WHERE role_id=@operations AND permission_id=@p_view)
        INSERT INTO sec.ROLE_PERMISSION(role_id,permission_id,can_view,can_create,can_update,can_delete,can_approve)
        VALUES(@operations,@p_view,1,0,0,0,0);

    IF NOT EXISTS(SELECT 1 FROM sec.ROLE_PERMISSION WHERE role_id=@operations AND permission_id=@p_manage)
        INSERT INTO sec.ROLE_PERMISSION(role_id,permission_id,can_view,can_create,can_update,can_delete,can_approve)
        VALUES(@operations,@p_manage,1,1,1,0,0);

    IF NOT EXISTS(SELECT 1 FROM sec.ROLE_PERMISSION WHERE role_id=@operations AND permission_id=@p_release)
        INSERT INTO sec.ROLE_PERMISSION(role_id,permission_id,can_view,can_create,can_update,can_delete,can_approve)
        VALUES(@operations,@p_release,1,0,0,0,1);
END;
GO
