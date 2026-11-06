# ImportOps

## Levantar el stack con Docker Compose

Requisitos: Docker Desktop con Docker Compose.

```powershell
docker compose up --build -d
```

La primera ejecución crea MariaDB e importa `bd/ImportOpsDB_MariaDB_COMPLETO.sql`; la importación puede tardar unos minutos. La API espera a que la base de datos esté lista.

- Aplicación web: <http://localhost:5173>
- API y Swagger: <http://localhost:5090/swagger>
- MariaDB desde el equipo: `localhost:3307`, base `ImportOpsDB`

Para detener los contenedores conserva los datos con `docker compose down`. La base y los documentos subidos viven en volúmenes de Docker.

Las credenciales locales tienen valores de desarrollo predeterminados. Para personalizarlas, copia `.env.example` a `.env` y cambia `DB_PASSWORD` y `MARIADB_ROOT_PASSWORD` antes de iniciar el stack. Cambiar esas variables después de crear el volumen no modifica las contraseñas de la base ya inicializada.
