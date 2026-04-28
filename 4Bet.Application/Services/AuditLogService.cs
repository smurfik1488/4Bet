using System.Text.Json;
using _4Bet.Application.IServices;
using _4Bet.Infrastructure.Data;
using _4Bet.Infrastructure.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace _4Bet.Application.Services;

public class AuditLogService(FourBetDbContext dbContext, ILogger<AuditLogService> logger) : IAuditLogService
{
    public async Task LogAsync(
        string action,
        string entityType,
        Guid? entityId,
        Guid? userId,
        string summary,
        object? payload = null,
        CancellationToken cancellationToken = default)
    {
        var log = new AuditLog
        {
            Id = Guid.NewGuid(),
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            UserId = userId,
            Summary = summary,
            PayloadJson = payload is null ? null : JsonSerializer.Serialize(payload),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        try
        {
            dbContext.AuditLogs.Add(log);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            // Do not break critical user flows if migration was not applied yet.
            logger.LogWarning(ex, "Audit log write skipped. Ensure latest migration is applied.");
        }
    }
}
