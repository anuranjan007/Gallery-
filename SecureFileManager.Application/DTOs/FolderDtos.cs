using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureFileManager.Application.Interfaces;
using SecureFileManager.Application.DTOs;

namespace SecureFileManager.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class FoldersController : ControllerBase
    {
        private readonly IFolderService _folderService;
        private readonly IAuditLogService _auditLogService;

        public FoldersController(IFolderService folderService, IAuditLogService auditLogService)
        {
            _folderService = folderService;
            _auditLogService = auditLogService;
        }

        [HttpGet]
        public async Task<IActionResult> GetRootFolders(CancellationToken cancellationToken)
        {
            var folders = await _folderService.GetRootFoldersAsync(cancellationToken);
            return Ok(folders.Select(f => new FolderDto(f.Id, f.Name, f.ParentFolderId, f.CreatedAt, f.UpdatedAt)));
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var folder = await _folderService.GetByIdAsync(id, cancellationToken);
            if (folder == null) return NotFound();
            return Ok(new FolderDto(folder.Id, folder.Name, folder.ParentFolderId, folder.CreatedAt, folder.UpdatedAt));
        }

        [HttpGet("{id:guid}/contents")]
        public async Task<IActionResult> GetContents(Guid id, CancellationToken cancellationToken)
        {
            var folder = await _folderService.GetByIdAsync(id, cancellationToken);
            if (folder == null) return NotFound();

            var childFolders = await _folderService.GetChildFoldersAsync(id, cancellationToken);
            var files = await _folderService.GetFilesInFolderAsync(id, cancellationToken);

            return Ok(new
            {
                folder = new FolderDto(folder.Id, folder.Name, folder.ParentFolderId, folder.CreatedAt, folder.UpdatedAt),
                folders = childFolders.Select(f => new FolderDto(f.Id, f.Name, f.ParentFolderId, f.CreatedAt, f.UpdatedAt)),
                files = files.Select(f => new FileDto(f.Id, f.OriginalFileName, f.ContentType, f.OriginalSizeBytes, f.UploadedAt, f.FolderId))
            });
        }

        [HttpPost]
        public async Task<IActionResult> CreateFolder([FromBody] CreateFolderRequest request, CancellationToken cancellationToken)
        {
            var folder = await _folderService.CreateAsync(request.Name, request.ParentFolderId, cancellationToken);

            await _auditLogService.LogAsync(
                "FolderCreated",
                "folder",
                folder.Id.ToString(),
                null,
                folder.ParentFolderId,
                "success",
                $"Created folder '{folder.Name}'",
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers["User-Agent"].ToString(),
                Request.Headers["X-Correlation-Id"].ToString(),
                cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = folder.Id }, new FolderDto(folder.Id, folder.Name, folder.ParentFolderId, folder.CreatedAt, folder.UpdatedAt));
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> RenameFolder(Guid id, [FromBody] RenameFolderRequest request, CancellationToken cancellationToken)
        {
            var folder = await _folderService.RenameAsync(id, request.Name, cancellationToken);

            await _auditLogService.LogAsync(
                "FolderRenamed",
                "folder",
                folder.Id.ToString(),
                folder.ParentFolderId,
                folder.ParentFolderId,
                "success",
                $"Renamed folder to '{folder.Name}'",
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers["User-Agent"].ToString(),
                Request.Headers["X-Correlation-Id"].ToString(),
                cancellationToken);

            return Ok(new FolderDto(folder.Id, folder.Name, folder.ParentFolderId, folder.CreatedAt, folder.UpdatedAt));
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteFolder(Guid id, [FromQuery] bool recursive = false, CancellationToken cancellationToken = default)
        {
            var before = await _folderService.GetByIdAsync(id, cancellationToken);
            if (before == null) return NotFound();

            await _folderService.DeleteAsync(id, recursive, cancellationToken);

            await _auditLogService.LogAsync(
                "FolderDeleted",
                "folder",
                before.Id.ToString(),
                before.ParentFolderId,
                null,
                "success",
                recursive ? "Deleted folder recursively" : "Deleted folder",
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers["User-Agent"].ToString(),
                Request.Headers["X-Correlation-Id"].ToString(),
                cancellationToken);

            return NoContent();
        }
    }

    public record CreateFolderRequest(string Name, Guid? ParentFolderId);
    public record RenameFolderRequest(string Name);
}
