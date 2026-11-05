# ImportOps Backend V1 — cierre técnico

## Flujo de negocio cerrado

Client
→ ImportRequest
→ Quotation
→ ImportOperation
→ Workflow
→ Shipment/Containers
→ Customs
→ Documents
→ Finance
→ Settlement
→ Operation Closed

## Prueba end-to-end obligatoria

1. Crear cliente/contacto/dirección.
2. Crear solicitud.
3. Enviar a revisión.
4. Crear cotización.
5. Agregar líneas SERVICE y ESTIMATED_COST.
6. Enviar y aprobar cotización.
7. Crear operación.
8. Crear Shipment y contenedor.
9. Booking → Departed → InTransit → Arrived.
10. Crear Customs.
11. InReview → canal → tributos → Released.
12. Cargar documentos obligatorios.
13. Observar/reemplazar/validar documento.
14. Confirmar documentary_status COMPLETE.
15. Crear presupuesto inicial desde quotation.
16. Registrar fondos.
17. Registrar gastos y pagos.
18. Registrar cobros y aplicar a servicios.
19. Registrar ajustes/devoluciones si corresponde.
20. Entrar a SETTLEMENT.
21. Preparar y aprobar rendición.
22. Confirmar financial_status CLOSED.
23. Cerrar operación.

## Comandos

```bash
dotnet restore
dotnet build
dotnet test
dotnet run --project src/ImportOps.Api
```

## Scripts

Ejecutar en orden:

1. ImportOpsDB_V1.sql
2. ImportOpsDB_V1_01_Request_Module.sql
3. ImportOpsDB_V1_02_Quotation_Module.sql
4. ImportOpsDB_V1_03_Operation_Core.sql
5. ImportOpsDB_V1_04_Hardening.sql
6. ImportOpsDB_V1_05_Shipment.sql
7. ImportOpsDB_V1_06_Customs.sql
8. ImportOpsDB_V1_07_Documents.sql
9. ImportOpsDB_V1_08_Finance.sql

## Producción

- configurar Authentication:Authority/Audience;
- asociar JWT `sub` con `sec.APP_USER.external_auth_id`;
- restringir CORS;
- sustituir LOCAL storage por S3/MinIO/Azure si se requiere alta disponibilidad;
- usar secretos externos para connection strings;
- configurar backups SQL/storage;
- habilitar HTTPS obligatorio;
- revisar retención de auditoría;
- ejecutar pruebas end-to-end contra una BD limpia antes del despliegue.

## Convención monetaria

`exchange_rate` significa **unidades de moneda base por 1 unidad de la moneda del movimiento**. Por tanto `base_amount = amount * exchange_rate`. Si la moneda del movimiento ya es la moneda base, usar 1.
