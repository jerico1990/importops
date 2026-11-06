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

## Desplegar en Railway desde este monorepo

Conecta el mismo repositorio GitHub a tres servicios del mismo proyecto Railway:

1. **api**: root directory `/backend`; Railway detecta `backend/Dockerfile`.
2. **frontend**: root directory `/frontend`; Railway detecta `frontend/Dockerfile`.
3. **mariadb**: usa la raiz del repositorio y configura `RAILWAY_DOCKERFILE_PATH=/Dockerfile.mariadb`.

Configura las variables del servicio `mariadb`: `MARIADB_DATABASE=ImportOpsDB`, `MARIADB_USER`, `MARIADB_PASSWORD` y `MARIADB_ROOT_PASSWORD`. Adjunta un volumen en `/var/lib/mysql`; el dump y las secuencias se ejecutan al inicializar un volumen vacio. El archivo `bd/ImportOpsDB_MariaDB_COMPLETO.sql` debe estar incluido en Git para que Railway pueda copiarlo al construir la imagen.

En `api`, configura `ASPNETCORE_ENVIRONMENT=Production`, `ConnectionStrings__ImportOpsDb` con el host privado `mariadb.railway.internal:3306` y las credenciales del servicio MariaDB, `Cors__AllowedOrigins__0` con el dominio publico del frontend y `Storage__LocalRoot=/app/storage`. Adjunta un volumen en `/app/storage` para conservar documentos y establece el healthcheck path `/health/ready`.

En `frontend`, configura `VITE_API_BASE_URL` como variable de build con la URL HTTPS publica de la API. El Dockerfile la incorpora al bundle durante `npm run build` y Nginx escucha en el puerto `PORT` asignado por Railway.

Referencias de variables para el proyecto Railway (nombra los servicios exactamente `api`, `frontend` y `mariadb`):

- `api.ConnectionStrings__ImportOpsDb`: `Server=${{mariadb.RAILWAY_PRIVATE_DOMAIN}};Port=3306;Database=${{mariadb.MARIADB_DATABASE}};User=${{mariadb.MARIADB_USER}};Password=${{mariadb.MARIADB_PASSWORD}};CharSet=utf8mb4`
- `api.Cors__AllowedOrigins__0`: `https://${{frontend.RAILWAY_PUBLIC_DOMAIN}}`
- `frontend.VITE_API_BASE_URL`: `https://${{api.RAILWAY_PUBLIC_DOMAIN}}`

Genera los dominios publicos de `api` y `frontend` en Railway antes de configurar esas referencias. La API solo necesita acceso privado desde MariaDB; la base no requiere dominio publico.
