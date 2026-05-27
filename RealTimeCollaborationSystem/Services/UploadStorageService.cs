using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace RealTimeCollaborationSystem.Services
{
    public sealed class UploadStorageService
    {
        private static readonly StringComparison PathComparison =
            OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        public UploadStorageService(IWebHostEnvironment environment, IConfiguration configuration)
        {
            WebRootPath = NormalizeDirectory(environment.WebRootPath
                ?? Path.Combine(environment.ContentRootPath, "wwwroot"));

            var configuredRootPath = configuration["FileStorage:RootPath"];
            RootPath = string.IsNullOrWhiteSpace(configuredRootPath)
                ? WebRootPath
                : NormalizeDirectory(ResolveConfiguredPath(configuredRootPath, environment.ContentRootPath));

            UploadsRootPath = CombineInside(RootPath, "uploads");
            ProfilePhotosRootPath = CombineInside(RootPath, "images", "users", "uploads");
        }

        public string RootPath { get; }

        public string WebRootPath { get; }

        public string UploadsRootPath { get; }

        public string ProfilePhotosRootPath { get; }

        public bool UsesWebRootStorage => string.Equals(RootPath, WebRootPath, PathComparison);

        public void EnsureStorageDirectories()
        {
            Directory.CreateDirectory(WebRootPath);
            Directory.CreateDirectory(UploadsRootPath);
            Directory.CreateDirectory(ProfilePhotosRootPath);
        }

        public string GetUploadsDirectory(params string[] segments)
        {
            var directory = CombineInside(UploadsRootPath, segments);
            Directory.CreateDirectory(directory);
            return directory;
        }

        public string GetUploadsPath(params string[] segments)
        {
            return CombineInside(UploadsRootPath, segments);
        }

        public string GetProfilePhotosDirectory()
        {
            Directory.CreateDirectory(ProfilePhotosRootPath);
            return ProfilePhotosRootPath;
        }

        public string? TryGetUploadsPhysicalPath(string? appRelativePath, string expectedFolder)
        {
            var pathOnly = GetPathWithoutQuery(appRelativePath);
            if (string.IsNullOrWhiteSpace(pathOnly))
                return null;

            var normalizedFolder = expectedFolder.Replace('\\', '/').Trim('/');
            var prefix = $"/uploads/{normalizedFolder}/";

            if (!pathOnly.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return null;

            var folderPath = CombineInside(UploadsRootPath, normalizedFolder.Split('/'));
            var fileSegment = pathOnly[prefix.Length..].Replace('/', Path.DirectorySeparatorChar);

            return TryCombineInside(folderPath, fileSegment);
        }

        public string? TryGetProfilePhotoPhysicalPath(string? appRelativePath)
        {
            const string prefix = "/images/users/uploads/";

            var pathOnly = GetPathWithoutQuery(appRelativePath);
            if (string.IsNullOrWhiteSpace(pathOnly)
                || !pathOnly.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var fileSegment = pathOnly[prefix.Length..].Replace('/', Path.DirectorySeparatorChar);

            return TryCombineInside(ProfilePhotosRootPath, fileSegment);
        }

        private static string ResolveConfiguredPath(string configuredPath, string contentRootPath)
        {
            var expandedPath = Environment.ExpandEnvironmentVariables(configuredPath.Trim());

            return Path.IsPathRooted(expandedPath)
                ? expandedPath
                : Path.Combine(contentRootPath, expandedPath);
        }

        private static string? GetPathWithoutQuery(string? appRelativePath)
        {
            if (string.IsNullOrWhiteSpace(appRelativePath))
                return null;

            var pathOnly = appRelativePath.Trim();
            var queryStart = pathOnly.IndexOfAny(['?', '#']);

            return queryStart >= 0 ? pathOnly[..queryStart] : pathOnly;
        }

        private static string CombineInside(string rootPath, params string[] segments)
        {
            var parts = new string[segments.Length + 1];
            parts[0] = rootPath;
            Array.Copy(segments, 0, parts, 1, segments.Length);

            var combinedPath = Path.Combine(parts);
            var fullPath = Path.GetFullPath(combinedPath);

            if (!IsInside(rootPath, fullPath))
                throw new InvalidOperationException("Configured upload storage path resolves outside the allowed root.");

            return fullPath;
        }

        private static string? TryCombineInside(string rootPath, params string[] segments)
        {
            try
            {
                return CombineInside(rootPath, segments);
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }

        private static string NormalizeDirectory(string path)
        {
            return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        private static bool IsInside(string rootPath, string candidatePath)
        {
            var normalizedRoot = NormalizeDirectory(rootPath);
            var normalizedCandidate = Path.GetFullPath(candidatePath);

            return string.Equals(normalizedRoot, normalizedCandidate, PathComparison)
                || normalizedCandidate.StartsWith(normalizedRoot + Path.DirectorySeparatorChar, PathComparison)
                || normalizedCandidate.StartsWith(normalizedRoot + Path.AltDirectorySeparatorChar, PathComparison);
        }
    }
}
