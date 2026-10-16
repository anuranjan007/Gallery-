using Microsoft.AspNetCore.Mvc;
using SecureFileManager.Application.DTOs;

namespace SecureFileManager.Api.Controllers
{
    [ApiController]
    [Route("api/mock/folders")]
    public class MockFoldersController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetRoot()
        {
            var list = new[]
            {
                new FolderDto(System.Guid.NewGuid(), "Documents", (System.Guid?)null, System.DateTime.UtcNow.AddDays(-10), System.DateTime.UtcNow),
                new FolderDto(System.Guid.NewGuid(), "Personal", (System.Guid?)null, System.DateTime.UtcNow.AddDays(-20), System.DateTime.UtcNow),
                new FolderDto(System.Guid.NewGuid(), "Projects", (System.Guid?)null, System.DateTime.UtcNow.AddDays(-2), System.DateTime.UtcNow),
            };
            return Ok(list);
        }

        [HttpPost]
        public IActionResult Create([FromBody] FolderDto dto)
        {
            var created = dto with { Id = System.Guid.NewGuid(), CreatedAt = System.DateTime.UtcNow, UpdatedAt = System.DateTime.UtcNow };
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpGet("{id}")]
        public IActionResult GetById(System.Guid id)
        {
            return Ok(new FolderDto(id, "MockFolder", (System.Guid?)null, System.DateTime.UtcNow.AddDays(-1), System.DateTime.UtcNow));
        }
    }
}
