using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SecureFileManager.Infrastructure.Services
{
    public class JwtService
    {
        private readonly IConfiguration _config;

        public JwtService(IConfiguration config)
        {
            _config = config;
        }

        public string CreateToken(string userId, string username)
        {
            var issuer = _config.GetValue<string>("Jwt:Issuer") ?? "SecureFileManager";
            var audience = _config.GetValue<string>("Jwt:Audience") ?? "SecureFileManagerClient";
            var key = _config.GetValue<string>("Jwt:SigningKey") ?? "development-signing-key-please-replace";
            var minutes = _config.GetValue<int?>("Jwt:AccessTokenMinutes") ?? 15;

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var creds = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var claims = new[] {
                new Claim(JwtRegisteredClaimNames.Sub, userId),
                new Claim(JwtRegisteredClaimNames.UniqueName, username)
            };
            var token = new JwtSecurityToken(issuer, audience, claims, expires: DateTime.UtcNow.AddMinutes(minutes), signingCredentials: creds);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
