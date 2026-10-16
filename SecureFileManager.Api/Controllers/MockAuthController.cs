using Microsoft.AspNetCore.Mvc;
using SecureFileManager.Application.DTOs;

namespace SecureFileManager.Api.Controllers
{
    [ApiController]
    [Route("api/mock/auth")]
    public class MockAuthController : ControllerBase
    {
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest req)
        {
            if ((req.Username == "admin" && req.Password == "password") || req.Username.Contains("@"))
            {
                return Ok(new { token = "mock-token-123" });
            }
            return Unauthorized(new { message = "Invalid credentials" });
        }

        [HttpPost("request-otp")]
        public IActionResult RequestOtp([FromBody] object body)
        {
            // In mock mode we always succeed and "send" code 123456
            return Ok(new { message = "OTP sent (mock)", expiresInSeconds = 900 });
        }

        [HttpPost("verify-otp")]
        public IActionResult VerifyOtp([FromBody] dynamic body)
        {
            string code = body?.code ?? "";
            if (code == "123456") return Ok(new { token = "mock-token-123" });
            return BadRequest(new { message = "Invalid OTP" });
        }
    }
}
