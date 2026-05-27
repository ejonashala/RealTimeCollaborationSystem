using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RealTimeCollaborationSystem.Data;
using RealTimeCollaborationSystem.Models;
using RealTimeCollaborationSystem.Services;
using RealTimeCollaborationSystem.ViewModels;

namespace RealTimeCollaborationSystem.Controllers
{
    public class StudentTasksController : Controller
    {
        private static readonly HashSet<string> AllowedSubmissionExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf",
            ".docx",
            ".zip",
            ".png",
            ".jpg",
            ".jpeg"
        };

        private readonly AppDbContext _context;
        private readonly UploadStorageService _uploadStorage;

        public StudentTasksController(AppDbContext context, UploadStorageService uploadStorage)
        {
            _context = context;
            _uploadStorage = uploadStorage;
        }

        private bool IsStudent()
        {
            return HttpContext.Session.GetString("UserRole") == "Student";
        }

        private IActionResult? RedirectIfNotStudent()
        {
            if (string.IsNullOrWhiteSpace(HttpContext.Session.GetString("UserId")))
                return RedirectToAction("Login", "Account");

            if (!IsStudent())
            {
                TempData["DashboardError"] = "Only students can view assigned tasks.";
                return RedirectToAction("Index", "Dashboard");
            }

            return null;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (RedirectIfNotStudent() is IActionResult redirect)
                return redirect;

            if (!TryGetCurrentStudentId(out var studentId))
                return RedirectToAction("Login", "Account");

            var joinedBatchIds = await GetJoinedBatchIdsAsync(studentId);

            var tasks = await _context.CourseTasks
                .Include(task => task.Professor)
                .Include(task => task.TopicBatch)
                .Include(task => task.Submissions.Where(submission => submission.StudentId == studentId))
                .Where(task =>
                    task.TopicBatchId.HasValue
                    && joinedBatchIds.Contains(task.TopicBatchId.Value))
                .OrderBy(task => task.Deadline)
                .ThenByDescending(task => task.CreatedAt)
                .AsNoTracking()
                .ToListAsync();

            return View(tasks);
        }

        [HttpGet]
        public async Task<IActionResult> SubmittedTasks()
        {
            if (RedirectIfNotStudent() is IActionResult redirect)
                return redirect;

            if (!TryGetCurrentStudentId(out var studentId))
                return RedirectToAction("Login", "Account");

            var joinedBatchIds = await GetJoinedBatchIdsAsync(studentId);

            var tasks = await _context.CourseTasks
                .Include(task => task.Professor)
                .Include(task => task.TopicBatch)
                .Include(task => task.Submissions.Where(submission => submission.StudentId == studentId))
                .Where(task =>
                    task.TopicBatchId.HasValue
                    && joinedBatchIds.Contains(task.TopicBatchId.Value)
                    && task.Submissions.Any(submission => submission.StudentId == studentId))
                .OrderByDescending(task => task.Submissions
                    .Where(submission => submission.StudentId == studentId)
                    .Max(submission => submission.SubmittedAt))
                .AsNoTracking()
                .ToListAsync();

            return View(tasks);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            if (RedirectIfNotStudent() is IActionResult redirect)
                return redirect;

            if (!TryGetCurrentStudentId(out var studentId))
                return RedirectToAction("Login", "Account");

            var joinedBatchIds = await GetJoinedBatchIdsAsync(studentId);

            var task = await _context.CourseTasks
                .Include(item => item.Professor)
                .Include(item => item.TopicBatch)
                .AsNoTracking()
                .FirstOrDefaultAsync(item =>
                    item.Id == id
                    && item.TopicBatchId.HasValue
                    && joinedBatchIds.Contains(item.TopicBatchId.Value));

            if (task == null)
                return NotFound();

            var submission = await _context.TaskSubmissions
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.CourseTaskId == id && item.StudentId == studentId);

            return View(new StudentTaskDetailsViewModel
            {
                Task = task,
                Submission = submission
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(int taskId, IFormFile? submissionFile, string? studentComment)
        {
            if (RedirectIfNotStudent() is IActionResult redirect)
                return redirect;

            if (!TryGetCurrentStudentId(out var studentId))
                return RedirectToAction("Login", "Account");

            var joinedBatchIds = await GetJoinedBatchIdsAsync(studentId);

            var task = await _context.CourseTasks
                .Include(item => item.TopicBatch)
                .FirstOrDefaultAsync(item => item.Id == taskId);

            if (task == null)
                return NotFound();

            if (!task.TopicBatchId.HasValue || !joinedBatchIds.Contains(task.TopicBatchId.Value))
            {
                TempData["TaskSubmissionError"] = "This task is not assigned to one of your groups.";
                return RedirectToAction(nameof(Index));
            }

            if (task.Deadline < DateTime.UtcNow)
            {
                TempData["TaskSubmissionError"] = "The deadline has passed. Submissions are closed.";
                return RedirectToAction(nameof(Index));
            }

            var fileError = ValidateSubmissionFile(submissionFile);

            if (!string.IsNullOrWhiteSpace(fileError))
            {
                TempData["TaskSubmissionError"] = fileError;
                return RedirectToAction(nameof(Index));
            }

            var savedFile = await SaveSubmissionFileAsync(submissionFile!);
            var submission = await _context.TaskSubmissions
                .FirstOrDefaultAsync(item => item.CourseTaskId == taskId && item.StudentId == studentId);

            if (submission == null)
            {
                submission = new TaskSubmission
                {
                    CourseTaskId = taskId,
                    StudentId = studentId
                };

                _context.TaskSubmissions.Add(submission);
            }
            else
            {
                DeleteSubmissionFile(submission.FilePath);
            }

            submission.FilePath = savedFile.Path;
            submission.FileName = savedFile.FileName;
            submission.StudentComment = string.IsNullOrWhiteSpace(studentComment) ? null : studentComment.Trim();
            submission.SubmittedAt = DateTime.UtcNow;
            submission.Status = "Submitted";
            submission.Feedback = null;
            submission.Grade = null;
            submission.ReviewedAt = null;

            await _context.SaveChangesAsync();

            TempData["TaskSubmissionSuccess"] = "Task submitted successfully.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSubmission(int id)
        {
            if (RedirectIfNotStudent() is IActionResult redirect)
                return redirect;

            if (!TryGetCurrentStudentId(out var studentId))
                return RedirectToAction("Login", "Account");

            var submission = await _context.TaskSubmissions
                .Include(item => item.CourseTask)
                    .ThenInclude(task => task!.TopicBatch)
                .FirstOrDefaultAsync(item => item.Id == id && item.StudentId == studentId);

            if (submission?.CourseTask == null)
                return NotFound();

            var joinedBatchIds = await GetJoinedBatchIdsAsync(studentId);

            if (!submission.CourseTask.TopicBatchId.HasValue
                || !joinedBatchIds.Contains(submission.CourseTask.TopicBatchId.Value))
            {
                TempData["TaskSubmissionError"] = "This submission is no longer connected to one of your groups.";
                return RedirectToAction(nameof(Index));
            }

            if (submission.CourseTask.Deadline < DateTime.UtcNow)
            {
                TempData["TaskSubmissionError"] = "The deadline has passed. You cannot delete this submission.";
                return RedirectToAction(nameof(Index));
            }

            var taskId = submission.CourseTaskId;
            var notifications = await _context.SystemNotifications
                .Where(item => item.TaskSubmissionId == submission.Id)
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.TaskSubmissionId = null;
            }

            DeleteSubmissionFile(submission.FilePath);
            _context.TaskSubmissions.Remove(submission);

            await _context.SaveChangesAsync();

            TempData["TaskSubmissionSuccess"] = "Submission removed. You can upload a new version before the deadline.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<HashSet<int>> GetJoinedBatchIdsAsync(int studentId)
        {
            var selectedTopicBatchIds = await _context.Topics
                .Where(topic => topic.ReservedByStudentId == studentId)
                .Select(topic => topic.TopicBatchId)
                .ToListAsync();

            var acceptedJoinRequestBatchIds = await _context.SystemNotifications
                .Where(notification =>
                    notification.Type == "GroupJoinRequest"
                    && notification.InvitationStatus == "Accepted"
                    && notification.ActorUserId == studentId
                    && notification.Topic != null)
                .Select(notification => notification.Topic!.TopicBatchId)
                .ToListAsync();

            var acceptedInvitationBatchIds = await _context.SystemNotifications
                .Where(notification =>
                    notification.Type == "GroupInvitation"
                    && notification.InvitationStatus == "Accepted"
                    && notification.RecipientUserId == studentId
                    && notification.Topic != null)
                .Select(notification => notification.Topic!.TopicBatchId)
                .ToListAsync();

            return selectedTopicBatchIds
                .Concat(acceptedJoinRequestBatchIds)
                .Concat(acceptedInvitationBatchIds)
                .ToHashSet();
        }

        private bool TryGetCurrentStudentId(out int studentId)
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            return int.TryParse(userIdString, out studentId);
        }

        private static string? ValidateSubmissionFile(IFormFile? submissionFile)
        {
            if (submissionFile == null || submissionFile.Length == 0)
                return "Please upload a file before submitting.";

            var extension = Path.GetExtension(submissionFile.FileName);

            return AllowedSubmissionExtensions.Contains(extension)
                ? null
                : "Supported files are PDF, DOCX, ZIP, PNG, JPG, or JPEG.";
        }

        private async Task<(string Path, string FileName)> SaveSubmissionFileAsync(IFormFile submissionFile)
        {
            var uploadsFolder = _uploadStorage.GetUploadsDirectory("submissions");

            var originalFileName = Path.GetFileName(submissionFile.FileName);
            var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
            var storedFileName = $"{Guid.NewGuid():N}{extension}";
            var filePath = Path.Combine(uploadsFolder, storedFileName);

            await using var stream = System.IO.File.Create(filePath);
            await submissionFile.CopyToAsync(stream);

            return ($"/uploads/submissions/{storedFileName}", originalFileName);
        }

        private void DeleteSubmissionFile(string? relativePath)
        {
            var absolutePath = _uploadStorage.TryGetUploadsPhysicalPath(relativePath, "submissions");

            if (absolutePath == null)
            {
                return;
            }

            if (System.IO.File.Exists(absolutePath))
            {
                System.IO.File.Delete(absolutePath);
            }
        }
    }
}
