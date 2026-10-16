using Microsoft.AspNetCore.Mvc;
using SecureFileManager.Infrastructure.Services;
using SecureFileManager.Infrastructure.Data;
using SecureFileManager.Application.DTOs;
using SecureFileManager.Domain.Entities;
using Microsoft.AspNetCore.Authorization;

namespace SecureFileManager.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IAdminService _adminService;
        private readonly IAdminOtpService _otpService;
        private readonly IConfiguration _config;

        public AuthController(AppDbContext db, IAdminService adminService, IAdminOtpService otpService, IConfiguration config)
        {
            _db = db;
            _adminService = adminService;
            _otpService = otpService;
            _config = config;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            var res = await _adminService.AuthenticateAsync(req.Username, req.Password, CancellationToken.None);
            if (!res.Succeeded) return Unauthorized(new { message = res.FailureReason });
            return Ok(new { token = res.Token });
        }

        [HttpPost("request-otp")]
        public async Task<IActionResult> RequestOtp([FromBody] dynamic body)
        {
            string contact = body?.contact ?? string.Empty;
            if (string.IsNullOrWhiteSpace(contact)) return BadRequest(new { message = "contact required" });
            var ttl = TimeSpan.FromMinutes(15);
            var otp = await _otpService.CreateOtpAsync(contact, "Login", ttl);
            return Ok(new { message = "OTP sent" });
        }

        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] dynamic body)
        {
            string contact = body?.contact ?? string.Empty;
            string code = body?.code ?? string.Empty;
            if (string.IsNullOrWhiteSpace(contact) || string.IsNullOrWhiteSpace(code)) return BadRequest(new { message = "contact and code required" });
            var ok = await _otpService.VerifyOtpAsync(contact, code, "Login");
            if (!ok) return BadRequest(new { message = "Invalid code" });

            // If OTP verified, create/return a JWT for the admin (provision admin if not exists?)
            // For now, find first admin user and issue token for them
            var admin = await _db.AdminUsers.FirstOrDefaultAsync();
            if (admin == null) return StatusCode(500, new { message = "No admin registered" });

            var jwtSvc = new JwtService(_config);
            var token = jwtSvc.CreateToken(admin.Id.ToString(), admin.Username);
            return Ok(new { token = token });
        }

        [HttpPost("provision")]
        public async Task<IActionResult> Provision([FromBody] dynamic body)
        {
            string username = body?.username ?? string.Empty;
            string password = body?.password ?? string.Empty;
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password)) return BadRequest(new { message = "username/password required" });
            var ok = await _adminService.ProvisionAdminAsync(username, password, CancellationToken.None);
            if (!ok) return BadRequest(new { message = "Provisioning not allowed" });
            return Ok(new { message = "Admin provisioned" });
        }
    }
}
