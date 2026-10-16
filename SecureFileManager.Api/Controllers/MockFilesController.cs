using Microsoft.AspNetCore.Mvc;
using SecureFileManager.Application.DTOs;

namespace SecureFileManager.Api.Controllers
{
    [ApiController]
    [Route("api/mock/files")]
    public class MockFilesController : ControllerBase
    {
        [HttpGet]
        public IActionResult List([FromQuery] Guid? folderId)
        {
            var files = new[]
            {
                new FileDto(Guid.NewGuid(), "Design.png", "image/png", 234567, DateTime.UtcNow.AddDays(-2), folderId ?? Guid.Empty),
                new FileDto(Guid.NewGuid(), "Agreement.pdf", "application/pdf", 123456, DateTime.UtcNow.AddDays(-10), folderId ?? Guid.Empty),
                new FileDto(Guid.NewGuid(), "Demo.mp4", "video/mp4", 12345678, DateTime.UtcNow.AddDays(-1), folderId ?? Guid.Empty),
            };
            return Ok(files);
        }

        [HttpPost("upload")]
        public IActionResult Upload([FromForm] IFormFile file, [FromForm] Guid folderId)
        {
            // Mock: do not persist, just return fake metadata
            var rec = new FileDto(Guid.NewGuid(), file.FileName, file.ContentType ?? "application/octet-stream", file.Length, DateTime.UtcNow, folderId);
            return CreatedAtAction(nameof(Get), new { id = rec.Id }, rec);
        }

        [HttpGet("{id}/download")]
        public IActionResult Download(Guid id)
        {
            // Return a small static file (text) for mock download
            var content = System.Text.Encoding.UTF8.GetBytes("This is mock file content for id: " + id);
            return File(content, "application/octet-stream", "mockfile.txt", enableRangeProcessing: false);
        }
    }
}
