using System;

namespace SecureFileManager.Domain.Entities
{
    public class FileRecord
    {
        public Guid Id { get; set; }
        public Guid FolderId { get; set; }
        public Folder? Folder { get; set; }
        public string OriginalFileName { get; set; } = null!;
        public string StoredFileName { get; set; } = null!;
        public string StoragePath { get; set; } = null!;
        public string ContentType { get; set; } = null!;
        public string FileExtension { get; set; } = null!;
        public long OriginalSizeBytes { get; set; }
        public long EncryptedSizeBytes { get; set; }
        public Guid EncryptionKeyId { get; set; }
        public string EncryptionAlgorithm { get; set; } = "AES-256-GCM";
        public int EncryptionFormatVersion { get; set; } = 1;
        public DateTime UploadedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
