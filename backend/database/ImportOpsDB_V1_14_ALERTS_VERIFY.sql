USE [ImportOpsDB];
GO

PRINT '=== OBJETOS ALERTAS ===';
SELECT
    OBJECT_ID(N'cfg.ALERT_RULE', N'U') AS alert_rule_object_id,
    OBJECT_ID(N'ops.ALERT', N'U') AS alert_object_id,
    OBJECT_ID(N'ops.ALERT_USER_STATE', N'U') AS alert_user_state_object_id;
GO

PRINT '=== REGLAS POR EMPRESA ===';
SELECT c.company_id, c.legal_name, COUNT(ar.alert_rule_id) AS alert_rules
FROM cfg.COMPANY c
LEFT JOIN cfg.ALERT_RULE ar ON ar.company_id = c.company_id
WHERE c.is_active = 1 AND c.is_deleted = 0
GROUP BY c.company_id, c.legal_name
ORDER BY c.company_id;
GO

PRINT '=== CONFIGURACION DE REGLAS ===';
SELECT company_id, rule_code, name, module_code, is_active, severity,
       threshold_minutes, client_enabled, internal_enabled, display_order
FROM cfg.ALERT_RULE
ORDER BY company_id, display_order, rule_code;
GO

PRINT '=== PERMISOS DE ALERTAS ===';
SELECT p.permission_id, p.code, p.name, p.is_active
FROM sec.PERMISSION p
WHERE p.code IN ('ALERTS.VIEW', 'ALERTS.MANAGE')
ORDER BY p.code;
GO

PRINT '=== ROLES CON PERMISOS DE ALERTAS ===';
SELECT r.code AS role_code, p.code AS permission_code,
       rp.can_view, rp.can_create, rp.can_update, rp.can_delete, rp.can_approve
FROM sec.ROLE_PERMISSION rp
JOIN sec.ROLE r ON r.role_id = rp.role_id
JOIN sec.PERMISSION p ON p.permission_id = rp.permission_id
WHERE p.code IN ('ALERTS.VIEW', 'ALERTS.MANAGE')
ORDER BY r.code, p.code;
GO

PRINT '=== ALERTAS ACTIVAS POR TIPO / AUDIENCIA ===';
SELECT company_id, alert_type, audience, severity, COUNT(*) AS total
FROM ops.ALERT
WHERE status = 'ACTIVE'
GROUP BY company_id, alert_type, audience, severity
ORDER BY company_id, alert_type, audience, severity;
GO

PRINT '=== ULTIMAS 50 ALERTAS ===';
SELECT TOP (50)
       a.alert_id, a.company_id, a.client_id, a.operation_id,
       o.operation_no, a.alert_type, a.audience, a.severity,
       a.title, a.status, a.triggered_at, a.last_evaluated_at, a.resolved_at,
       a.responsible_user_id, a.required_permission, a.target_url, a.dedup_key
FROM ops.ALERT a
LEFT JOIN ops.IMPORT_OPERATION o ON o.operation_id = a.operation_id
ORDER BY a.alert_id DESC;
GO

PRINT '=== ESTADO POR USUARIO ===';
SELECT TOP (50)
       aus.user_id, u.username, aus.alert_id,
       aus.is_read, aus.read_at, aus.is_dismissed, aus.dismissed_at, aus.updated_at
FROM ops.ALERT_USER_STATE aus
JOIN sec.APP_USER u ON u.user_id = aus.user_id
ORDER BY aus.updated_at DESC;
GO
