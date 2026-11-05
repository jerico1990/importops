# ImportOps Backend V10.2 — Users & Access

## Account manager catalog
`GET /api/catalogs/users?purpose=ACCOUNT_MANAGER`

Returns only active INTERNAL users, excluding technical `system.*` accounts,
with an active ADMIN or COMMERCIAL role.

Client create/update also validates the same invariant server-side.

Additional supported purpose:
`OPERATION_RESPONSIBLE` => INTERNAL ADMIN/OPERATIONS.

## Security endpoints
- GET /api/security/users
  - search
  - userType
  - clientId
  - isActive
  - page
  - pageSize
- GET /api/security/roles
- POST /api/security/users
- PUT /api/security/users/{userId}
- PUT /api/security/users/{userId}/roles
- PUT /api/security/users/{userId}/active
- POST /api/security/users/{userId}/reset-password

V10.2 does not require a database schema patch. It uses the V10.1
APP_USER / ROLE / USER_ROLE structure already installed.
