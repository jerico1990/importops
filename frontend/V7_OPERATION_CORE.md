# Frontend V7 — Import Operation Core

Base acumulativa: V6 Quotation.

Implementado contra backend real:
- creación explícita desde cotización APPROVED;
- ver operación existente desde la cotización;
- listado y filtros desde backend;
- expediente operativo;
- responsable operativo;
- workflow y transiciones habilitadas;
- stage/status history;
- pause/resume/cancel/close;
- tareas automáticas y manuales;
- allowedNextStatuses provistos por backend;
- timeline con visibilidad;
- notas internas/cliente/incidencias;
- aislamiento de portal cliente.

No hay operaciones, estados, tareas ni eventos simulados.
Shipment, Customs, Documents y Finance quedan como siguientes módulos
especializados ligados al operationId.
