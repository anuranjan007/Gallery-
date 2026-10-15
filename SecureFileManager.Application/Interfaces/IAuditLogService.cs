using SecureFileManager.Domain.Entities;

namespace SecureFileManager.Application.Interfaces
{
    public interface IFolderService
    {
        Task<IEnumerable<Folder>> GetRootFoldersAsync(CancellationToken cancellationToken);
        Task<Folder?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
        Task<IEnumerable<Folder>> GetChildFoldersAsync(Guid parentFolderId, CancellationToken cancellationToken);
        Task<IEnumerable<FileRecord>> GetFilesInFolderAsync(Guid folderId, CancellationToken cancellationToken);
        Task<Folder> CreateAsync(string name, Guid? parentFolderId, CancellationToken cancellationToken);
        Task<Folder> RenameAsync(Guid id, string newName, CancellationToken cancellationToken);
        Task DeleteAsync(Guid id, bool recursive, CancellationToken cancellationToken);
    }
}
