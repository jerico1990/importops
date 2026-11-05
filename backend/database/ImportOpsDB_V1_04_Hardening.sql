USE ImportOpsDB;
GO

SET XACT_ABORT ON;
GO

/* ============================================================================
   V5.1 HARDENING
   - Roles/permisos reales
   - Mapeo de usuario externo JWT/OIDC
   - Índices de autorización
============================================================================ */

DECLARE @company_id BIGINT =
(
    SELECT TOP (1) company_id
    FROM cfg.COMPANY
    WHERE is_deleted = 0
    ORDER BY company_id
);

IF @company_id IS NULL
    THROW 50001, 'No existe empresa activa.', 1;


IF NOT EXISTS(SELECT 1 FROM sec.PERMISSION WHERE code='SUPPLIERS.VIEW')
    INSERT INTO sec.PERMISSION(code,name,module_code,is_active) VALUES('SUPPLIERS.VIEW',N'Ver proveedores','SUPPLIERS',1);
IF NOT EXISTS(SELECT 1 FROM sec.PERMISSION WHERE code='SUPPLIERS.MANAGE')
    INSERT INTO sec.PERMISSION(code,name,module_code,is_active) VALUES('SUPPLIERS.MANAGE',N'Gestionar proveedores','SUPPLIERS',1);

/* Roles V1 */
IF NOT EXISTS (SELECT 1 FROM sec.ROLE WHERE company_id=@company_id AND code='ADMIN')
    INSERT INTO sec.ROLE(company_id,code,name,is_system,is_active)
    VALUES(@company_id,'ADMIN',N'Administrador',1,1);

IF NOT EXISTS (SELECT 1 FROM sec.ROLE WHERE company_id=@company_id AND code='COMMERCIAL')
    INSERT INTO sec.ROLE(company_id,code,name,is_system,is_active)
    VALUES(@company_id,'COMMERCIAL',N'Comercial',1,1);

IF NOT EXISTS (SELECT 1 FROM sec.ROLE WHERE company_id=@company_id AND code='OPERATIONS')
    INSERT INTO sec.ROLE(company_id,code,name,is_system,is_active)
    VALUES(@company_id,'OPERATIONS',N'Operaciones',1,1);

IF NOT EXISTS (SELECT 1 FROM sec.ROLE WHERE company_id=@company_id AND code='FINANCE')
    INSERT INTO sec.ROLE(company_id,code,name,is_system,is_active)
    VALUES(@company_id,'FINANCE',N'Control económico',1,1);

DECLARE @admin INT=(SELECT role_id FROM sec.ROLE WHERE company_id=@company_id AND code='ADMIN');
DECLARE @commercial INT=(SELECT role_id FROM sec.ROLE WHERE company_id=@company_id AND code='COMMERCIAL');
DECLARE @operations INT=(SELECT role_id FROM sec.ROLE WHERE company_id=@company_id AND code='OPERATIONS');
DECLARE @finance INT=(SELECT role_id FROM sec.ROLE WHERE company_id=@company_id AND code='FINANCE');

/* ADMIN recibe todo permiso activo */
INSERT INTO sec.ROLE_PERMISSION(role_id,permission_id,can_view,can_create,can_update,can_delete,can_approve)
SELECT @admin,p.permission_id,1,1,1,1,1
FROM sec.PERMISSION p
WHERE p.is_active=1
AND NOT EXISTS(
    SELECT 1 FROM sec.ROLE_PERMISSION rp
    WHERE rp.role_id=@admin AND rp.permission_id=p.permission_id
);

/* Comercial */
INSERT INTO sec.ROLE_PERMISSION(role_id,permission_id,can_view,can_create,can_update,can_delete,can_approve)
SELECT @commercial,p.permission_id,1,1,1,0,
       CASE WHEN p.code='QUOTATIONS.APPROVE' THEN 0 ELSE 0 END
FROM sec.PERMISSION p
WHERE p.code IN(
    'CLIENTS.VIEW','CLIENTS.MANAGE','SUPPLIERS.VIEW','SUPPLIERS.MANAGE',
    'REQUESTS.VIEW','REQUESTS.MANAGE',
    'QUOTATIONS.VIEW','QUOTATIONS.MANAGE',
    'OPERATIONS.VIEW'
)
AND NOT EXISTS(
    SELECT 1 FROM sec.ROLE_PERMISSION rp
    WHERE rp.role_id=@commercial AND rp.permission_id=p.permission_id
);

/* Operaciones */
INSERT INTO sec.ROLE_PERMISSION(role_id,permission_id,can_view,can_create,can_update,can_delete,can_approve)
SELECT @operations,p.permission_id,1,1,1,0,0
FROM sec.PERMISSION p
WHERE p.code IN(
    'CLIENTS.VIEW','SUPPLIERS.VIEW',
    'REQUESTS.VIEW',
    'QUOTATIONS.VIEW',
    'OPERATIONS.VIEW','OPERATIONS.MANAGE',
    'DOCUMENTS.VIEW','DOCUMENTS.MANAGE'
)
AND NOT EXISTS(
    SELECT 1 FROM sec.ROLE_PERMISSION rp
    WHERE rp.role_id=@operations AND rp.permission_id=p.permission_id
);

/* Finanzas */
INSERT INTO sec.ROLE_PERMISSION(role_id,permission_id,can_view,can_create,can_update,can_delete,can_approve)
SELECT @finance,p.permission_id,1,1,1,0,
       CASE WHEN p.code='FINANCE.APPROVE' THEN 1 ELSE 0 END
FROM sec.PERMISSION p
WHERE p.code IN(
    'CLIENTS.VIEW',
    'OPERATIONS.VIEW',
    'FINANCE.VIEW','FINANCE.MANAGE','FINANCE.APPROVE',
    'REPORTS.VIEW'
)
AND NOT EXISTS(
    SELECT 1 FROM sec.ROLE_PERMISSION rp
    WHERE rp.role_id=@finance AND rp.permission_id=p.permission_id
);

IF NOT EXISTS(
    SELECT 1 FROM sys.indexes
    WHERE object_id=OBJECT_ID(N'sec.USER_ROLE')
      AND name=N'IX_USER_ROLE_USER'
)
    CREATE INDEX IX_USER_ROLE_USER ON sec.USER_ROLE(user_id,role_id);

IF NOT EXISTS(
    SELECT 1 FROM sys.indexes
    WHERE object_id=OBJECT_ID(N'sec.ROLE_PERMISSION')
      AND name=N'IX_ROLE_PERMISSION_ROLE'
)
    CREATE INDEX IX_ROLE_PERMISSION_ROLE ON sec.ROLE_PERMISSION(role_id,permission_id);

IF NOT EXISTS(
    SELECT 1 FROM sys.indexes
    WHERE object_id=OBJECT_ID(N'audit.AUDIT_LOG')
      AND name=N'IX_AUDIT_ENTITY'
)
    CREATE INDEX IX_AUDIT_ENTITY ON audit.AUDIT_LOG(entity_name,entity_id,event_at DESC);

/*
IMPORTANTE:
Actualizar external_auth_id con el claim "sub" del proveedor OIDC.

Ejemplo:
UPDATE sec.APP_USER
SET external_auth_id = N'<subject-del-token>'
WHERE user_id = 1;

Luego asignar rol:
INSERT INTO sec.USER_ROLE(user_id,role_id,assigned_by)
SELECT 1,@admin,1
WHERE NOT EXISTS(
  SELECT 1 FROM sec.USER_ROLE WHERE user_id=1 AND role_id=@admin
);
*/
GO
