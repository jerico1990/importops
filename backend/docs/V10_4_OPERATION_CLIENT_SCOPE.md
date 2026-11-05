# V10.4 — Operation Client Scope

- `GET /api/operations` siempre aplica `currentUser.ClientId` cuando `user_type = CLIENT`.
- Si un usuario CLIENT intenta enviar un `clientId` distinto al de su sesión, responde 403.
- `GET /api/operations/{id}` ya filtra por `ClientId` y devuelve 404 cuando la operación no pertenece al usuario cliente.
- Historial, tasks, events y notes pasan primero por la validación de operación accesible.
- El frontend cliente no envía `clientId`; oculta el filtro Cliente.
- La pantalla de importaciones dejó de usar datos simulados.
