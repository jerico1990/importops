# ImportOps Fase 4.1 - Administración de catálogos

Incluye administración de Países, Puertos, Monedas, Incoterms, Modos de transporte, Tipos de documento, Categorías de servicio, Categorías de gasto y Métodos de pago.

## Instalación
1. Ejecutar `database/ImportOpsDB_V1_15_CATALOG_ADMINISTRATION.sql` sobre la BD actual.
2. Levantar el backend actualizado.
3. Levantar el frontend actualizado.
4. Cerrar/iniciar sesión para que la sesión recargue `CONFIGURATION.VIEW` y `CONFIGURATION.MANAGE`.

## Reglas principales
- No se elimina físicamente un registro que tenga referencias actuales o históricas.
- El intento de eliminación muestra referencias y permite desactivar.
- Los catálogos inactivos dejan de aparecer en los endpoints operativos existentes.
- Códigos y nombres se validan contra duplicados; los códigos se normalizan a mayúsculas.
- Puertos se consultan con paginación SQL y filtros país/tipo/estado.
- Las operaciones CREATE/UPDATE/DELETE son auditadas; activar/desactivar se registra como UPDATE.
