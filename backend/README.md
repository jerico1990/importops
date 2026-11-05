# ImportOps Backend

API ASP.NET Core .NET 8.

## Configuración

Definir la cadena de conexión mediante la variable de entorno `ConnectionStrings__ImportOpsDb` o mediante secretos de usuario de .NET. No guardar credenciales en Git.

```powershell
dotnet restore
dotnet build
```

Para Azure App Service, establecer `ConnectionStrings__ImportOpsDb` en la configuración de la aplicación y revisar los orígenes CORS autorizados.
