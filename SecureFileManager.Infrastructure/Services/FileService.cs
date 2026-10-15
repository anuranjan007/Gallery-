using SecureFileManager.Application.Interfaces;
using SecureFileManager.Infrastructure.Data;
using SecureFileManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace SecureFileManager.Infrastructure.Services
{
    public class FileService : IFileService
    {
        private readonly AppDbContext _db;
        public FileService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IEnumerable<Folder>> ListRootFoldersAsync(CancellationToken cancellationToken)
        {
            return await _db.Folders.Where(f => f.ParentFolderId == null).OrderBy(f => f.Name).ToListAsync(cancellationToken);
        }
    }
}
