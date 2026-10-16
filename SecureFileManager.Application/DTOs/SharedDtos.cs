using System;

namespace SecureFileManager.Application.DTOs
{
    public record FolderDto(Guid Id, string Name, Guid? ParentFolderId, DateTime CreatedAt, DateTime UpdatedAt);
    public record FileDto(Guid Id, string OriginalFileName, string ContentType, long OriginalSizeBytes, DateTime UploadedAt, Guid FolderId);
    public record LoginRequest(string Username, string Password);
    public record AuthenticateResult(bool Succeeded, string? Token, string? FailureReason = null);
}
