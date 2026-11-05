# Backend V10.6 — Import Request hardening

- Scope obligatorio por client_id para CLIENT en list/detail/mutations.
- Query con clientId ajeno devuelve 403.
- Detail de otro cliente devuelve 404.
- CLIENT no puede asignar proveedor, responsable ni notas internas.
- InternalNotes no se exponen a CLIENT.
- Timeline cliente excluye eventos internos de reasignación (old_status == new_status).
- REQUEST_ASSIGNEE retorna solo INTERNAL activos con ADMIN/COMMERCIAL/OPERATIONS.
- ValidatePayload usa la misma regla para el responsable.
- Nuevo catálogo GET /api/catalogs/import-request-statuses.
- No requiere cambio de esquema SQL.
