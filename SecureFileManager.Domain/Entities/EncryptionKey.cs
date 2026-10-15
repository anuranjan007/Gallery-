using System;

namespace SecureFileManager.Domain.Entities
{
    public class EncryptionKey
    {
        public Guid Id { get; set; }
        public int KeyVersion { get; set; }
        public string KeyProvider { get; set; } = null!;
        public byte[] WrappedDataKey { get; set; } = null!;
        public byte[]? NoncePrefix { get; set; }
        public int ChunkSizeBytes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? RetiredAt { get; set; }
    }
}
