using SecureFileManager.Domain.Entities;

namespace SecureFileManager.Application.Interfaces
{
    public interface IAuditLogService
    {
        Task LogAsync(
            string action,
            string entityType,
            string? entityId,
            Guid? sourceFolderId,
            Guid? destinationFolderId,
            string status,
            string? details,
            string? ipAddress,
            string? userAgent,
            string? correlationId,
            CancellationToken cancellationToken = default);

        Task<IEnumerable<AuditLog>> GetLogsAsync(
            string? action = null,
            string? status = null,
            string? entityType = null,
            DateTime? fromUtc = null,
            DateTime? toUtc = null,
            int page = 1,
            int pageSize = 50,
            CancellationToken cancellationToken = default);
    }
}
