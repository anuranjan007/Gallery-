using SecureFileManager.Application.DTOs;

namespace SecureFileManager.Maui.Services
{
    // Mock implementation returns static data; later the HTTP-based implementation will be added.
    public class MockApiClient : IApiClient
    {
        public Task<(bool Success, string? Message)> LoginAsync(string username, string password)
        {
            if ((username == "admin" && password == "password") || username.Contains("@"))
                return Task.FromResult((true, (string?)null));
            return Task.FromResult((false, "Invalid credentials"));
        }

        public Task<(bool Success, string? Message)> VerifyOtpAsync(string contact, string code)
        {
            if (code == "123456") return Task.FromResult((true, (string?)null));
            return Task.FromResult((false, "Invalid OTP"));
        }

        public Task<IEnumerable<FolderDto>> GetRootFoldersAsync()
        {
            var list = new List<FolderDto>
            {
                new FolderDto(Guid.NewGuid(), "Documents", null, DateTime.UtcNow.AddDays(-10), DateTime.UtcNow),
                new FolderDto(Guid.NewGuid(), "Personal", null, DateTime.UtcNow.AddDays(-20), DateTime.UtcNow),
                new FolderDto(Guid.NewGuid(), "Projects", null, DateTime.UtcNow.AddDays(-2), DateTime.UtcNow),
            };
            return Task.FromResult((IEnumerable<FolderDto>)list);
        }
    }
}
