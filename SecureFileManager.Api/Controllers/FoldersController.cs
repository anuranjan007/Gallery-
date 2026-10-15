using Microsoft.EntityFrameworkCore;
using SecureFileManager.Application.Interfaces;
using SecureFileManager.Domain.Entities;
using SecureFileManager.Infrastructure.Data;

namespace SecureFileManager.Infrastructure.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly AppDbContext _db;

        public AuditLogService(AppDbContext db)
        {
            _db = db;
        }

        public async Task LogAsync(
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
            CancellationToken cancellationToken = default)
        {
            var log = new AuditLog
            {
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                SourceFolderId = sourceFolderId,
                DestinationFolderId = destinationFolderId,
                Status = status,
                Details = details,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                CorrelationId = correlationId,
                CreatedAt = DateTime.UtcNow
            };

            _db.AuditLogs.Add(log);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<IEnumerable<AuditLog>> GetLogsAsync(
            string? action = null,
            string? status = null,
            string? entityType = null,
            DateTime? fromUtc = null,
            DateTime? toUtc = null,
            int page = 1,
            int pageSize = 50,
            CancellationToken cancellationToken = default)
        {
            var query = _db.AuditLogs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(action)) query = query.Where(x => x.Action == action);
            if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status);
            if (!string.IsNullOrWhiteSpace(entityType)) query = query.Where(x => x.EntityType == entityType);
            if (fromUtc.HasValue) query = query.Where(x => x.CreatedAt >= fromUtc.Value);
            if (toUtc.HasValue) query = query.Where(x => x.CreatedAt <= toUtc.Value);

            return await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }
    }
}
