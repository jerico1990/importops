using System.Text.Json;
using System.Buffers.Binary;
using ImportOps.Application.Common;
using ImportOps.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ImportOps.Infrastructure.Persistence;

public sealed class AuditSaveChangesInterceptor(
    ICurrentUser currentUser,
    ICurrentCompany currentCompany,
    IRequestMetadata requestMetadata) : SaveChangesInterceptor
{
    private static readonly HashSet<string> IgnoredProperties =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "RowVer"
        };

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        AddAuditEntries(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        AddAuditEntries(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void AddAuditEntries(DbContext? context)
    {
        if (context is null) return;

        var entries = context.ChangeTracker.Entries()
            .Where(x =>
                x.Entity is not AuditLog &&
                x.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        if (entries.Count == 0) return;

        long? userId = null;
        long? companyId = null;

        try { userId = currentUser.UserId; } catch { }
        try { companyId = currentCompany.CompanyId; } catch { }

        foreach (var entry in entries)
        {
            AdvanceRowVersion(entry);

            var oldValues = entry.State == EntityState.Added ? null : Values(entry, original: true);
            var newValues = entry.State == EntityState.Deleted
                ? null
                : Values(entry, original: false);

            context.Set<AuditLog>().Add(new AuditLog
            {
                CompanyId = companyId,
                UserId = userId,
                EventAt = DateTime.UtcNow,
                EntityName = entry.Metadata.GetTableName()
                ?? entry.Metadata.ClrType.Name,
                        EntityId = EntityId(entry),

                        ActionType = entry.State == EntityState.Deleted
                ? "DELETE"
                : entry.State == EntityState.Added
                    ? "INSERT"
                    : "UPDATE",

                        OldValuesJson = oldValues is null
                ? null
                : JsonSerializer.Serialize(oldValues),

                        NewValuesJson = newValues is null
                ? null
                : JsonSerializer.Serialize(newValues),

                IpAddress = requestMetadata.IpAddress,
                UserAgent = requestMetadata.UserAgent,
                CorrelationId = requestMetadata.CorrelationId
            });
        }
    }

    private static void AdvanceRowVersion(EntityEntry entry)
    {
        var rowVersion = entry.Properties.FirstOrDefault(property => property.Metadata.Name == "RowVer");
        if (rowVersion is null)
            return;

        if (entry.State == EntityState.Added)
        {
            rowVersion.CurrentValue = new byte[sizeof(ulong)];
            return;
        }

        var original = rowVersion.OriginalValue as byte[] ?? Array.Empty<byte>();
        Span<byte> current = stackalloc byte[sizeof(ulong)];
        var currentValue = original.AsSpan(Math.Max(0, original.Length - current.Length));
        currentValue.CopyTo(current[(current.Length - currentValue.Length)..]);

        var next = BinaryPrimitives.ReadUInt64BigEndian(current) + 1;
        var nextBytes = new byte[sizeof(ulong)];
        BinaryPrimitives.WriteUInt64BigEndian(nextBytes, next);
        rowVersion.CurrentValue = nextBytes;
        rowVersion.IsModified = true;
    }

    private static Dictionary<string, object?> Values(EntityEntry entry, bool original)
    {
        var values = new Dictionary<string, object?>();

        foreach (var property in entry.Properties)
        {
            if (IgnoredProperties.Contains(property.Metadata.Name))
                continue;

            if (entry.State == EntityState.Modified && !property.IsModified)
                continue;

            values[property.Metadata.Name] =
                original ? property.OriginalValue : property.CurrentValue;
        }

        return values;
    }

    private static string? EntityId(EntityEntry entry)
    {
        var pk = entry.Metadata.FindPrimaryKey();
        if (pk is null) return null;

        var parts = pk.Properties
            .Select(p => entry.Property(p.Name).CurrentValue?.ToString())
            .Where(x => !string.IsNullOrWhiteSpace(x));

        var value = string.Join("|", parts);
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
