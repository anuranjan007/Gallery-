using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureFileManager.Application.DTOs;
using SecureFileManager.Application.Interfaces;

namespace SecureFileManager.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public AuthController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest dto, CancellationToken cancellationToken)
        {
            var result = await _adminService.AuthenticateAsync(dto.Username, dto.Password, cancellationToken);
            if (!result.Succeeded) return Unauthorized(new { message = "Invalid credentials" });
            return Ok(new { token = result.Token });
        }
    }
}
