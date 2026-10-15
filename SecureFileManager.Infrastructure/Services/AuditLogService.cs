using Microsoft.EntityFrameworkCore;
using SecureFileManager.Application.Interfaces;
using SecureFileManager.Domain.Entities;
using SecureFileManager.Infrastructure.Data;

namespace SecureFileManager.Infrastructure.Services
{
    public class FolderService : IFolderService
    {
        private readonly AppDbContext _db;

        public FolderService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IEnumerable<Folder>> GetRootFoldersAsync(CancellationToken cancellationToken)
        {
            return await _db.Folders
                .Where(f => f.ParentFolderId == null)
                .OrderBy(f => f.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<Folder?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return await _db.Folders.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
        }

        public async Task<IEnumerable<Folder>> GetChildFoldersAsync(Guid parentFolderId, CancellationToken cancellationToken)
        {
            return await _db.Folders
                .Where(f => f.ParentFolderId == parentFolderId)
                .OrderBy(f => f.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<FileRecord>> GetFilesInFolderAsync(Guid folderId, CancellationToken cancellationToken)
        {
            return await _db.Files
                .Where(f => f.FolderId == folderId)
                .OrderBy(f => f.OriginalFileName)
                .ToListAsync(cancellationToken);
        }

        public async Task<Folder> CreateAsync(string name, Guid? parentFolderId, CancellationToken cancellationToken)
        {
            var safeName = ValidateAndNormalizeName(name);

            if (parentFolderId.HasValue)
            {
                var parent = await _db.Folders.FirstOrDefaultAsync(f => f.Id == parentFolderId.Value, cancellationToken);
                if (parent == null) throw new InvalidOperationException("Parent folder not found.");
            }

            var existing = await _db.Folders.AnyAsync(f => f.ParentFolderId == parentFolderId && f.Name == safeName, cancellationToken);
            if (existing)
                throw new InvalidOperationException("A folder with that name already exists in the same parent folder.");

            var folder = new Folder
            {
                Id = Guid.NewGuid(),
                Name = safeName,
                ParentFolderId = parentFolderId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Folders.Add(folder);
            await _db.SaveChangesAsync(cancellationToken);
            return folder;
        }

        public async Task<Folder> RenameAsync(Guid id, string newName, CancellationToken cancellationToken)
        {
            var folder = await _db.Folders.FirstOrDefaultAsync(f => f.Id == id, cancellationToken)
                ?? throw new InvalidOperationException("Folder not found.");

            var safeName = ValidateAndNormalizeName(newName);
            if (folder.Name == safeName)
                return folder;

            var siblingExists = await _db.Folders.AnyAsync(f => f.ParentFolderId == folder.ParentFolderId && f.Id != folder.Id && f.Name == safeName, cancellationToken);
            if (siblingExists)
                throw new InvalidOperationException("A sibling folder with that name already exists.");

            folder.Name = safeName;
            folder.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return folder;
        }

        public async Task DeleteAsync(Guid id, bool recursive, CancellationToken cancellationToken)
        {
            var folder = await _db.Folders.FirstOrDefaultAsync(f => f.Id == id, cancellationToken)
                ?? throw new InvalidOperationException("Folder not found.");

            var childFolders = await _db.Folders.Where(f => f.ParentFolderId == id).ToListAsync(cancellationToken);
            if (childFolders.Count > 0 && !recursive)
                throw new InvalidOperationException("Folder contains children. Use recursive deletion to remove it.");

            if (recursive)
            {
                foreach (var child in childFolders)
                {
                    await DeleteAsync(child.Id, true, cancellationToken);
                }
            }

            var files = await _db.Files.Where(f => f.FolderId == id).ToListAsync(cancellationToken);
            _db.Files.RemoveRange(files);
            _db.Folders.Remove(folder);
            await _db.SaveChangesAsync(cancellationToken);
        }

        private static string ValidateAndNormalizeName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new InvalidOperationException("Folder name cannot be empty.");

            var trimmed = name.Trim();
            if (trimmed.Length > 255)
                throw new InvalidOperationException("Folder name is too long.");

            if (trimmed.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                throw new InvalidOperationException("Folder name contains invalid characters.");

            return trimmed;
        }
    }
}
