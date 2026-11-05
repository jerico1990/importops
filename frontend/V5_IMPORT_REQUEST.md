# Frontend V5 — Import Request

Base exacta: V4.1 validada.

Todo el módulo Solicitudes usa backend real:
- GET /api/import-requests
- GET /api/import-requests/{id}
- POST /api/import-requests
- PUT /api/import-requests/{id}
- DELETE /api/import-requests/{id}
- POST submit-review / observe / reject / cancel / assign
- GET /api/quotations/by-request/{requestId}
- GET /api/documents?requestId=...
- POST /api/documents multipart/form-data

Todos los combos salen de backend:
- clients
- client contacts
- suppliers
- request assignees
- countries
- ports
- transport modes
- incoterms
- currencies
- document types
- import request statuses

No hay fixtures ni arrays de solicitudes simuladas.

Para usuario CLIENT:
- no se envía clientId como filtro de seguridad;
- backend impone client_id de la sesión;
- cliente queda fijo al crear/editar;
- no ve ni modifica notas internas;
- no asigna responsable interno;
- no asigna proveedor global;
- no ve auditoría interna de reasignaciones.
