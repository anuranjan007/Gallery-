using System.Net.Http;
using System.Text.Json;
using System.Text;
using SecureFileManager.Application.DTOs;
using Microsoft.Maui.Storage;
using System.Net.Http.Headers;

namespace SecureFileManager.Maui.Services
{
    public class HttpApiClient : IApiClient
    {
        private readonly HttpClient _http;
        private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

        public HttpApiClient(HttpClient http)
        {
            _http = http;
        }

        public async Task<(bool Success, string? Message)> LoginAsync(string username, string password)
        {
            var req = new LoginRequest(username, password);
            var content = new StringContent(JsonSerializer.Serialize(req), Encoding.UTF8, "application/json");
            var res = await _http.PostAsync("api/auth/login", content);
            if (!res.IsSuccessStatusCode)
            {
                return (false, "Invalid credentials");
            }
            using var stream = await res.Content.ReadAsStreamAsync();
            var obj = await JsonSerializer.DeserializeAsync<JsonElement>(stream, _jsonOptions);
            if (obj.TryGetProperty("token", out var tokenElem))
            {
                var token = tokenElem.GetString();
                if (!string.IsNullOrEmpty(token))
                {
                    await SecureStorage.Default.SetAsync("auth_token", token);
                    _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    return (true, null);
                }
            }
            return (false, "Invalid response from server");
        }

        public async Task<(bool Success, string? Message)> VerifyOtpAsync(string contact, string code)
        {
            var payload = new { contact = contact, code = code };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var res = await _http.PostAsync("api/auth/verify-otp", content);
            if (!res.IsSuccessStatusCode)
                return (false, "OTP verification failed");
            using var stream = await res.Content.ReadAsStreamAsync();
            var obj = await JsonSerializer.DeserializeAsync<JsonElement>(stream, _jsonOptions);
            if (obj.TryGetProperty("token", out var tokenElem))
            {
                var token = tokenElem.GetString();
                if (!string.IsNullOrEmpty(token))
                {
                    await SecureStorage.Default.SetAsync("auth_token", token);
                    _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    return (true, null);
                }
            }
            return (false, "Invalid response from server");
        }

        public async Task<IEnumerable<FolderDto>> GetRootFoldersAsync()
        {
            var res = await _http.GetAsync("api/folders");
            res.EnsureSuccessStatusCode();
            var stream = await res.Content.ReadAsStreamAsync();
            var list = await JsonSerializer.DeserializeAsync<List<FolderDto>>(stream, _jsonOptions) ?? new List<FolderDto>();
            return list;
        }
    }
}
