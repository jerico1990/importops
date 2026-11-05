# Frontend V6 — Quotation

Base exacta: V5 Import Request.

Módulo real:
- listado paginado
- filtros backend
- creación desde IMPORT_REQUEST UNDER_REVIEW
- edición de cabecera DRAFT
- conceptos CRUD
- categorías de servicio/gasto desde backend
- tipos de concepto desde backend
- totales calculados exclusivamente por backend
- envío
- aprobación/rechazo
- vencimiento/cancelación
- versionado
- historial
- versiones por solicitud
- integración con request detail

CLIENT:
- nunca recibe DRAFT / cotizaciones nunca enviadas
- CLIENT_USER: consulta
- CLIENT_ADMIN: decide APPROVE / REJECT
- nunca recibe InternalNotes

No existen arrays de cotizaciones, estados, tipos o categorías hardcodeados en el frontend.
