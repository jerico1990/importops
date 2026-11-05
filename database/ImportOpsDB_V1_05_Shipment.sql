USE ImportOpsDB;
GO

SET XACT_ABORT ON;
GO

/* ============================================================================
   V6 - SHIPMENT + CONTAINERS
============================================================================ */

/* El modelo V1 tenía container_no/seal_no en SHIPMENT para un caso simple.
   En V6 se normaliza el detalle marítimo a múltiples contenedores. */
IF OBJECT_ID(N'ops.SHIPMENT_CONTAINER', N'U') IS NULL
BEGIN
    CREATE TABLE ops.SHIPMENT_CONTAINER
    (
        shipment_container_id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        shipment_id           BIGINT NOT NULL,
        container_no          NVARCHAR(50) NOT NULL,
        container_type        NVARCHAR(30) NULL,
        seal_no               NVARCHAR(50) NULL,
        package_count         DECIMAL(18,3) NULL,
        gross_weight_kg       DECIMAL(18,3) NULL,
        volume_m3             DECIMAL(18,4) NULL,
        notes                 NVARCHAR(1000) NULL,
        is_active             BIT NOT NULL CONSTRAINT DF_SHIPMENT_CONTAINER_ACTIVE DEFAULT(1),
        created_at            DATETIME2(0) NOT NULL CONSTRAINT DF_SHIPMENT_CONTAINER_CREATED DEFAULT(SYSUTCDATETIME()),
        created_by            BIGINT NOT NULL,
        modified_at           DATETIME2(0) NULL,
        modified_by           BIGINT NULL,

        CONSTRAINT FK_SHIPMENT_CONTAINER_SHIPMENT
            FOREIGN KEY(shipment_id) REFERENCES ops.SHIPMENT(shipment_id),

        CONSTRAINT FK_SHIPMENT_CONTAINER_CREATED_BY
            FOREIGN KEY(created_by) REFERENCES sec.APP_USER(user_id),

        CONSTRAINT FK_SHIPMENT_CONTAINER_MODIFIED_BY
            FOREIGN KEY(modified_by) REFERENCES sec.APP_USER(user_id),

        CONSTRAINT CK_SHIPMENT_CONTAINER_PACKAGE
            CHECK(package_count IS NULL OR package_count >= 0),

        CONSTRAINT CK_SHIPMENT_CONTAINER_WEIGHT
            CHECK(gross_weight_kg IS NULL OR gross_weight_kg >= 0),

        CONSTRAINT CK_SHIPMENT_CONTAINER_VOLUME
            CHECK(volume_m3 IS NULL OR volume_m3 >= 0)
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'ops.SHIPMENT_CONTAINER')
      AND name = N'UX_SHIPMENT_CONTAINER_ACTIVE_NO'
)
BEGIN
    CREATE UNIQUE INDEX UX_SHIPMENT_CONTAINER_ACTIVE_NO
        ON ops.SHIPMENT_CONTAINER(shipment_id, container_no)
        WHERE is_active = 1;
END;
GO

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'ops.SHIPMENT')
      AND name = N'IX_SHIPMENT_COMPANY_OPERATION_STATUS'
)
BEGIN
    CREATE INDEX IX_SHIPMENT_COMPANY_OPERATION_STATUS
        ON ops.SHIPMENT(company_id, operation_id, shipment_status)
        INCLUDE(transport_mode_id, booking_no, etd, eta, actual_departure_at, actual_arrival_at);
END;
GO

/* Permisos */
IF NOT EXISTS(SELECT 1 FROM sec.PERMISSION WHERE code='SHIPMENTS.VIEW')
    INSERT INTO sec.PERMISSION(code,name,module_code,is_active)
    VALUES('SHIPMENTS.VIEW',N'Ver embarques','SHIPMENTS',1);

IF NOT EXISTS(SELECT 1 FROM sec.PERMISSION WHERE code='SHIPMENTS.MANAGE')
    INSERT INTO sec.PERMISSION(code,name,module_code,is_active)
    VALUES('SHIPMENTS.MANAGE',N'Gestionar embarques','SHIPMENTS',1);
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
DECLARE @view_permission INT=(SELECT permission_id FROM sec.PERMISSION WHERE code='SHIPMENTS.VIEW');
DECLARE @manage_permission INT=(SELECT permission_id FROM sec.PERMISSION WHERE code='SHIPMENTS.MANAGE');

IF @admin IS NOT NULL
BEGIN
    IF NOT EXISTS(SELECT 1 FROM sec.ROLE_PERMISSION WHERE role_id=@admin AND permission_id=@view_permission)
        INSERT INTO sec.ROLE_PERMISSION(role_id,permission_id,can_view,can_create,can_update,can_delete,can_approve)
        VALUES(@admin,@view_permission,1,1,1,1,1);

    IF NOT EXISTS(SELECT 1 FROM sec.ROLE_PERMISSION WHERE role_id=@admin AND permission_id=@manage_permission)
        INSERT INTO sec.ROLE_PERMISSION(role_id,permission_id,can_view,can_create,can_update,can_delete,can_approve)
        VALUES(@admin,@manage_permission,1,1,1,1,1);
END;

IF @operations IS NOT NULL
BEGIN
    IF NOT EXISTS(SELECT 1 FROM sec.ROLE_PERMISSION WHERE role_id=@operations AND permission_id=@view_permission)
        INSERT INTO sec.ROLE_PERMISSION(role_id,permission_id,can_view,can_create,can_update,can_delete,can_approve)
        VALUES(@operations,@view_permission,1,0,0,0,0);

    IF NOT EXISTS(SELECT 1 FROM sec.ROLE_PERMISSION WHERE role_id=@operations AND permission_id=@manage_permission)
        INSERT INTO sec.ROLE_PERMISSION(role_id,permission_id,can_view,can_create,can_update,can_delete,can_approve)
        VALUES(@operations,@manage_permission,1,1,1,0,0);
END;
GO
