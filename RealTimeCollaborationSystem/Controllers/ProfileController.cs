using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RealTimeCollaborationSystem.Data;
using RealTimeCollaborationSystem.Filters;
using RealTimeCollaborationSystem.Models;
using RealTimeCollaborationSystem.Services;

namespace RealTimeCollaborationSystem.Controllers
{
    [RequireUserSession]
    public class ProfileController : Controller
    {
        private const string DefaultAvatarUrl = "/images/users/default-avatar.svg";
        private static readonly string[] AllowedPhotoExtensions = [".jpg", ".jpeg", ".png", ".gif", ".webp"];
        private readonly AppDbContext _db;
        private readonly UploadStorageService _uploadStorage;

        public ProfileController(AppDbContext db, UploadStorageService uploadStorage)
        {
            _db = db;
            _uploadStorage = uploadStorage;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return RedirectToAction("Index", "Settings");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePhoto(IFormFile? photoFile)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId.Value);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var result = await ApplyProfilePhotoChangeAsync(user, photoFile, null);
            TempData[result.Success ? "ProfileSaved" : "ProfileError"] = result.Message;

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSidebarPhoto(IFormFile? photoFile, string? croppedPhotoData)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new { message = "Your session has expired. Refresh the page and try again." });
            }

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId.Value);
            if (user == null)
            {
                return Unauthorized(new { message = "Your session has expired. Refresh the page and try again." });
            }

            var result = await ApplyProfilePhotoChangeAsync(user, photoFile, croppedPhotoData);
            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }

            return Json(new
            {
                message = result.Message,
                photoUrl = AddCacheBuster(result.PhotoUrl!)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPhoto()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId.Value);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            DeleteCustomPhotoFile(user.ProfilePhotoUrl);

            user.ProfilePhotoUrl = null;
            if (!string.IsNullOrWhiteSpace(user.PhotoUrl)
                && !string.Equals(user.PhotoUrl, "/images/users/default.png", StringComparison.OrdinalIgnoreCase))
            {
                user.PhotoUrl = null;
            }

            await _db.SaveChangesAsync();

            UpdatePhotoSession(user);
            TempData["ProfileSaved"] = "Profile photo reset to the default image.";

            return RedirectToAction(nameof(Index));
        }

        private async Task<User?> GetCurrentUserAsync()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return null;
            }

            return await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId.Value);
        }

        private int? GetCurrentUserId()
        {
            var userIdRaw = HttpContext.Session.GetString("UserId");
            return int.TryParse(userIdRaw, out var userId) ? userId : null;
        }

        private static string? Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private async Task<PhotoUpdateResult> ApplyProfilePhotoChangeAsync(User user, IFormFile? photoFile, string? croppedPhotoData = null)
        {
            var normalizedCroppedPhotoData = Normalize(croppedPhotoData);

            if (!string.IsNullOrWhiteSpace(normalizedCroppedPhotoData))
            {
                try
                {
                    user.ProfilePhotoUrl = await SaveCroppedPhotoDataAsync(user, normalizedCroppedPhotoData);
                }
                catch
                {
                    return PhotoUpdateResult.Failed("Unable to process the selected image. Try another photo.");
                }

                await _db.SaveChangesAsync();
                UpdatePhotoSession(user);

                return PhotoUpdateResult.Saved(ResolveProfilePhotoUrl(user), "Profile photo updated successfully.");
            }

            if (photoFile != null && photoFile.Length > 0)
            {
                if (photoFile.Length > 5 * 1024 * 1024)
                {
                    return PhotoUpdateResult.Failed("Profile photo must be smaller than 5 MB.");
                }

                if (!photoFile.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                {
                    return PhotoUpdateResult.Failed("Only image files can be used as profile photos.");
                }

                var extension = Path.GetExtension(photoFile.FileName);
                if (string.IsNullOrWhiteSpace(extension)
                    || !AllowedPhotoExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
                {
                    return PhotoUpdateResult.Failed("Use a JPG, PNG, GIF, or WEBP image.");
                }

                var uploadsFolder = EnsureUploadsFolder();
                DeleteCustomPhotoFile(user.ProfilePhotoUrl);

                var fileName = $"profile-{user.Id}-{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                await using var stream = System.IO.File.Create(filePath);
                await photoFile.CopyToAsync(stream);

                user.ProfilePhotoUrl = $"/images/users/uploads/{fileName}";
            }
            else
            {
                return PhotoUpdateResult.Failed("Choose a file to upload.");
            }

            await _db.SaveChangesAsync();
            UpdatePhotoSession(user);

            return PhotoUpdateResult.Saved(ResolveProfilePhotoUrl(user), "Profile photo updated successfully.");
        }

        private static string ResolveProfilePhotoUrl(User user)
        {
            if (!string.IsNullOrWhiteSpace(user.ProfilePhotoUrl))
            {
                return user.ProfilePhotoUrl;
            }

            if (!string.IsNullOrWhiteSpace(user.PhotoUrl)
                && !string.Equals(user.PhotoUrl, "/images/users/default.png", StringComparison.OrdinalIgnoreCase))
            {
                return user.PhotoUrl;
            }

            return DefaultAvatarUrl;
        }

        private void UpdatePhotoSession(User user)
        {
            HttpContext.Session.SetString("PhotoUrl", ResolveProfilePhotoUrl(user));
        }

        private string EnsureUploadsFolder()
        {
            return _uploadStorage.GetProfilePhotosDirectory();
        }

        private async Task<string> SaveCroppedPhotoDataAsync(User user, string croppedPhotoData)
        {
            const string dataPrefix = "data:image/";
            if (!croppedPhotoData.StartsWith(dataPrefix, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Cropped photo data is invalid.");
            }

            var separatorIndex = croppedPhotoData.IndexOf(',', StringComparison.Ordinal);
            if (separatorIndex < 0 || separatorIndex == croppedPhotoData.Length - 1)
            {
                throw new InvalidOperationException("Cropped photo data is incomplete.");
            }

            var bytes = Convert.FromBase64String(croppedPhotoData[(separatorIndex + 1)..]);
            if (bytes.Length == 0)
            {
                throw new InvalidOperationException("Cropped photo data is empty.");
            }

            var uploadsFolder = EnsureUploadsFolder();
            DeleteCustomPhotoFile(user.ProfilePhotoUrl);

            var fileName = $"profile-{user.Id}-{Guid.NewGuid():N}.png";
            var filePath = Path.Combine(uploadsFolder, fileName);

            await System.IO.File.WriteAllBytesAsync(filePath, bytes);

            return $"/images/users/uploads/{fileName}";
        }

        private static string AddCacheBuster(string url)
        {
            var separator = url.Contains('?') ? "&" : "?";
            return $"{url}{separator}v={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
        }

        private void DeleteCustomPhotoFile(string? relativePath)
        {
            var absolutePath = _uploadStorage.TryGetProfilePhotoPhysicalPath(relativePath);

            if (absolutePath == null)
            {
                return;
            }

            if (System.IO.File.Exists(absolutePath))
            {
                System.IO.File.Delete(absolutePath);
            }
        }

        private sealed record PhotoUpdateResult(bool Success, string Message, string? PhotoUrl)
        {
            public static PhotoUpdateResult Failed(string message) => new(false, message, null);

            public static PhotoUpdateResult Saved(string photoUrl, string message) => new(true, message, photoUrl);
        }
    }
}
