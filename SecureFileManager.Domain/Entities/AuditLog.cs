using System;

namespace SecureFileManager.Domain.Entities
{
    public class AuditLog
    {
        public long Id { get; set; }
        public string Action { get; set; } = null!;
        public string EntityType { get; set; } = null!;
        public string? EntityId { get; set; }
        public Guid? SourceFolderId { get; set; }
        public Guid? DestinationFolderId { get; set; }
        public string Status { get; set; } = null!;
        public string? Details { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public string? CorrelationId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
