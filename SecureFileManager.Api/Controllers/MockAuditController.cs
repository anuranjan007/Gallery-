using Microsoft.AspNetCore.Mvc;

namespace SecureFileManager.Api.Controllers
{
    [ApiController]
    [Route("api/mock/audit")]
    public class MockAuditController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            var logs = Enumerable.Range(1, 10).Select(i => new {
                Id = i,
                Action = "MockAction",
                EntityType = "file",
                EntityId = Guid.NewGuid().ToString(),
                Status = "success",
                Details = "Mock audit log entry",
                CreatedAt = DateTime.UtcNow.AddMinutes(-i * 5)
            });
            return Ok(new { total = 10, items = logs });
        }
    }
}
