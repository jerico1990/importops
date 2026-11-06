using System.Data;
using Microsoft.EntityFrameworkCore;

namespace ImportOps.Infrastructure.Persistence;

internal static class MariaDbSequence
{
    public static async Task<long> NextAsync(
        ImportOpsDbContext db,
        string sequenceKey,
        CancellationToken cancellationToken)
    {
        var connection = db.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose)
            await connection.OpenAsync(cancellationToken);

        try
        {
            await using (var update = connection.CreateCommand())
            {
                update.CommandText = "UPDATE sys__APP_SEQUENCE SET next_value = LAST_INSERT_ID(next_value + 1) WHERE sequence_key = @sequenceKey;";
                var parameter = update.CreateParameter();
                parameter.ParameterName = "@sequenceKey";
                parameter.Value = sequenceKey;
                update.Parameters.Add(parameter);

                if (await update.ExecuteNonQueryAsync(cancellationToken) != 1)
                    throw new InvalidOperationException($"No existe la secuencia MariaDB '{sequenceKey}'.");
            }

            await using var read = connection.CreateCommand();
            read.CommandText = "SELECT LAST_INSERT_ID();";
            var value = await read.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt64(value);
        }
        finally
        {
            if (shouldClose)
                await connection.CloseAsync();
        }
    }
}
