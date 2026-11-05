# ImportOps V10.1 — autenticación local y alcance cliente

## Autenticación de desarrollo

No se usa JWT ni Keycloak.

El backend usa una cookie HTTP-only:

`ImportOps.DevSession`

Flujo:

1. `POST /api/auth/login`
2. ASP.NET Core crea la cookie de sesión.
3. Swagger/browser conserva la cookie.
4. Los siguientes endpoints quedan autenticados.
5. `POST /api/auth/logout` elimina la sesión.

## Usuarios demo

| Usuario | Password | Tipo | Perfil |
|---|---|---|---|
| admin | Admin123* | INTERNAL | ADMIN |
| comercial.demo | Comercial123* | INTERNAL | COMMERCIAL |
| operaciones.demo | Operaciones123* | INTERNAL | OPERATIONS |
| finanzas.demo | Finanzas123* | INTERNAL | FINANCE |
| cliente.admin.andino | Cliente123* | CLIENT | CLIENT_ADMIN |
| cliente.usuario.andino | Cliente123* | CLIENT | CLIENT_USER |
| cliente.usuario.pacifico | Cliente123* | CLIENT | CLIENT_USER |

Estos usuarios son SOLO para desarrollo.

## Prueba en Swagger

Ejecutar:

`POST /api/auth/login`

```json
{
  "username": "admin",
  "password": "Admin123*",
  "rememberMe": false
}
```

No necesitas botón Authorize ni token.

Luego ejecutar:

`GET /api/auth/session`

y:

`GET /api/security/me`

## Aislamiento de clientes

Un `APP_USER` CLIENT tiene `client_id`.

El backend filtra obligatoriamente:

- CLIENT;
- IMPORT_REQUEST;
- QUOTATION;
- IMPORT_OPERATION;
- SHIPMENT;
- CUSTOMS;
- DOCUMENTS.

Un usuario de `Cliente Demo Andino SAC` no puede obtener registros de `Cliente Demo Pacífico SAC` aunque conozca el ID.

## Permisos cliente

CLIENT_ADMIN y CLIENT_USER:

- CLIENTS.VIEW
- REQUESTS.VIEW
- REQUESTS.CREATE
- QUOTATIONS.VIEW
- OPERATIONS.VIEW
- SHIPMENTS.VIEW
- CUSTOMS.VIEW
- DOCUMENTS.VIEW
- DOCUMENTS.UPLOAD

No reciben:

- QUOTATIONS.MANAGE / APPROVE
- OPERATIONS.MANAGE / CLOSE
- CUSTOMS.MANAGE / RELEASE
- FINANCE.VIEW
- SECURITY.MANAGE

## Visibilidad

### Operation Notes

Los usuarios CLIENT solo reciben notas `CUSTOMER`.

`INTERNAL` e `INCIDENT` quedan ocultas del portal cliente.

### Operation Events

Los eventos tienen:

- INTERNAL
- CLIENT
- BOTH

Los usuarios cliente solo reciben CLIENT/BOTH.

### Documents

Los documentos tienen:

- INTERNAL
- CLIENT
- BOTH

Un usuario cliente:
- no puede consultar INTERNAL;
- no puede cargar INTERNAL.

## Migración futura a JWT/Keycloak

Roles, permisos, user_type y client_id no cambian.

Solo se reemplaza Cookie Auth por el mecanismo que construye el `ClaimsPrincipal`.
