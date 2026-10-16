using System;

namespace SecureFileManager.Domain.Entities
{
    public class AdminOtp
    {
        public Guid Id { get; set; }
        public string EmailOrMobile { get; set; } = null!; // target
        public byte[] CodeHash { get; set; } = null!; // HMAC/SHA256
        public byte[] Salt { get; set; } = null!;
        public string Purpose { get; set; } = null!; // Registration | PasswordReset | Login
        public int AttemptCount { get; set; }
        public bool IsUsed { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
