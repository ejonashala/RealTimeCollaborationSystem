using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RealTimeCollaborationSystem.Data;
using RealTimeCollaborationSystem.Filters;
using RealTimeCollaborationSystem.Models;
using RealTimeCollaborationSystem.ViewModels;

namespace RealTimeCollaborationSystem.Controllers
{
    [RequireUserSession]
    public class SettingsController : Controller
    {
        private readonly AppDbContext _db;
        private readonly PasswordHasher<User> _passwordHasher = new();

        public SettingsController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            SetSharedViewData("Account", user);

            return View(new SettingsViewModel
            {
                Name = user.Name,
                Email = user.Email,
                Role = user.Role
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(SettingsViewModel model)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            model.Name = model.Name?.Trim() ?? string.Empty;
            model.Email = user.Email;
            model.Role = user.Role;

            ModelState.Remove(nameof(SettingsViewModel.Email));

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                ModelState.AddModelError(nameof(SettingsViewModel.Name), "Name is required.");
            }

            if (!ModelState.IsValid)
            {
                SetSharedViewData("Account", user);
                return View(model);
            }

            if (string.Equals(user.Name, model.Name, StringComparison.Ordinal))
            {
                TempData["Info"] = "No changes were made.";
                return RedirectToAction(nameof(Index));
            }

            user.Name = model.Name;

            await _db.SaveChangesAsync();

            HttpContext.Session.SetString("UserName", user.Name);
            TempData["Success"] = "Your account details were updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Preferences()
        {
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SetTheme(string theme)
        {
            var normalizedTheme = NormalizeTheme(theme);
            var currentTheme = HttpContext.Session.GetString("Theme") ?? "light";

            if (normalizedTheme == null)
            {
                TempData["Error"] = "Please choose a valid theme.";
                return RedirectToAction(nameof(Index));
            }

            if (string.Equals(currentTheme, normalizedTheme, StringComparison.OrdinalIgnoreCase))
            {
                TempData["Info"] = "No changes were made.";
                return RedirectToAction(nameof(Index));
            }

            HttpContext.Session.SetString("Theme", normalizedTheme);
            TempData["Success"] = $"Theme changed to {normalizedTheme}.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeLanguage(string language)
        {
            var normalizedLanguage = NormalizeLanguage(language);

            if (normalizedLanguage == null)
            {
                TempData["Error"] = "Please choose a valid language.";
                return RedirectToAction(nameof(Index));
            }

            var user = await GetCurrentUserAsync();

            if (user != null)
            {
                user.Language = normalizedLanguage;
                await _db.SaveChangesAsync();
            }

            HttpContext.Session.SetString("Language", normalizedLanguage);
            TempData["Success"] = $"Language changed to {normalizedLanguage}.";

            return RedirectToAction(nameof(Index));
        }

        private static string? NormalizeLanguage(string? language)
        {
            if (string.Equals(language, "sq", StringComparison.OrdinalIgnoreCase))
            {
                return "sq";
            }

            if (string.Equals(language, "en", StringComparison.OrdinalIgnoreCase))
            {
                return "en";
            }

            return null;
        }

        [HttpGet]
        public IActionResult Password()
        {
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Password(string CurrentPassword, string NewPassword, string ConfirmPassword)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            ValidatePasswordForm(CurrentPassword, NewPassword, ConfirmPassword);

            if (!ModelState.IsValid)
            {
                SetSharedViewData("Password", user);
                return View(nameof(Index), new SettingsViewModel
                {
                    Name = user.Name,
                    Email = user.Email,
                    Role = user.Role
                });
            }

            var result = _passwordHasher.VerifyHashedPassword(user, user.Password, CurrentPassword);

            if (result == PasswordVerificationResult.Failed)
            {
                TempData["Error"] = "The current password you entered is incorrect.";
                return RedirectToAction(nameof(Index));
            }

            user.Password = _passwordHasher.HashPassword(user, NewPassword);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Your password was updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMyAccount()
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

            await using var transaction = await _db.Database.BeginTransactionAsync();

            try
            {
                var receivedNotifications = await _db.SystemNotifications
                    .Where(notification => notification.RecipientUserId == user.Id)
                    .ToListAsync();

                var authoredNotifications = await _db.SystemNotifications
                    .Where(notification =>
                        notification.ActorUserId == user.Id
                        && notification.RecipientUserId != user.Id)
                    .ToListAsync();

                var studentSubmissions = await _db.TaskSubmissions
                    .Where(submission => submission.StudentId == user.Id)
                    .ToListAsync();
                var studentSubmissionIds = studentSubmissions
                    .Select(submission => submission.Id)
                    .ToList();

                var authoredTasks = await _db.CourseTasks
                    .Include(task => task.Submissions)
                    .Where(task => task.ProfessorId == user.Id)
                    .ToListAsync();
                var authoredTaskIds = authoredTasks
                    .Select(task => task.Id)
                    .ToList();
                var authoredSubmissionIds = authoredTasks
                    .SelectMany(task => task.Submissions.Select(submission => submission.Id))
                    .ToList();

                var authoredBatches = await _db.TopicBatches
                    .Include(batch => batch.Topics)
                    .Where(batch => batch.CreatedByProfessorId == user.Id)
                    .ToListAsync();
                var authoredTopicIds = authoredBatches
                    .SelectMany(batch => batch.Topics.Select(topic => topic.Id))
                    .ToList();

                var notificationsLinkedToDeletedData = await _db.SystemNotifications
                    .Where(notification =>
                        (notification.CourseTaskId.HasValue && authoredTaskIds.Contains(notification.CourseTaskId.Value))
                        || (notification.TaskSubmissionId.HasValue
                            && (studentSubmissionIds.Contains(notification.TaskSubmissionId.Value)
                                || authoredSubmissionIds.Contains(notification.TaskSubmissionId.Value)))
                        || (notification.TopicId.HasValue && authoredTopicIds.Contains(notification.TopicId.Value)))
                    .ToListAsync();

                foreach (var notification in notificationsLinkedToDeletedData)
                {
                    if (notification.CourseTaskId.HasValue && authoredTaskIds.Contains(notification.CourseTaskId.Value))
                    {
                        notification.CourseTaskId = null;
                    }

                    if (notification.TaskSubmissionId.HasValue
                        && (studentSubmissionIds.Contains(notification.TaskSubmissionId.Value)
                            || authoredSubmissionIds.Contains(notification.TaskSubmissionId.Value)))
                    {
                        notification.TaskSubmissionId = null;
                    }

                    if (notification.TopicId.HasValue && authoredTopicIds.Contains(notification.TopicId.Value))
                    {
                        notification.TopicId = null;
                    }
                }

                foreach (var notification in authoredNotifications)
                {
                    notification.ActorUserId = null;
                }

                var selectedTopics = await _db.Topics
                    .Where(topic => topic.ReservedByStudentId == user.Id)
                    .ToListAsync();

                foreach (var topic in selectedTopics)
                {
                    topic.ReservedByStudentId = null;
                    topic.ReservedAt = null;
                    topic.Status = "Available";
                }

                _db.SystemNotifications.RemoveRange(receivedNotifications);
                _db.TaskSubmissions.RemoveRange(studentSubmissions);
                _db.Users.Remove(user);

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync();
                TempData["Error"] = "Your account could not be deleted because it is linked to existing records. Please contact support or remove related data first.";
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                await transaction.RollbackAsync();
                TempData["Error"] = "Your account could not be deleted. Please try again or contact support.";
                return RedirectToAction(nameof(Index));
            }

            HttpContext.Session.Clear();

            return RedirectToAction("Login", "Account");
        }

        private async Task<User?> GetCurrentUserAsync()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return null;
            }

            return await _db.Users.FirstOrDefaultAsync(u => u.Id == userId.Value);
        }

        private int? GetCurrentUserId()
        {
            var userIdRaw = HttpContext.Session.GetString("UserId");

            return int.TryParse(userIdRaw, out var userId) ? userId : null;
        }

        private void SetSharedViewData(string activeTab, User user)
        {
            var profilePhotoUrl = ResolveProfilePhotoUrl(user);

            ViewData["ActiveTab"] = activeTab;
            ViewData["CurrentTheme"] = HttpContext.Session.GetString("Theme") ?? "light";
            ViewData["SettingsRole"] = user.Role;
            ViewData["PhotoName"] = user.Name;
            ViewData["PhotoUrl"] = profilePhotoUrl;
            ViewData["HasPhoto"] = !string.Equals(profilePhotoUrl, "/images/users/default-avatar.svg", StringComparison.OrdinalIgnoreCase);
        }

        private void ValidatePasswordForm(string currentPassword, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(currentPassword))
            {
                ModelState.AddModelError("CurrentPassword", "Current password is required.");
            }

            if (string.IsNullOrWhiteSpace(newPassword))
            {
                ModelState.AddModelError("NewPassword", "New password is required.");
            }
            else if (newPassword.Length < 6)
            {
                ModelState.AddModelError("NewPassword", "New password must be at least 6 characters long.");
            }

            if (string.IsNullOrWhiteSpace(confirmPassword))
            {
                ModelState.AddModelError("ConfirmPassword", "Please confirm your new password.");
            }
            else if (!string.Equals(newPassword, confirmPassword, StringComparison.Ordinal))
            {
                ModelState.AddModelError("ConfirmPassword", "Password confirmation does not match.");
            }
        }

        private static string? NormalizeTheme(string? theme)
        {
            if (string.Equals(theme, "light", StringComparison.OrdinalIgnoreCase))
            {
                return "light";
            }

            if (string.Equals(theme, "dark", StringComparison.OrdinalIgnoreCase))
            {
                return "dark";
            }

            return null;
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

            return "/images/users/default-avatar.svg";
        }
    }
}
