# Backend V10.9 — Shipment + Documents

Base acumulativa: V10.8 Operation Core FIXED.

Shipment:
- endpoint available-actions calculado por backend;
- catálogos de estados y tipos de contenedor;
- notes internas no se exponen a usuarios CLIENT;
- se conserva client scope en list/detail/by-operation/mutations;
- acciones continúan validándose en backend.

Documents:
- catálogos de estados y observaciones;
- GET /api/documents/{id}/observations;
- ReplaceAsync usa scope real de documento;
- operation requirements aplica client scope;
- se mantienen versionado, validación, observación, resolución,
  reemplazo, descarga y recalculo de DocumentaryStatus.

No requiere cambio de esquema SQL.
