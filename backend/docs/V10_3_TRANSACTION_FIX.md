# V10.3 - SQL Server transaction fix

## Problema
La configuración usaba:

```csharp
UseSqlServer(cs, sql => sql.EnableRetryOnFailure(3))
```

y el backend usa transacciones explícitas mediante `BeginTransactionAsync()` en varios
módulos.

`SqlServerRetryingExecutionStrategy` no permite transacciones iniciadas por el usuario
fuera de `CreateExecutionStrategy().ExecuteAsync(...)`.

## Corrección V10.3

Para la V1 se usa:

```csharp
UseSqlServer(cs)
```

Esto conserva las transacciones explícitas ya implementadas y evita que fallen:
- Security / Users
- Import Requests
- Quotations
- Operations
- Shipment
- Customs
- Documents
- Finance

Si en producción se desea habilitar retry de conexión, primero debe envolverse cada
unidad transaccional con la execution strategy de EF Core. No volver a activar
`EnableRetryOnFailure` sin ese refactor.
