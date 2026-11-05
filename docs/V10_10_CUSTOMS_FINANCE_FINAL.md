# Backend V10.10 — Customs + Finance Final

Base acumulativa: V10.9.1 Shipment Query Fix.

## Customs
- CRUD de proceso aduanero por operación/Shipment ARRIVED.
- DAM/declaración, fecha, oficina, canal y régimen.
- Observaciones con tarea obligatoria automática y resolución.
- Tributos, estados de pago y consolidado.
- Levante solo desde IN_REVIEW, sin observaciones abiertas y sin tributos pendientes.
- Catálogos backend para estados, canales, tipos/severidad de observación y tributos.

## Finance
- resumen financiero por operación;
- consulta explícita de presupuesto y rendición;
- presupuesto idempotente desde cotización aprobada;
- fondos del cliente;
- gastos y pagos;
- servicios cobrables;
- cobros y aplicaciones a servicios;
- ajustes;
- devoluciones;
- preparación y aprobación de rendición;
- Finance bloqueado explícitamente para usuarios CLIENT;
- GET de servicios ya no crea presupuesto como efecto colateral.

## Shipment
- incluye fix del conflicto CS1931 en GetByIdAsync.
- mantiene queries EF traducibles sin ShipmentQueryRow/BaseQuery.

## Base de datos
No hay nuevas tablas en V10.10. Se incluye el script V1.12 de países/puertos para instalaciones nuevas o incompletas.
