# ImportOps frontend V11.15 — Primera etapa de simplificación

- Navegación principal de Finanzas reducida a tres secciones: Resumen financiero, Registro de movimientos, Liquidación y financiamiento.
- Registro de movimientos agrupa accesos a fondos, gastos, servicios, cobros, devoluciones y conciliación; conserva formularios, consultas y operaciones existentes.
- La cuenta por cobrar se muestra como dato informativo en la conciliación, no como señal visual de error. ATENCIÓN: la validación de aprobación del backend V10.22 NO ha sido cambiada.
- En liquidación, un 404 al consultar delivery (sin envío registrado) se interpreta como ausencia de envío. El 404 de financing sigue siendo un error real.

## Pendiente para completar el rediseño

- El backend actual carece de endpoints CRUD completos de modificación/eliminación de fondos, gastos, servicios, cobros y devoluciones. No se agregaron botones falsos de edición/eliminación.
- Antes de permitir editar/eliminar, definir y programar restricciones por pagos, asignaciones, documentos emitidos, auditoría y permisos.
- La liquidación definitiva sigue consolidando una moneda; el modelo separado PEN/USD del Excel de R&B requiere cambios de contrato y cálculo backend.
- No se ha ejecutado npm install/npm run build en este entorno. Ejecutar npm ci y npm run build localmente antes de desplegar.
