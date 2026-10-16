using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SecureFileManager.Application.DTOs;
using SecureFileManager.Domain.Entities;
using SecureFileManager.Infrastructure.Data;
using SecureFileManager.Infrastructure.Services;
using System.Net.Mime;

namespace SecureFileManager.Api.Controllers
{
    [ApiController]
    [Route("api/files")]
    public class FilesController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly FileSystemStorageService _storage;
        private readonly EncryptionService _enc;
        private readonly IConfiguration _config;
        private readonly IAuditLogService _auditLog;

        public FilesController(AppDbContext db, FileSystemStorageService storage, EncryptionService enc, IConfiguration config, IAuditLogService auditLog)
        {
            _db = db;
            _storage = storage;
            _enc = enc;
            _config = config;
            _auditLog = auditLog;
        }

        [HttpPost("upload")]
        [RequestSizeLimit(long.MaxValue)]
        public async Task<IActionResult> Upload([FromForm] IFormFile file, [FromForm] Guid folderId, CancellationToken ct)
        {
            if (file == null) return BadRequest(new { message = "No file" });
            var folder = await _db.Folders.FindAsync(new object[] { folderId }, ct);
            if (folder == null) return NotFound(new { message = "Folder not found" });

            var sanitized = Path.GetFileName(file.FileName);
            var stored = _storage.GenerateUniqueFileName(sanitized);
            var relativePath = Path.Combine(folderId.ToString(), stored);

            // Generate data key and wrap
            var dataKey = _enc.GenerateDataKey();
            var wrapped = _enc.ProtectDataKey(dataKey);
            var chunkSize = _config.GetValue<int?>("Encryption:ChunkSizeBytes") ?? 1048576;

            // Ensure directory exists and write encrypted stream directly to file
            var fullPath = _storage.GetFullPathForRelative(relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

            try
            {
                using (var inStream = file.OpenReadStream())
                using (var fs = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    await _enc.EncryptStreamAsync(inStream, fs, dataKey, chunkSize, ct);
                    await fs.FlushAsync(ct);
                }

                var fileInfo = new FileInfo(fullPath);

                var rec = new FileRecord
                {
                    Id = Guid.NewGuid(),
                    FolderId = folderId,
                    OriginalFileName = sanitized,
                    StoredFileName = stored,
                    StoragePath = relativePath,
                    ContentType = file.ContentType ?? MediaTypeNames.Application.Octet,
                    FileExtension = Path.GetExtension(sanitized),
                    OriginalSizeBytes = file.Length,
                    EncryptedSizeBytes = fileInfo.Length,
                    EncryptionKeyId = Guid.NewGuid(),
                    UploadedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var keyRec = new EncryptionKey
                {
                    Id = rec.EncryptionKeyId,
                    KeyProvider = "DPAPI/fallback",
                    WrappedDataKey = wrapped,
                    ChunkSizeBytes = chunkSize,
                    CreatedAt = DateTime.UtcNow
                };

                _db.Files.Add(rec);
                _db.EncryptionKeys.Add(keyRec);
                await _db.SaveChangesAsync(ct);

                await _auditLog.LogAsync("FileUploaded", "file", rec.Id.ToString(), rec.FolderId, null, "success", $"Uploaded {rec.OriginalFileName}", HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers["User-Agent"].ToString(), Request.Headers["X-Correlation-Id"].ToString(), ct);

                return CreatedAtAction(nameof(Get), new { id = rec.Id }, new FileDto(rec.Id, rec.OriginalFileName, rec.ContentType, rec.OriginalSizeBytes, rec.UploadedAt, rec.FolderId));
            }
            catch (Exception ex)
            {
                // On failure, ensure no orphan file remains
                try { if (System.IO.File.Exists(fullPath)) System.IO.File.Delete(fullPath); } catch { }
                await _auditLog.LogAsync("FileUploadFailed", "file", null, folderId, null, "failure", "Upload failed", HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers["User-Agent"].ToString(), Request.Headers["X-Correlation-Id"].ToString(), ct);
                return StatusCode(500, new { message = "Upload failed" });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id, CancellationToken ct)
        {
            var file = await _db.Files.FindAsync(new object[] { id }, ct);
            if (file == null) return NotFound();
            return Ok(new FileDto(file.Id, file.OriginalFileName, file.ContentType, file.OriginalSizeBytes, file.UploadedAt, file.FolderId));
        }

        [HttpGet("{id}/download")]
        public async Task<IActionResult> Download(Guid id, CancellationToken ct)
        {
            var file = await _db.Files.FindAsync(new object[] { id }, ct);
            if (file == null) return NotFound();
            var key = await _db.EncryptionKeys.FindAsync(new object[] { file.EncryptionKeyId }, ct);
            if (key == null) return StatusCode(500);

            var dataKey = _enc.UnprotectDataKey(key.WrappedDataKey);
            var fullPath = _storage.GetFullPathForRelative(file.StoragePath);
            if (!System.IO.File.Exists(fullPath)) return NotFound();

            // Stream decrypt directly to response
            var readStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var pipe = new System.IO.Pipelines.Pipe();

            // We'll create a streaming response using PushStream
            Response.Headers["Cache-Control"] = "no-store";
            Response.ContentType = file.ContentType;
            Response.Headers["Content-Disposition"] = new Microsoft.Net.Http.Headers.ContentDispositionHeaderValue("attachment") { FileName = file.OriginalFileName }.ToString();

            try
            {
                // Use a custom stream where we decrypt on the fly and copy to response body
                await using var outputStream = Response.BodyWriter.AsStream();
                // Decrypt input readStream to outputStream
                await _enc.DecryptStreamAsync(readStream, outputStream, dataKey, key.ChunkSizeBytes == 0 ? 1048576 : key.ChunkSizeBytes, ct);
                await _auditLog.LogAsync("FileDownloaded", "file", file.Id.ToString(), file.FolderId, null, "success", $"Downloaded {file.OriginalFileName}", HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers["User-Agent"].ToString(), Request.Headers["X-Correlation-Id"].ToString(), ct);
                return new EmptyResult(); // response already written
            }
            catch (InvalidDataException ex)
            {
                // Authentication failed during decrypt
                return StatusCode(500, new { message = "Decryption failed" });
            }
            finally
            {
                readStream.Dispose();
            }
        }

        [HttpGet("{id}/preview")]
        public async Task<IActionResult> Preview(Guid id, CancellationToken ct)
        {
            var file = await _db.Files.FindAsync(new object[] { id }, ct);
            if (file == null) return NotFound();
            var allowed = new[] { "image/jpeg", "image/png", "image/gif", "image/webp", "application/pdf", "video/mp4", "video/webm" };
            if (!allowed.Contains(file.ContentType)) return BadRequest(new { message = "Preview not available for this file type" });

            var key = await _db.EncryptionKeys.FindAsync(new object[] { file.EncryptionKeyId }, ct);
            if (key == null) return StatusCode(500);
            var dataKey = _enc.UnprotectDataKey(key.WrappedDataKey);

            var fullPath = _storage.GetFullPathForRelative(file.StoragePath);
            if (!System.IO.File.Exists(fullPath)) return NotFound();

            Response.Headers["X-Content-Type-Options"] = "nosniff";
            Response.Headers["Cache-Control"] = "no-store";
            Response.ContentType = file.ContentType;

            try
            {
                await using var readStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                await using var outputStream = Response.BodyWriter.AsStream();
                await _enc.DecryptStreamAsync(readStream, outputStream, dataKey, key.ChunkSizeBytes == 0 ? 1048576 : key.ChunkSizeBytes, ct);
                await _auditLog.LogAsync("FilePreviewed", "file", file.Id.ToString(), file.FolderId, null, "success", $"Previewed {file.OriginalFileName}", HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers["User-Agent"].ToString(), Request.Headers["X-Correlation-Id"].ToString(), ct);
                return new EmptyResult();
            }
            catch (InvalidDataException ex)
            {
                return StatusCode(500, new { message = "Decryption failed" });
            }
        }
    }
}
