# Fase 4.2 - Administración de tareas por etapa

## Objetivo
Permitir que un usuario con CONFIGURATION.VIEW / CONFIGURATION.MANAGE administre las plantillas de tareas de `cfg.STAGE_TASK_TEMPLATE` sin alterar las tareas históricas ya generadas en `ops.OPERATION_TASK`.

## Reglas
- La tarea automática continúa asignándose al `ResponsibleUserId` de la operación.
- Al entrar a una etapa, `GenerateStageTasksAsync` usa únicamente plantillas activas.
- Crear/editar una plantilla no modifica tareas históricas.
- Una plantilla que ya generó al menos una tarea, incluso una tarea eliminada lógicamente, no puede borrarse físicamente; debe desactivarse.
- No se permiten títulos duplicados dentro de la misma etapa y compañía.
- Plazo: NULL (sin plazo) o 0..3650 días.
- Orden: 0..100000.
- Solo se pueden crear/editar/reactivar plantillas sobre etapas activas de la compañía actual.

## Endpoints
- GET `/api/configuration/workflow/task-templates`
- POST `/api/configuration/workflow/task-templates`
- PUT `/api/configuration/workflow/task-templates/{id}`
- PATCH `/api/configuration/workflow/task-templates/{id}/active`
- GET `/api/configuration/workflow/task-templates/{id}/delete-check`
- DELETE `/api/configuration/workflow/task-templates/{id}`

## Base de datos
Ejecutar `ImportOpsDB_V1_16_WORKFLOW_TASK_ADMINISTRATION.sql` después de V1.15.
