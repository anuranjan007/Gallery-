using System.Security.Cryptography;

namespace SecureFileManager.Infrastructure.Services
{
    public class FileSystemStorageService
    {
        private readonly IConfiguration _configuration;
        private readonly string _root;

        public FileSystemStorageService(IConfiguration configuration)
        {
            _configuration = configuration;
            _root = _configuration.GetValue<string>("Storage:RootPath") ?? "D:\\AppStorage";
        }

        public string GetFullPathForRelative(string relativePath)
        {
            // Prevent rooted paths, UNC paths
            if (Path.IsPathRooted(relativePath)) throw new ArgumentException("Relative path expected");
            // Normalize
            var combined = Path.GetFullPath(Path.Combine(_root, relativePath));
            var rootFull = Path.GetFullPath(_root);
            if (!combined.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException("Attempt to escape storage root");
            }
            return combined;
        }

        public string GenerateUniqueFileName(string originalFileName)
        {
            var ext = Path.GetExtension(originalFileName);
            return $"{Guid.NewGuid():N}{ext}.enc";
        }

        public async Task WriteStreamAsync(Stream cipherStream, string relativePath, CancellationToken cancellationToken)
        {
            var full = GetFullPathForRelative(relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            using var fs = new FileStream(full, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            await cipherStream.CopyToAsync(fs, cancellationToken);
        }

        public Stream OpenRead(string relativePath)
        {
            var full = GetFullPathForRelative(relativePath);
            return new FileStream(full, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        public void Delete(string relativePath)
        {
            var full = GetFullPathForRelative(relativePath);
            if (File.Exists(full)) File.Delete(full);
        }
    }
}
