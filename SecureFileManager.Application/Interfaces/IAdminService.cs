using SecureFileManager.Application.DTOs;

namespace SecureFileManager.Application.Interfaces
{
    public interface IAdminService
    {
        Task<AuthenticateResult> AuthenticateAsync(string username, string password, CancellationToken cancellationToken);
        Task<bool> ProvisionAdminAsync(string username, string password, CancellationToken cancellationToken);
    }
}
