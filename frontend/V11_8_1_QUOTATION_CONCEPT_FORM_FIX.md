# V11.8.1 — Corrección integral del formulario de Conceptos de Proforma

Base exacta: Frontend V11.8.

## Causa raíz
Los controles de clasificación de `quotation-concepts` habían quedado insertados dentro del bloque visual exclusivo de alta de catálogos estándar (`standardCreate && form.code`). Como `quotation-concepts` no pertenece a ese flujo, en edición se mostraban Código y Nombre, pero no Tipo, Categoría, Grupo ni tratamiento tributario, aunque la validación sí los exigía.

## Corrección
- El bloque de clasificación ahora es independiente y se renderiza tanto al crear como al editar un concepto de proforma.
- Tipo controla de forma excluyente Categoría de servicio / Categoría de gasto.
- Al cambiar de tipo se limpia la categoría incompatible.
- Grupo, orden, afectación tributaria, tasa y refacturación se editan en el mismo mantenimiento.
- Las categorías se cargan de los catálogos activos del backend.
- La tasa se deshabilita y queda en 0 cuando el concepto no está afecto a impuesto.
- La opción de rendición/refacturación solo aplica a conceptos de tipo Gasto.

## Compatibilidad
No requiere cambios de BD ni backend. Compatible con BD V1.19 y Backend V10.16.2.
