namespace _4Bet.Application.IServices;

public interface IAuditLogService
{
    Task LogAsync(
        string action,
        string entityType,
        Guid? entityId,
        Guid? userId,
        string summary,
        object? payload = null,
        CancellationToken cancellationToken = default);
}
