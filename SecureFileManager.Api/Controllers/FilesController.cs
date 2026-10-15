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
    [Route("api/[controller]")]
    public class FilesController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly FileSystemStorageService _storage;
        private readonly EncryptionService _enc;
        private readonly IConfiguration _config;

        public FilesController(AppDbContext db, FileSystemStorageService storage, EncryptionService enc, IConfiguration config)
        {
            _db = db;
            _storage = storage;
            _enc = enc;
            _config = config;
        }

        [HttpPost("upload")]
        [RequestSizeLimit(long.MaxValue)]
        public async Task<IActionResult> Upload([FromForm] IFormFile file, [FromForm] Guid folderId, CancellationToken ct)
        {
            if (file == null) return BadRequest(new { message = "No file" });
            var folder = await _db.Folders.FindAsync(new object[] { folderId }, ct);
            if (folder == null) return NotFound(new { message = "Folder not found" });

            // Validate filename and content type minimally
            var sanitized = Path.GetFileName(file.FileName);
            var stored = _storage.GenerateUniqueFileName(sanitized);
            var relativePath = Path.Combine(folderId.ToString(), stored);

            // Encrypt stream
            var dataKey = _enc.GenerateDataKey();
            var wrapped = _enc.ProtectDataKey(dataKey);

            // Prepare encryption format header: magic + version + chunkSize
            var chunkSize = _config.GetValue<int?>("Encryption:ChunkSizeBytes") ?? 1048576;
            using var input = file.OpenReadStream();
            // We'll write to temp memory stream of encrypted data then write to storage
            using var cipherStream = new MemoryStream();
            await _enc.EncryptStreamAsync(input, cipherStream, dataKey, chunkSize, ct);
            cipherStream.Seek(0, SeekOrigin.Begin);

            // Persist encrypted file
            await _storage.WriteStreamAsync(cipherStream, relativePath, ct);

            // Save metadata
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
                EncryptedSizeBytes = cipherStream.Length,
                EncryptionKeyId = Guid.NewGuid(),
                UploadedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Create encryption key record
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

            return CreatedAtAction(nameof(Get), new { id = rec.Id }, new FileDto(rec.Id, rec.OriginalFileName, rec.ContentType, rec.OriginalSizeBytes, rec.UploadedAt, rec.FolderId));
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

            var wrapped = key.WrappedDataKey;
            var dataKey = _enc.UnprotectDataKey(wrapped);

            var stream = _storage.OpenRead(file.StoragePath);
            var plaintext = new MemoryStream();
            await _enc.DecryptStreamAsync(stream, plaintext, dataKey, key.ChunkSizeBytes == 0 ? 1048576 : key.ChunkSizeBytes, ct);
            plaintext.Seek(0, SeekOrigin.Begin);

            return File(plaintext, file.ContentType, file.OriginalFileName, enableRangeProcessing: true);
        }

        [HttpGet("{id}/preview")]
        public async Task<IActionResult> Preview(Guid id, CancellationToken ct)
        {
            var file = await _db.Files.FindAsync(new object[] { id }, ct);
            if (file == null) return NotFound();
            // Only allow certain content types
            var allowed = new[] { "image/jpeg", "image/png", "image/gif", "image/webp", "application/pdf", "video/mp4", "video/webm" };
            if (!allowed.Contains(file.ContentType)) return BadRequest(new { message = "Preview not available for this file type" });

            var key = await _db.EncryptionKeys.FindAsync(new object[] { file.EncryptionKeyId }, ct);
            if (key == null) return StatusCode(500);
            var dataKey = _enc.UnprotectDataKey(key.WrappedDataKey);

            var stream = _storage.OpenRead(file.StoragePath);
            var plaintext = new MemoryStream();
            await _enc.DecryptStreamAsync(stream, plaintext, dataKey, key.ChunkSizeBytes == 0 ? 1048576 : key.ChunkSizeBytes, ct);
            plaintext.Seek(0, SeekOrigin.Begin);

            Response.Headers["X-Content-Type-Options"] = "nosniff";
            Response.Headers["Cache-Control"] = "no-store";
            return File(plaintext, file.ContentType, file.OriginalFileName);
        }
    }
}
