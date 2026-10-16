using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using SecureFileManager.Application.DTOs;
using SecureFileManager.Domain.Entities;
using SecureFileManager.Infrastructure.Data;

namespace SecureFileManager.Infrastructure.Services
{
    public interface IAdminOtpService
    {
        Task<AdminOtp> CreateOtpAsync(string emailOrMobile, string purpose, TimeSpan ttl, CancellationToken ct = default);
        Task<bool> VerifyOtpAsync(string emailOrMobile, string code, string purpose, CancellationToken ct = default);
    }

    public class AdminOtpService : IAdminOtpService
    {
        private readonly AppDbContext _db;
        private readonly IEmailService _email;

        public AdminOtpService(AppDbContext db, IEmailService email)
        {
            _db = db;
            _email = email;
        }

        public async Task<AdminOtp> CreateOtpAsync(string emailOrMobile, string purpose, TimeSpan ttl, CancellationToken ct = default)
        {
            // generate 6-digit code
            var rng = RandomNumberGenerator.Create();
            var codeBytes = new byte[4];
            rng.GetBytes(codeBytes);
            var codeInt = Math.Abs(BitConverter.ToInt32(codeBytes, 0)) % 1000000;
            var code = codeInt.ToString("D6");

            // salt and HMAC-SHA256
            var salt = new byte[16]; rng.GetBytes(salt);
            using var hmac = new HMACSHA256(salt);
            var hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(code));

            var otp = new AdminOtp
            {
                Id = Guid.NewGuid(),
                EmailOrMobile = emailOrMobile,
                CodeHash = hash,
                Salt = salt,
                Purpose = purpose,
                AttemptCount = 0,
                IsUsed = false,
                ExpiresAt = DateTime.UtcNow.Add(ttl),
                CreatedAt = DateTime.UtcNow
            };

            _db.AdminOtps.Add(otp);
            await _db.SaveChangesAsync(ct);

            // send via email service (subject/body simple)
            await _email.SendAsync(emailOrMobile, "Your OTP Code", $"Your verification code is: {code}");

            return otp;
        }

        public async Task<bool> VerifyOtpAsync(string emailOrMobile, string code, string purpose, CancellationToken ct = default)
        {
            var otp = await _db.AdminOtps.Where(x => x.EmailOrMobile == emailOrMobile && x.Purpose == purpose && !x.IsUsed && x.ExpiresAt > DateTime.UtcNow).OrderByDescending(x => x.CreatedAt).FirstOrDefaultAsync(ct);
            if (otp == null) return false;

            using var hmac = new HMACSHA256(otp.Salt);
            var hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(code));
            if (!hash.SequenceEqual(otp.CodeHash))
            {
                otp.AttemptCount++;
                await _db.SaveChangesAsync(ct);
                return false;
            }

            otp.IsUsed = true;
            await _db.SaveChangesAsync(ct);
            return true;
        }
    }
}
