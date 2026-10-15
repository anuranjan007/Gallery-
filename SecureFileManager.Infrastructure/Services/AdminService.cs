using SecureFileManager.Application.DTOs;
using SecureFileManager.Application.Interfaces;
using SecureFileManager.Domain.Entities;
using SecureFileManager.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SecureFileManager.Infrastructure.Services
{
    public class AdminService : IAdminService
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;
        private readonly IPasswordHasher<AdminUser> _passwordHasher;

        public AdminService(AppDbContext db, IConfiguration config, IPasswordHasher<AdminUser> passwordHasher)
        {
            _db = db;
            _config = config;
            _passwordHasher = passwordHasher;
        }

        public async Task<AuthenticateResult> AuthenticateAsync(string username, string password, CancellationToken cancellationToken)
        {
            var admin = await _db.AdminUsers.SingleOrDefaultAsync(x => x.Username == username, cancellationToken);
            if (admin == null) return new AuthenticateResult(false, null, "Invalid credentials");
            if (!admin.IsActive) return new AuthenticateResult(false, null, "Account disabled");

            var verify = _passwordHasher.VerifyHashedPassword(admin, admin.PasswordHash, password);
            if (verify == PasswordVerificationResult.Failed) return new AuthenticateResult(false, null, "Invalid credentials");

            // generate JWT
            var jwtSvc = new JwtService(_config);
            var token = jwtSvc.CreateToken(admin.Id.ToString(), admin.Username);
            return new AuthenticateResult(true, token);
        }

        public async Task<bool> ProvisionAdminAsync(string username, string password, CancellationToken cancellationToken)
        {
            if (await _db.AdminUsers.AnyAsync(cancellationToken)) return false; // only allow if none exist
            var now = DateTime.UtcNow;
            var admin = new AdminUser { Id = Guid.NewGuid(), Username = username, CreatedAt = now, UpdatedAt = now };
            admin.PasswordHash = _passwordHasher.HashPassword(admin, password);
            _db.AdminUsers.Add(admin);
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
