USE ImportOpsDB;
GO
SET XACT_ABORT ON;
GO

/* ============================================================================
   V1.11 - DECISIÓN DE COTIZACIÓN POR ADMINISTRADOR DEL CLIENTE

   CLIENT_ADMIN:
     - puede VER sus cotizaciones (ya existente)
     - puede APROBAR / RECHAZAR cotizaciones SENT de su propio client_id

   CLIENT_USER:
     - continúa solo con QUOTATIONS.VIEW

   El aislamiento por client_id y la validación del estado SENT se aplican
   adicionalmente en backend.
============================================================================ */

DECLARE @company_id BIGINT =
(
    SELECT TOP 1 company_id
    FROM cfg.COMPANY
    WHERE is_deleted = 0
    ORDER BY company_id
);

IF @company_id IS NULL
    THROW 50001, 'No existe empresa activa.', 1;

DECLARE @client_admin INT =
(
    SELECT role_id
    FROM sec.ROLE
    WHERE company_id = @company_id
      AND code = 'CLIENT_ADMIN'
      AND is_active = 1
);

DECLARE @quotation_approve INT =
(
    SELECT permission_id
    FROM sec.PERMISSION
    WHERE code = 'QUOTATIONS.APPROVE'
      AND is_active = 1
);

IF @client_admin IS NULL
    THROW 50002, 'No existe el rol CLIENT_ADMIN.', 1;

IF @quotation_approve IS NULL
    THROW 50003, 'No existe el permiso QUOTATIONS.APPROVE.', 1;

IF NOT EXISTS
(
    SELECT 1
    FROM sec.ROLE_PERMISSION
    WHERE role_id = @client_admin
      AND permission_id = @quotation_approve
)
BEGIN
    INSERT INTO sec.ROLE_PERMISSION
    (
        role_id,
        permission_id,
        can_view,
        can_create,
        can_update,
        can_delete,
        can_approve
    )
    VALUES
    (
        @client_admin,
        @quotation_approve,
        1, 0, 0, 0, 1
    );
END
ELSE
BEGIN
    UPDATE sec.ROLE_PERMISSION
    SET can_view = 1,
        can_approve = 1
    WHERE role_id = @client_admin
      AND permission_id = @quotation_approve;
END;
GO

SELECT
    r.code AS role_code,
    p.code AS permission_code,
    rp.can_view,
    rp.can_create,
    rp.can_update,
    rp.can_delete,
    rp.can_approve
FROM sec.ROLE_PERMISSION rp
INNER JOIN sec.ROLE r ON r.role_id = rp.role_id
INNER JOIN sec.PERMISSION p ON p.permission_id = rp.permission_id
WHERE r.code = 'CLIENT_ADMIN'
  AND p.code = 'QUOTATIONS.APPROVE';
GO
