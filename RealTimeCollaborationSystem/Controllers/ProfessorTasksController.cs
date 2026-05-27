using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RealTimeCollaborationSystem.Data;
using RealTimeCollaborationSystem.Hubs;
using RealTimeCollaborationSystem.Models;
using RealTimeCollaborationSystem.Services;
using RealTimeCollaborationSystem.ViewModels;

namespace RealTimeCollaborationSystem.Controllers
{
    public class ProfessorTasksController : Controller
    {
        private static readonly HashSet<string> AllowedAttachmentExtensions = new(StringComparer.OrdinalIgnoreCase)
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
        private readonly IHubContext<CollaborationHub> _hubContext;

        public ProfessorTasksController(AppDbContext context, UploadStorageService uploadStorage, IHubContext<CollaborationHub> hubContext)
        {
            _context = context;
            _uploadStorage = uploadStorage;
            _hubContext = hubContext;
        }

        private bool IsProfessor()
        {
            return HttpContext.Session.GetString("UserRole") == "Professor";
        }

        private IActionResult? RedirectIfNotProfessor()
        {
            if (string.IsNullOrWhiteSpace(HttpContext.Session.GetString("UserId")))
                return RedirectToAction("Login", "Account");

            if (!IsProfessor())
            {
                TempData["DashboardError"] = "Only professors can manage tasks.";
                return RedirectToAction("Index", "Dashboard");
            }

            return null;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? search, string? status)
        {
            if (RedirectIfNotProfessor() is IActionResult redirect)
                return redirect;

            ViewBag.Search = search ?? string.Empty;
            ViewBag.Status = status ?? string.Empty;

            return View(await BuildIndexViewModelAsync(search, status));
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            if (RedirectIfNotProfessor() is IActionResult redirect)
                return redirect;

            if (!TryGetCurrentProfessorId(out var professorId))
                return RedirectToAction("Login", "Account");

            ViewBag.Groups = await GetProfessorGroupsAsync(professorId);
            return View(new CreateCourseTaskViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateCourseTaskViewModel newTask, IFormFile? attachment)
        {
            if (RedirectIfNotProfessor() is IActionResult redirect)
                return redirect;

            var userIdString = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrWhiteSpace(userIdString) || !int.TryParse(userIdString, out var professorId))
                return RedirectToAction("Login", "Account");

            var attachmentError = ValidateAttachment(attachment);

            if (!string.IsNullOrWhiteSpace(attachmentError))
                ModelState.AddModelError("Attachment", attachmentError);

            var selectedGroup = await _context.TopicBatches
                .AsNoTracking()
                .FirstOrDefaultAsync(group =>
                    group.Id == newTask.TopicBatchId
                    && group.CreatedByProfessorId == professorId);

            if (selectedGroup == null)
                ModelState.AddModelError(nameof(CreateCourseTaskViewModel.TopicBatchId), "Choose one of your groups.");

            if (!ModelState.IsValid)
            {
                ViewBag.Groups = await GetProfessorGroupsAsync(professorId);
                return View(newTask);
            }

            var task = new CourseTask
            {
                Title = newTask.Title.Trim(),
                Description = newTask.Description.Trim(),
                Deadline = newTask.Deadline!.Value,
                MeetingLink = string.IsNullOrWhiteSpace(newTask.MeetingLink) ? null : newTask.MeetingLink.Trim(),
                CreatedAt = DateTime.UtcNow,
                ProfessorId = professorId,
                TopicBatchId = selectedGroup!.Id
            };

            if (attachment != null && attachment.Length > 0)
            {
                var savedFile = await SaveAttachmentAsync(attachment);
                task.AttachmentPath = savedFile.Path;
                task.AttachmentFileName = savedFile.FileName;
            }

            _context.CourseTasks.Add(task);
            await _context.SaveChangesAsync();

            var studentIds = await GetStudentIdsForBatchAsync(selectedGroup.Id);

            foreach (var studentId in studentIds)
            {
                _context.SystemNotifications.Add(new SystemNotification
                {
                    RecipientUserId = studentId,
                    ActorUserId = professorId,
                    Type = "TaskAssigned",
                    Title = "Task assigned",
                    Message = $"{task.Title} has been assigned to {selectedGroup.Title}. Deadline: {task.Deadline:yyyy-MM-dd HH:mm}.",
                    CourseTaskId = task.Id,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
            await _hubContext.Clients.All.SendAsync("NotificationsUpdated");

            TempData["TaskSuccess"] = "Task created.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            if (RedirectIfNotProfessor() is IActionResult redirect)
                return redirect;

            if (!TryGetCurrentProfessorId(out var professorId))
                return RedirectToAction("Login", "Account");

            var task = await _context.CourseTasks
                .Include(item => item.Professor)
                .Include(item => item.TopicBatch)
                .Include(item => item.Submissions)
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == id && item.ProfessorId == professorId);

            if (task == null)
                return NotFound();

            ViewBag.CompletionState = await BuildTaskCompletionStateAsync(professorId, task);

            return View(task);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CreateCourseTaskViewModel taskUpdate)
        {
            if (RedirectIfNotProfessor() is IActionResult redirect)
                return redirect;

            if (!TryGetCurrentProfessorId(out var professorId))
                return RedirectToAction("Login", "Account");

            var task = await _context.CourseTasks
                .FirstOrDefaultAsync(item => item.Id == id && item.ProfessorId == professorId);

            if (task == null)
                return NotFound();

            if (!ModelState.IsValid)
            {
                TempData["TaskError"] = "Task title, description, group, and deadline are required.";
                return RedirectToAction(nameof(Index));
            }

            var selectedGroup = await _context.TopicBatches
                .AsNoTracking()
                .FirstOrDefaultAsync(group =>
                    group.Id == taskUpdate.TopicBatchId
                    && group.CreatedByProfessorId == professorId);

            if (selectedGroup == null)
            {
                TempData["TaskError"] = "Choose one of your groups.";
                return RedirectToAction(nameof(Index));
            }

            task.Title = taskUpdate.Title.Trim();
            task.Description = taskUpdate.Description.Trim();
            task.Deadline = taskUpdate.Deadline!.Value;
            task.MeetingLink = string.IsNullOrWhiteSpace(taskUpdate.MeetingLink) ? null : taskUpdate.MeetingLink.Trim();
            task.TopicBatchId = selectedGroup.Id;

            await _context.SaveChangesAsync();

            TempData["TaskSuccess"] = "Task updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (RedirectIfNotProfessor() is IActionResult redirect)
                return redirect;

            if (!TryGetCurrentProfessorId(out var professorId))
                return RedirectToAction("Login", "Account");

            var task = await _context.CourseTasks
                .Include(item => item.Submissions)
                .FirstOrDefaultAsync(item => item.Id == id && item.ProfessorId == professorId);

            if (task == null)
                return NotFound();

            var submissionIds = task.Submissions.Select(submission => submission.Id).ToList();
            var notifications = await _context.SystemNotifications
                .Where(item => item.CourseTaskId == task.Id
                    || (item.TaskSubmissionId.HasValue && submissionIds.Contains(item.TaskSubmissionId.Value)))
                .ToListAsync();

            foreach (var notification in notifications)
            {
                if (notification.CourseTaskId == task.Id)
                    notification.CourseTaskId = null;

                if (notification.TaskSubmissionId.HasValue && submissionIds.Contains(notification.TaskSubmissionId.Value))
                    notification.TaskSubmissionId = null;
            }

            foreach (var submission in task.Submissions)
            {
                DeleteFile(submission.FilePath, "submissions");
            }

            DeleteFile(task.AttachmentPath, "tasks");

            _context.CourseTasks.Remove(task);
            await _context.SaveChangesAsync();

            TempData["TaskSuccess"] = "Task deleted.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Submissions(int id)
        {
            if (RedirectIfNotProfessor() is IActionResult redirect)
                return redirect;

            if (!TryGetCurrentProfessorId(out var professorId))
                return RedirectToAction("Login", "Account");

            var task = await _context.CourseTasks
                .Include(item => item.Professor)
                .Include(item => item.TopicBatch)
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == id && item.ProfessorId == professorId);

            if (task == null)
                return NotFound();

            var submissions = await _context.TaskSubmissions
                .Include(item => item.Student)
                .Where(item => item.CourseTaskId == id)
                .OrderByDescending(item => item.SubmittedAt)
                .AsNoTracking()
                .ToListAsync();

            return View(new ProfessorTaskSubmissionsViewModel
            {
                Task = task,
                Submissions = submissions
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveFeedback(int id, string? feedback, decimal? grade)
        {
            if (RedirectIfNotProfessor() is IActionResult redirect)
                return redirect;

            if (!TryGetCurrentProfessorId(out var professorId))
                return RedirectToAction("Login", "Account");

            var submission = await _context.TaskSubmissions
                .Include(item => item.CourseTask)
                .Include(item => item.Student)
                .FirstOrDefaultAsync(item => item.Id == id);

            if (submission?.CourseTask == null || submission.CourseTask.ProfessorId != professorId)
                return NotFound();

            if (grade.HasValue && grade.Value < 0)
            {
                TempData["TaskFeedbackError"] = "Grade cannot be negative.";
                return RedirectToAction(nameof(Submissions), new { id = submission.CourseTaskId });
            }

            submission.Feedback = string.IsNullOrWhiteSpace(feedback) ? null : feedback.Trim();
            submission.Grade = grade;
            submission.Status = "Reviewed";
            submission.ReviewedAt = DateTime.UtcNow;

            _context.SystemNotifications.Add(new SystemNotification
            {
                RecipientUserId = submission.StudentId,
                ActorUserId = professorId,
                Type = "FeedbackGiven",
                Title = "Feedback received",
                Message = $"Feedback was added for {submission.CourseTask.Title}.",
                CourseTaskId = submission.CourseTaskId,
                TaskSubmissionId = submission.Id,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            await _hubContext.Clients.All.SendAsync("NotificationsUpdated");

            TempData["TaskFeedbackSuccess"] = "Feedback saved.";

            return RedirectToAction(nameof(Submissions), new { id = submission.CourseTaskId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFeedback(int id)
        {
            if (RedirectIfNotProfessor() is IActionResult redirect)
                return redirect;

            if (!TryGetCurrentProfessorId(out var professorId))
                return RedirectToAction("Login", "Account");

            var submission = await _context.TaskSubmissions
                .Include(item => item.CourseTask)
                .FirstOrDefaultAsync(item => item.Id == id);

            if (submission?.CourseTask == null || submission.CourseTask.ProfessorId != professorId)
                return NotFound();

            submission.Feedback = null;
            submission.Grade = null;
            submission.Status = "Submitted";
            submission.ReviewedAt = null;

            await _context.SaveChangesAsync();

            TempData["TaskFeedbackSuccess"] = "Feedback removed.";
            return RedirectToAction(nameof(Submissions), new { id = submission.CourseTaskId });
        }

        private async Task<ProfessorTasksIndexViewModel> BuildIndexViewModelAsync(string? search, string? status)
        {
            var query = _context.CourseTasks
                .Include(task => task.Professor)
                .Include(task => task.TopicBatch)
                .Include(task => task.Submissions)
                .AsQueryable();

            var groups = new List<TopicBatch>();

            if (TryGetCurrentProfessorId(out var professorId))
            {
                query = query.Where(task => task.ProfessorId == professorId);
                groups = await GetProfessorGroupsAsync(professorId);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var normalizedSearch = search.Trim();
                query = query.Where(task =>
                    task.Title.Contains(normalizedSearch)
                    || task.Description.Contains(normalizedSearch));
            }

            var tasks = await query
                .OrderBy(task => task.Deadline)
                .ThenByDescending(task => task.CreatedAt)
                .AsNoTracking()
                .ToListAsync();

            var completionStates = TryGetCurrentProfessorId(out professorId)
                ? await BuildTaskCompletionStatesAsync(professorId, tasks)
                : new Dictionary<int, ProfessorTaskCompletionState>();

            if (string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase))
            {
                tasks = tasks
                    .Where(task => !completionStates.GetValueOrDefault(task.Id)?.IsComplete ?? true)
                    .ToList();
            }
            else if (string.Equals(status, "Completed", StringComparison.OrdinalIgnoreCase))
            {
                tasks = tasks
                    .Where(task => completionStates.GetValueOrDefault(task.Id)?.IsComplete == true)
                    .ToList();
            }

            return new ProfessorTasksIndexViewModel
            {
                Tasks = tasks,
                Groups = groups,
                CompletionStates = completionStates
            };
        }

        private async Task<ProfessorTaskCompletionState> BuildTaskCompletionStateAsync(int professorId, CourseTask task)
        {
            var states = await BuildTaskCompletionStatesAsync(professorId, new[] { task });
            return states.TryGetValue(task.Id, out var state)
                ? state
                : new ProfessorTaskCompletionState { TotalSubmissions = task.Submissions.Count };
        }

        private async Task<Dictionary<int, ProfessorTaskCompletionState>> BuildTaskCompletionStatesAsync(int professorId, IReadOnlyCollection<CourseTask> tasks)
        {
            var batchIds = tasks
                .Where(task => task.TopicBatchId.HasValue)
                .Select(task => task.TopicBatchId!.Value)
                .Distinct()
                .ToList();

            var requiredStudentIdsByBatch = batchIds.ToDictionary(
                batchId => batchId,
                _ => new HashSet<int>());

            if (batchIds.Count > 0)
            {
                var selectedTopicMembers = await _context.Topics
                    .Where(topic =>
                        batchIds.Contains(topic.TopicBatchId)
                        && topic.TopicBatch != null
                        && topic.TopicBatch.CreatedByProfessorId == professorId
                        && topic.ReservedByStudentId.HasValue)
                    .Select(topic => new
                    {
                        topic.TopicBatchId,
                        StudentId = topic.ReservedByStudentId!.Value
                    })
                    .ToListAsync();

                var acceptedJoinRequestMembers = await _context.SystemNotifications
                    .Where(notification =>
                        notification.Type == "GroupJoinRequest"
                        && notification.InvitationStatus == "Accepted"
                        && notification.ActorUserId.HasValue
                        && notification.Topic != null
                        && batchIds.Contains(notification.Topic.TopicBatchId)
                        && notification.Topic.TopicBatch != null
                        && notification.Topic.TopicBatch.CreatedByProfessorId == professorId)
                    .Select(notification => new
                    {
                        notification.Topic!.TopicBatchId,
                        StudentId = notification.ActorUserId!.Value
                    })
                    .ToListAsync();

                var acceptedInvitationMembers = await _context.SystemNotifications
                    .Where(notification =>
                        notification.Type == "GroupInvitation"
                        && notification.InvitationStatus == "Accepted"
                        && notification.Topic != null
                        && batchIds.Contains(notification.Topic.TopicBatchId)
                        && notification.Topic.TopicBatch != null
                        && notification.Topic.TopicBatch.CreatedByProfessorId == professorId)
                    .Select(notification => new
                    {
                        notification.Topic!.TopicBatchId,
                        StudentId = notification.RecipientUserId
                    })
                    .ToListAsync();

                foreach (var membership in selectedTopicMembers
                    .Concat(acceptedJoinRequestMembers)
                    .Concat(acceptedInvitationMembers))
                {
                    if (requiredStudentIdsByBatch.TryGetValue(membership.TopicBatchId, out var studentIds))
                        studentIds.Add(membership.StudentId);
                }
            }

            return tasks.ToDictionary(
                task => task.Id,
                task =>
                {
                    var requiredStudentIds = task.TopicBatchId.HasValue
                        && requiredStudentIdsByBatch.TryGetValue(task.TopicBatchId.Value, out var batchMembers)
                            ? batchMembers
                            : new HashSet<int>();

                    var submittedStudentIds = task.Submissions
                        .Select(submission => submission.StudentId)
                        .Distinct()
                        .ToHashSet();

                    return new ProfessorTaskCompletionState
                    {
                        RequiredSubmissions = requiredStudentIds.Count,
                        SubmittedRequiredMembers = requiredStudentIds.Count(submittedStudentIds.Contains),
                        TotalSubmissions = task.Submissions.Count
                    };
                });
        }

        private async Task<List<TopicBatch>> GetProfessorGroupsAsync(int professorId)
        {
            return await _context.TopicBatches
                .Where(group => group.CreatedByProfessorId == professorId)
                .OrderByDescending(group => group.IsPublished)
                .ThenBy(group => group.Title)
                .AsNoTracking()
                .ToListAsync();
        }

        private async Task<List<int>> GetStudentIdsForBatchAsync(int batchId)
        {
            var selectedTopicMembers = await _context.Topics
                .Where(topic =>
                    topic.TopicBatchId == batchId
                    && topic.ReservedByStudentId.HasValue)
                .Select(topic => topic.ReservedByStudentId!.Value)
                .ToListAsync();

            var acceptedJoinRequestMembers = await _context.SystemNotifications
                .Where(notification =>
                    notification.Type == "GroupJoinRequest"
                    && notification.InvitationStatus == "Accepted"
                    && notification.ActorUserId.HasValue
                    && notification.Topic != null
                    && notification.Topic.TopicBatchId == batchId)
                .Select(notification => notification.ActorUserId!.Value)
                .ToListAsync();

            var acceptedInvitationMembers = await _context.SystemNotifications
                .Where(notification =>
                    notification.Type == "GroupInvitation"
                    && notification.InvitationStatus == "Accepted"
                    && notification.Topic != null
                    && notification.Topic.TopicBatchId == batchId)
                .Select(notification => notification.RecipientUserId)
                .ToListAsync();

            return selectedTopicMembers
                .Concat(acceptedJoinRequestMembers)
                .Concat(acceptedInvitationMembers)
                .Distinct()
                .ToList();
        }

        private static string? ValidateAttachment(IFormFile? attachment)
        {
            if (attachment == null || attachment.Length == 0)
                return null;

            var extension = Path.GetExtension(attachment.FileName);

            return AllowedAttachmentExtensions.Contains(extension)
                ? null
                : "Supported files are PDF, DOCX, ZIP, PNG, JPG, or JPEG.";
        }

        private bool TryGetCurrentProfessorId(out int professorId)
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            return int.TryParse(userIdString, out professorId);
        }

        private async Task<(string Path, string FileName)> SaveAttachmentAsync(IFormFile attachment)
        {
            var uploadsFolder = _uploadStorage.GetUploadsDirectory("tasks");

            var originalFileName = Path.GetFileName(attachment.FileName);
            var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
            var storedFileName = $"{Guid.NewGuid():N}{extension}";
            var filePath = Path.Combine(uploadsFolder, storedFileName);

            await using var stream = System.IO.File.Create(filePath);
            await attachment.CopyToAsync(stream);

            return ($"/uploads/tasks/{storedFileName}", originalFileName);
        }

        private void DeleteFile(string? relativePath, string folder)
        {
            var absolutePath = _uploadStorage.TryGetUploadsPhysicalPath(relativePath, folder);

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
