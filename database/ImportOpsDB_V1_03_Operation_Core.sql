USE ImportOpsDB;
GO
SET XACT_ABORT ON;
GO

/* ============================================================================
   V5 - IMPORT_OPERATION CORE
   Refuerza integridad, snapshot y templates operativos.
============================================================================ */

/* Snapshot comercial y de cotización para preservar la foto al abrir operación. */
IF COL_LENGTH('ops.IMPORT_OPERATION', 'client_name_snapshot') IS NULL
    ALTER TABLE ops.IMPORT_OPERATION ADD client_name_snapshot NVARCHAR(200) NULL;
IF COL_LENGTH('ops.IMPORT_OPERATION', 'client_contact_name_snapshot') IS NULL
    ALTER TABLE ops.IMPORT_OPERATION ADD client_contact_name_snapshot NVARCHAR(160) NULL;
IF COL_LENGTH('ops.IMPORT_OPERATION', 'supplier_name_snapshot') IS NULL
    ALTER TABLE ops.IMPORT_OPERATION ADD supplier_name_snapshot NVARCHAR(200) NULL;
IF COL_LENGTH('ops.IMPORT_OPERATION', 'quotation_no_snapshot') IS NULL
    ALTER TABLE ops.IMPORT_OPERATION ADD quotation_no_snapshot NVARCHAR(30) NULL;
IF COL_LENGTH('ops.IMPORT_OPERATION', 'quotation_version_snapshot') IS NULL
    ALTER TABLE ops.IMPORT_OPERATION ADD quotation_version_snapshot INT NULL;
IF COL_LENGTH('ops.IMPORT_OPERATION', 'quotation_currency_snapshot') IS NULL
    ALTER TABLE ops.IMPORT_OPERATION ADD quotation_currency_snapshot CHAR(3) NULL;
IF COL_LENGTH('ops.IMPORT_OPERATION', 'quotation_total_snapshot') IS NULL
    ALTER TABLE ops.IMPORT_OPERATION ADD quotation_total_snapshot DECIMAL(19,4) NULL;
GO

/* Enlace a template para generar tareas idempotentes por operación/etapa. */
IF COL_LENGTH('ops.OPERATION_TASK', 'stage_task_template_id') IS NULL
BEGIN
    ALTER TABLE ops.OPERATION_TASK ADD stage_task_template_id BIGINT NULL;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_OPERATION_TASK_TEMPLATE')
BEGIN
    ALTER TABLE ops.OPERATION_TASK
    ADD CONSTRAINT FK_OPERATION_TASK_TEMPLATE
        FOREIGN KEY(stage_task_template_id)
        REFERENCES cfg.STAGE_TASK_TEMPLATE(stage_task_template_id);
END;
GO

/* Una cotización aprobada y una solicitud solo pueden originar una operación activa. */
IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'ops.IMPORT_OPERATION')
      AND name = N'UX_OPERATION_QUOTATION_ACTIVE'
)
BEGIN
    CREATE UNIQUE INDEX UX_OPERATION_QUOTATION_ACTIVE
        ON ops.IMPORT_OPERATION(quotation_id)
        WHERE quotation_id IS NOT NULL AND is_deleted = 0;
END;
GO

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'ops.IMPORT_OPERATION')
      AND name = N'UX_OPERATION_REQUEST_ACTIVE'
)
BEGIN
    CREATE UNIQUE INDEX UX_OPERATION_REQUEST_ACTIVE
        ON ops.IMPORT_OPERATION(request_id)
        WHERE request_id IS NOT NULL AND is_deleted = 0;
END;
GO

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'ops.OPERATION_TASK')
      AND name = N'UX_OPERATION_TASK_TEMPLATE_ACTIVE'
)
BEGIN
    CREATE UNIQUE INDEX UX_OPERATION_TASK_TEMPLATE_ACTIVE
        ON ops.OPERATION_TASK(operation_id, stage_task_template_id)
        WHERE stage_task_template_id IS NOT NULL AND is_deleted = 0;
END;
GO

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'ops.OPERATION_STATUS_HISTORY')
      AND name = N'IX_OPERATION_STATUS_HISTORY_DATE'
)
BEGIN
    CREATE INDEX IX_OPERATION_STATUS_HISTORY_DATE
        ON ops.OPERATION_STATUS_HISTORY(operation_id, changed_at DESC);
END;
GO

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'ops.OPERATION_EVENT')
      AND name = N'IX_OPERATION_EVENT_TIMELINE'
)
BEGIN
    CREATE INDEX IX_OPERATION_EVENT_TIMELINE
        ON ops.OPERATION_EVENT(operation_id, event_date DESC);
END;
GO

/* ============================================================================
   Templates V1
   Se insertan solo si no existe el mismo título para la misma etapa.
============================================================================ */
DECLARE @templates TABLE
(
    stage_code VARCHAR(40),
    title NVARCHAR(180),
    description NVARCHAR(500),
    mandatory BIT,
    due_days INT,
    display_order INT
);

INSERT INTO @templates VALUES
('DOCUMENTATION',N'Validar Commercial Invoice',N'Verificar emisor, consignatario, descripción, valores, moneda e Incoterm.',1,2,10),
('DOCUMENTATION',N'Validar Packing List',N'Verificar bultos, pesos, medidas y consistencia con la mercancía declarada.',1,2,20),
('DOCUMENTATION',N'Confirmar requisitos especiales de la mercancía',N'Identificar permisos, certificados o restricciones que puedan afectar el despacho.',1,2,30),
('DOCUMENTATION',N'Verificar datos logísticos de origen y destino',N'Confirmar proveedor, modalidad, puertos/aeropuertos e Incoterm operativo.',1,2,40),

('COORDINATION',N'Confirmar booking o reserva',N'Validar reserva con transportista/agente y condiciones del embarque.',1,3,10),
('COORDINATION',N'Confirmar instrucciones de embarque',N'Verificar datos de consignación, documentos y coordinación con proveedor.',1,3,20),
('COORDINATION',N'Confirmar ETD y ETA iniciales',N'Registrar fechas estimadas de salida y arribo.',1,3,30),

('PENDING_SHIPMENT',N'Validar documento de transporte preliminar',N'Revisar BL/AWB preliminar y datos de consignación antes de salida.',1,2,10),
('PENDING_SHIPMENT',N'Confirmar salida de la carga',N'La tarea se completa cuando Shipment registre salida confirmada.',1,3,20),

('IN_TRANSIT',N'Realizar seguimiento de ETA',N'Controlar cambios de ETA y registrar incidencias relevantes.',1,5,10),
('IN_TRANSIT',N'Preparar expediente para despacho aduanero',N'Verificar que la documentación necesaria esté disponible antes del arribo.',1,5,20),

('ARRIVED',N'Confirmar arribo de la carga',N'Validar arribo real y disponibilidad de la carga/documentos.',1,1,10),
('ARRIVED',N'Coordinar inicio de despacho aduanero',N'Preparar numeración y documentación para aduanas.',1,1,20),

('CUSTOMS',N'Registrar declaración o DAM',N'Completar información de declaración aduanera aplicable.',1,2,10),
('CUSTOMS',N'Revisar canal y observaciones',N'Gestionar canal asignado, aforo u observaciones del despacho.',1,2,20),
('CUSTOMS',N'Gestionar levante',N'Completar acciones necesarias hasta obtener levante.',1,3,30),

('RELEASED',N'Confirmar levante',N'Verificar fecha y estado de levante en el proceso aduanero.',1,1,10),
('RELEASED',N'Coordinar retiro y transporte local',N'Coordinar retiro de terminal/almacén y traslado a destino.',1,2,20),

('PENDING_DELIVERY',N'Coordinar entrega con el cliente',N'Confirmar fecha, lugar, contacto receptor y condiciones de entrega.',1,2,10),
('PENDING_DELIVERY',N'Confirmar recepción de mercancía',N'Obtener confirmación de entrega/recepción del cliente.',1,2,20),

('DELIVERED',N'Registrar conformidad de entrega',N'Dejar evidencia de entrega y observaciones finales operativas.',1,2,10),
('DELIVERED',N'Preparar rendición de la operación',N'Reunir soportes y pendientes para cierre económico.',1,3,20),

('SETTLEMENT',N'Completar rendición económica',N'Consolidar gastos, fondos, servicios, cobros, ajustes y devoluciones.',1,5,10),
('SETTLEMENT',N'Validar cierre documentario',N'Confirmar que el expediente documentario esté completo.',1,5,20);

INSERT INTO cfg.STAGE_TASK_TEMPLATE
(
    company_id, workflow_stage_id, title, description,
    is_mandatory, due_days_offset, display_order, is_active
)
SELECT
    s.company_id,
    s.workflow_stage_id,
    t.title,
    t.description,
    t.mandatory,
    t.due_days,
    t.display_order,
    1
FROM @templates t
INNER JOIN cfg.WORKFLOW_STAGE s
    ON s.code = t.stage_code
   AND s.is_active = 1
INNER JOIN cfg.COMPANY c
    ON c.company_id = s.company_id
   AND c.is_active = 1
   AND c.is_deleted = 0
WHERE NOT EXISTS
(
    SELECT 1
    FROM cfg.STAGE_TASK_TEMPLATE e
    WHERE e.company_id = s.company_id
      AND e.workflow_stage_id = s.workflow_stage_id
      AND e.title = t.title
);
GO
