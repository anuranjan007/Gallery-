using SecureFileManager.Application.DTOs;

namespace SecureFileManager.Maui.Services
{
    public interface IApiClient
    {
        Task<(bool Success, string? Message)> LoginAsync(string username, string password);
        Task<(bool Success, string? Message)> VerifyOtpAsync(string contact, string code);
        Task<IEnumerable<FolderDto>> GetRootFoldersAsync();
    }
}
