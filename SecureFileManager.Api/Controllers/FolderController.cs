using Microsoft.AspNetCore.Mvc;
using SecureFileManager.Infrastructure.Data;
using SecureFileManager.Application.DTOs;
using SecureFileManager.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;

namespace SecureFileManager.Api.Controllers
{
    [ApiController]
    [Route("api/folders")]
    [Authorize]
    public class FolderController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IFolderService _folderService;
        private readonly IAuditLogService _audit;

        public FolderController(AppDbContext db, IFolderService folderService, IAuditLogService audit)
        {
            _db = db;
            _folderService = folderService;
            _audit = audit;
        }

        [HttpGet]
        public async Task<IActionResult> GetRoot()
        {
            var items = await _folderService.GetRootFoldersAsync(CancellationToken.None);
            return Ok(items.Select(f => new FolderDto(f.Id, f.Name, f.ParentFolderId, f.CreatedAt, f.UpdatedAt)));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var f = await _folderService.GetByIdAsync(id, CancellationToken.None);
            if (f == null) return NotFound();
            return Ok(new FolderDto(f.Id, f.Name, f.ParentFolderId, f.CreatedAt, f.UpdatedAt));
        }

        [HttpGet("{id}/contents")]
        public async Task<IActionResult> GetContents(Guid id)
        {
            var folder = await _folderService.GetByIdAsync(id, CancellationToken.None);
            if (folder == null) return NotFound();
            var children = await _folderService.GetChildFoldersAsync(id, CancellationToken.None);
            var files = await _folderService.GetFilesInFolderAsync(id, CancellationToken.None);
            return Ok(new {
                folder = new FolderDto(folder.Id, folder.Name, folder.ParentFolderId, folder.CreatedAt, folder.UpdatedAt),
                folders = children.Select(f => new FolderDto(f.Id, f.Name, f.ParentFolderId, f.CreatedAt, f.UpdatedAt)),
                files = files.Select(f => new FileDto(f.Id, f.OriginalFileName, f.ContentType, f.OriginalSizeBytes, f.UploadedAt, f.FolderId))
            });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] dynamic body)
        {
            string name = body?.name ?? string.Empty;
            Guid? parent = null;
            if (body?.parentFolderId != null) parent = Guid.Parse((string)body.parentFolderId.ToString());
            var folder = await _folderService.CreateAsync(name, parent, CancellationToken.None);
            await _audit.LogAsync("FolderCreated", "folder", folder.Id.ToString(), folder.ParentFolderId, null, "success", $"Created folder {folder.Name}", HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers["User-Agent"].ToString(), Request.Headers["X-Correlation-Id"].ToString());
            return CreatedAtAction(nameof(GetById), new { id = folder.Id }, new FolderDto(folder.Id, folder.Name, folder.ParentFolderId, folder.CreatedAt, folder.UpdatedAt));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Rename(Guid id, [FromBody] dynamic body)
        {
            string name = body?.name ?? string.Empty;
            var folder = await _folderService.RenameAsync(id, name, CancellationToken.None);
            await _audit.LogAsync("FolderRenamed", "folder", folder.Id.ToString(), folder.ParentFolderId, folder.ParentFolderId, "success", $"Renamed to {folder.Name}", HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers["User-Agent"].ToString(), Request.Headers["X-Correlation-Id"].ToString());
            return Ok(new FolderDto(folder.Id, folder.Name, folder.ParentFolderId, folder.CreatedAt, folder.UpdatedAt));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id, [FromQuery] bool recursive = false)
        {
            var before = await _folderService.GetByIdAsync(id, CancellationToken.None);
            if (before == null) return NotFound();
            await _folderService.DeleteAsync(id, recursive, CancellationToken.None);
            await _audit.LogAsync("FolderDeleted", "folder", before.Id.ToString(), before.ParentFolderId, null, "success", recursive ? "Deleted recursively" : "Deleted", HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers["User-Agent"].ToString(), Request.Headers["X-Correlation-Id"].ToString());
            return NoContent();
        }
    }
}
