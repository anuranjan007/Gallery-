using SecureFileManager.Application.DTOs;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using Microsoft.Maui.Storage;

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
            if (!res.IsSuccessStatusCode) return (false, "Invalid credentials");
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
            if (!res.IsSuccessStatusCode) return (false, "OTP verification failed");
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

        public async Task<IEnumerable<FileDto>> GetFilesInFolderAsync(Guid folderId)
        {
            var res = await _http.GetAsync($"api/files?folderId={folderId}");
            res.EnsureSuccessStatusCode();
            var stream = await res.Content.ReadAsStreamAsync();
            var list = await JsonSerializer.DeserializeAsync<List<FileDto>>(stream, _jsonOptions) ?? new List<FileDto>();
            return list;
        }

        public async Task<(bool Success, FileDto? File, string? Message)> UploadFileAsync(string filePath, Guid folderId, IProgress<double>? progress = null, CancellationToken ct = default)
        {
            if (!File.Exists(filePath)) return (false, null, "File not found");
            using var fs = File.OpenRead(filePath);
            var content = new MultipartFormDataContent();
            var streamContent = new ProgressableStreamContent(fs, 81920, (sent, total) =>
            {
                progress?.Report((double)sent / total);
            });
            streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            content.Add(streamContent, "file", Path.GetFileName(filePath));
            content.Add(new StringContent(folderId.ToString()), "folderId");
            var res = await _http.PostAsync("api/files/upload", content, ct);
            if (!res.IsSuccessStatusCode) return (false, null, "Upload failed");
            var stream = await res.Content.ReadAsStreamAsync(ct);
            var obj = await JsonSerializer.DeserializeAsync<FileDto>(stream, _jsonOptions, ct);
            return (true, obj, null);
        }

        public async Task<(bool Success, string? LocalPath, string? Message)> DownloadFileAsync(Guid fileId, string targetLocalPath, IProgress<double>? progress = null, CancellationToken ct = default)
        {
            var res = await _http.GetAsync($"api/files/{fileId}/download", HttpCompletionOption.ResponseHeadersRead, ct);
            if (!res.IsSuccessStatusCode) return (false, null, "Download failed");
            var total = res.Content.Headers.ContentLength ?? -1L;
            using var stream = await res.Content.ReadAsStreamAsync(ct);
            using var fs = File.Create(targetLocalPath);
            var buffer = new byte[81920];
            long read = 0;
            int bytes;
            while ((bytes = await stream.ReadAsync(buffer, ct)) > 0)
            {
                await fs.WriteAsync(buffer.AsMemory(0, bytes), ct);
                read += bytes;
                if (total > 0) progress?.Report((double)read / total);
            }
            return (true, targetLocalPath, null);
        }

        public async Task<IEnumerable<FileDto>> SearchFilesAsync(string query, Guid? folderId = null)
        {
            var q = System.Web.HttpUtility.UrlEncode(query);
            var extra = folderId.HasValue ? $"&folderId={folderId}" : string.Empty;
            var res = await _http.GetAsync($"api/files/search?q={q}{extra}");
            res.EnsureSuccessStatusCode();
            var stream = await res.Content.ReadAsStreamAsync();
            var list = await JsonSerializer.DeserializeAsync<List<FileDto>>(stream, _jsonOptions) ?? new List<FileDto>();
            return list;
        }

        public async Task<(bool Success, string? Message)> CreateFolderAsync(string name, Guid? parentFolderId)
        {
            var payload = new { name = name, parentFolderId = parentFolderId };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var res = await _http.PostAsync("api/folders", content);
            if (!res.IsSuccessStatusCode) return (false, "Create folder failed");
            return (true, null);
        }

        public async Task<(bool Success, string? Message)> RenameFolderAsync(Guid id, string newName)
        {
            var payload = new { name = newName };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var res = await _http.PutAsync($"api/folders/{id}", content);
            if (!res.IsSuccessStatusCode) return (false, "Rename failed");
            return (true, null);
        }

        public async Task<(bool Success, string? Message)> DeleteFolderAsync(Guid id, bool recursive)
        {
            var res = await _http.DeleteAsync($"api/folders/{id}?recursive={recursive}");
            if (!res.IsSuccessStatusCode) return (false, "Delete failed");
            return (true, null);
        }

        public async Task<(int Total, IEnumerable<object> Items)> GetAuditLogsAsync(int page = 1, int pageSize = 50)
        {
            var res = await _http.GetAsync($"api/audit-logs?page={page}&pageSize={pageSize}");
            res.EnsureSuccessStatusCode();
            var stream = await res.Content.ReadAsStreamAsync();
            var obj = await JsonSerializer.DeserializeAsync<JsonElement>(stream, _jsonOptions);
            var total = obj.GetProperty("total").GetInt32();
            var items = new List<object>();
            foreach (var it in obj.GetProperty("items" ).EnumerateArray()) items.Add(it);
            return (total, items);
        }
    }

    // Helper content for upload progress
    public class ProgressableStreamContent : HttpContent
    {
        private const int defaultBufferSize = 4096;
        private readonly Stream _content;
        private readonly int _bufferSize;
        private readonly Action<long, long> _progress;

        public ProgressableStreamContent(Stream content, int bufferSize, Action<long, long> progress)
        {
            _content = content;
            _bufferSize = bufferSize;
            _progress = progress;
        }

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            var buffer = new byte[_bufferSize];
            long size = _content.Length;
            long uploaded = 0;
            int length;
            while ((length = await _content.ReadAsync(buffer)) > 0)
            {
                await stream.WriteAsync(buffer.AsMemory(0, length));
                uploaded += length;
                _progress?.Invoke(uploaded, size);
            }
        }

        protected override bool TryComputeLength(out long length)
        {
            length = _content.Length;
            return true;
        }
    }
}
