using System.Security.Cryptography;
using System.Text;

namespace SecureFileManager.Infrastructure.Services
{
    public class EncryptionService
    {
        private readonly IConfiguration _configuration;

        public EncryptionService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // Generate a random 32-byte key
        public byte[] GenerateDataKey()
        {
            var key = new byte[32];
            RandomNumberGenerator.Fill(key);
            return key;
        }

        // Protect key using DPAPI (Windows) or fallback symmetric wrap in development
        public byte[] ProtectDataKey(byte[] dataKey)
        {
            var useDpapi = _configuration.GetValue<bool>("KeyProtection:UseDpapi", true);
            if (useDpapi && OperatingSystem.IsWindows())
            {
                return ProtectedData.Protect(dataKey, null, DataProtectionScope.LocalMachine);
            }
            // Development fallback: encrypt with appsettings key
            var fallback = _configuration.GetValue<string>("KeyProtection:FallbackKey") ?? "dev-fallback-key-please-change-123456";
            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(fallback.PadRight(32).Substring(0,32));
            aes.Mode = CipherMode.CBC;
            aes.GenerateIV();
            using var encryptor = aes.CreateEncryptor();
            var cipher = encryptor.TransformFinalBlock(dataKey, 0, dataKey.Length);
            var result = new byte[aes.IV.Length + cipher.Length];
            Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
            Buffer.BlockCopy(cipher, 0, result, aes.IV.Length, cipher.Length);
            return result;
        }

        // Unprotect
        public byte[] UnprotectDataKey(byte[] wrapped)
        {
            var useDpapi = _configuration.GetValue<bool>("KeyProtection:UseDpapi", true);
            if (useDpapi && OperatingSystem.IsWindows())
            {
                return ProtectedData.Unprotect(wrapped, null, DataProtectionScope.LocalMachine);
            }
            var fallback = _configuration.GetValue<string>("KeyProtection:FallbackKey") ?? "dev-fallback-key-please-change-123456";
            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(fallback.PadRight(32).Substring(0,32));
            aes.Mode = CipherMode.CBC;
            var iv = new byte[16];
            Buffer.BlockCopy(wrapped, 0, iv, 0, iv.Length);
            var cipher = new byte[wrapped.Length - iv.Length];
            Buffer.BlockCopy(wrapped, iv.Length, cipher, 0, cipher.Length);
            aes.IV = iv;
            using var decryptor = aes.CreateDecryptor();
            var plain = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
            return plain;
        }
    }
}
