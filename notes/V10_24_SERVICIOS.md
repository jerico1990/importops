# Backend V10.24 — Servicios propios de R&B
- POST /api/finance/operations/{id}/services
- PUT /api/finance/operations/{id}/services/{serviceId}
- POST /api/finance/operations/{id}/services/{serviceId}/void
- No se crean automáticamente servicios definitivos desde las líneas de la cotización al generar presupuesto. El presupuesto sigue guardando estimados.
- No se permite editar/anular servicios con cobros aplicados ni después de aprobar rendición o emitir liquidación.
- Se utiliza borrado lógico y eventos de auditoría. Los servicios históricos siguen disponibles y editables antes de emisión.
- No se requiere cambio de esquema SQL para estos endpoints.
- NO compilado ni probado en .NET por ausencia del SDK en este entorno.
