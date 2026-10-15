using SecureFileManager.Application.DTOs;
using SecureFileManager.Domain.Entities;

namespace SecureFileManager.Application.Interfaces
{
    public interface IFileService
    {
        Task<IEnumerable<Folder>> ListRootFoldersAsync(CancellationToken cancellationToken);
    }
}
