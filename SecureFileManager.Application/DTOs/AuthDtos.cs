namespace SecureFileManager.Application.DTOs
{
    public record LoginRequest(string Username, string Password);

    public record AuthenticateResult(bool Succeeded, string? Token, string? FailureReason = null);
}
