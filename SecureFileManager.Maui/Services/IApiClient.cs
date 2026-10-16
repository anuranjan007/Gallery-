using SecureFileManager.Application.DTOs;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Maui.Storage;

namespace SecureFileManager.Maui.Services
{
    public interface IApiClient
    {
        Task<(bool Success, string? Message)> LoginAsync(string username, string password);
        Task<(bool Success, string? Message)> VerifyOtpAsync(string contact, string code);
        Task<IEnumerable<FolderDto>> GetRootFoldersAsync();
        Task<IEnumerable<FileDto>> GetFilesInFolderAsync(Guid folderId);
        Task<(bool Success, FileDto? File, string? Message)> UploadFileAsync(string filePath, Guid folderId, IProgress<double>? progress = null, CancellationToken ct = default);
        Task<(bool Success, string? LocalPath, string? Message)> DownloadFileAsync(Guid fileId, string targetLocalPath, IProgress<double>? progress = null, CancellationToken ct = default);
        Task<IEnumerable<FileDto>> SearchFilesAsync(string query, Guid? folderId = null);
        Task<(bool Success, string? Message)> CreateFolderAsync(string name, Guid? parentFolderId);
        Task<(bool Success, string? Message)> RenameFolderAsync(Guid id, string newName);
        Task<(bool Success, string? Message)> DeleteFolderAsync(Guid id, bool recursive);
        Task<(int Total, IEnumerable<object> Items)> GetAuditLogsAsync(int page = 1, int pageSize = 50);
    }
}
