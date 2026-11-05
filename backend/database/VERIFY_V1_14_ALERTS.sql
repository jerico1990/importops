USE ImportOpsDB;
GO
SELECT OBJECT_ID('cfg.ALERT_RULE') alert_rule_table, OBJECT_ID('ops.ALERT') alert_table, OBJECT_ID('ops.ALERT_USER_STATE') alert_user_state_table;
SELECT code,name,module_code,is_active FROM sec.PERMISSION WHERE code IN ('ALERTS.VIEW','ALERTS.MANAGE') ORDER BY code;
SELECT r.code role_code,p.code permission_code FROM sec.ROLE_PERMISSION rp JOIN sec.ROLE r ON r.role_id=rp.role_id JOIN sec.PERMISSION p ON p.permission_id=rp.permission_id WHERE p.code LIKE 'ALERTS.%' ORDER BY r.code,p.code;
SELECT alert_type,severity,audience,threshold_hours,is_active FROM cfg.ALERT_RULE ORDER BY alert_type;
SELECT TOP(50) alert_id,alert_type,severity,audience,status,operation_id,client_id,responsible_user_id,title,triggered_at,resolved_at FROM ops.ALERT ORDER BY alert_id DESC;
GO
