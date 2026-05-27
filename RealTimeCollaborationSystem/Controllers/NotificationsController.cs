using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RealTimeCollaborationSystem.Data;
using RealTimeCollaborationSystem.Hubs;
using RealTimeCollaborationSystem.Models;
using RealTimeCollaborationSystem.ViewModels;

namespace RealTimeCollaborationSystem.Controllers
{
    public class NotificationsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<CollaborationHub> _hubContext;

        public NotificationsController(AppDbContext context, IHubContext<CollaborationHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (!TryGetCurrentUser(out var userId, out var role))
                return RedirectToAction("Login", "Account");

            var notifications = await _context.SystemNotifications
                .Include(item => item.ActorUser)
                .Include(item => item.Topic)
                    .ThenInclude(topic => topic!.TopicBatch)
                .Include(item => item.CourseTask)
                .Where(item => item.RecipientUserId == userId)
                .OrderByDescending(item => item.CreatedAt)
                .AsNoTracking()
                .ToListAsync();

            var model = new NotificationsViewModel
            {
                IsProfessor = string.Equals(role, "Professor", StringComparison.OrdinalIgnoreCase),
                Notifications = notifications.Select(ToViewModel).ToList()
            };

            if (model.IsProfessor)
            {
                model.Students = await _context.Users
                    .Where(user => user.Role == "Student")
                    .OrderBy(user => user.Name)
                    .Select(user => new SelectListItem
                    {
                        Value = user.Id.ToString(),
                        Text = string.IsNullOrWhiteSpace(user.Name) ? user.Email : $"{user.Name} ({user.Email})"
                    })
                    .ToListAsync();

                model.Topics = await _context.Topics
                    .Include(topic => topic.TopicBatch)
                    .Where(topic =>
                        topic.TopicBatch != null
                        && topic.TopicBatch.CreatedByProfessorId == userId
                        && !topic.ReservedByStudentId.HasValue
                        && topic.Status != "Taken")
                    .OrderBy(topic => topic.TopicBatch!.Title)
                    .ThenBy(topic => topic.Title)
                    .Select(topic => new SelectListItem
                    {
                        Value = topic.Id.ToString(),
                        Text = $"{topic.TopicBatch!.Title} - {topic.Title}"
                    })
                    .ToListAsync();

                model.SentInvitations = await _context.SystemNotifications
                    .Include(item => item.RecipientUser)
                    .Include(item => item.Topic)
                        .ThenInclude(topic => topic!.TopicBatch)
                    .Where(item => item.ActorUserId == userId && item.Type == "GroupInvitation")
                    .OrderByDescending(item => item.CreatedAt)
                    .AsNoTracking()
                    .ToListAsync();
            }
            else
            {
                model.Notifications.AddRange(await BuildDeadlineNotificationsAsync(userId));
                model.Notifications = model.Notifications
                    .OrderByDescending(item => item.CreatedAt)
                    .ToList();
            }

            PopulateNavigationUrls(model.Notifications, role);

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Latest()
        {
            if (!TryGetCurrentUser(out var userId, out var role))
                return Unauthorized();

            var query = _context.SystemNotifications
                .Where(item => item.RecipientUserId == userId);

            var unreadCount = await _context.SystemNotifications
                .CountAsync(item =>
                    item.RecipientUserId == userId
                    && item.Status == "Unread");

            var notifications = await query
                .OrderByDescending(item => item.CreatedAt)
                .AsNoTracking()
                .Take(8)
                .ToListAsync();

            var items = notifications.Select(ToViewModel).ToList();

            if (string.Equals(role, "Student", StringComparison.OrdinalIgnoreCase))
            {
                items.AddRange(await BuildDeadlineNotificationsAsync(userId));
            }

            items = items
                .OrderByDescending(item => item.CreatedAt)
                .Take(8)
                .ToList();

            PopulateNavigationUrls(items, role);

            return Json(new
            {
                unreadCount,
                items = items.Select(item => new
                {
                    item.Id,
                    item.Type,
                    item.Title,
                    item.Message,
                    item.Status,
                    item.InvitationStatus,
                    item.RequiresResponse,
                    url = item.NavigationUrl,
                    createdAt = item.CreatedAt.ToString("yyyy-MM-dd HH:mm")
                })
            });
        }

        [HttpGet]
        public async Task<IActionResult> Go(int id)
        {
            if (!TryGetCurrentUser(out var userId, out var role))
                return NavigationResponse(Url.Action("Login", "Account"));

            if (id <= 0)
            {
                TempData["NotificationError"] = "That notification is no longer available.";
                return NavigationResponse(NotificationsUrl());
            }

            var notification = await _context.SystemNotifications
                .Include(item => item.Topic)
                    .ThenInclude(topic => topic!.TopicBatch)
                .Include(item => item.CourseTask)
                .Include(item => item.TaskSubmission)
                    .ThenInclude(submission => submission!.CourseTask)
                .FirstOrDefaultAsync(item => item.Id == id && item.RecipientUserId == userId);

            if (notification == null)
            {
                TempData["NotificationError"] = "That notification is no longer available.";
                return NavigationResponse(NotificationsUrl());
            }

            if (!string.Equals(notification.Status, "Read", StringComparison.OrdinalIgnoreCase))
            {
                notification.Status = "Read";
                notification.ReadAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                await _hubContext.Clients.All.SendAsync("NotificationsUpdated");
            }

            var target = ResolveNotificationTarget(notification, userId, role);

            if (!string.IsNullOrWhiteSpace(target.Message))
            {
                TempData["NotificationError"] = target.Message;
            }

            return NavigationResponse(target.Url);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkSeen()
        {
            if (!TryGetCurrentUser(out var userId, out _))
            {
                return Unauthorized();
            }

            await MarkUnreadNotificationsReadAsync(userId);
            return Ok(new { unreadCount = 0 });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InviteToGroup(int topicId, int studentId)
        {
            if (!TryGetCurrentUser(out var professorId, out var role)
                || !string.Equals(role, "Professor", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("Login", "Account");
            }

            var topic = await _context.Topics
                .Include(item => item.TopicBatch)
                .FirstOrDefaultAsync(item =>
                    item.Id == topicId
                    && item.TopicBatch != null
                    && item.TopicBatch.CreatedByProfessorId == professorId);

            var student = await _context.Users.FirstOrDefaultAsync(item => item.Id == studentId && item.Role == "Student");

            if (topic == null || student == null)
            {
                TempData["NotificationError"] = "Choose a valid student and available topic.";
                return RedirectToAction(nameof(Index));
            }

            if (topic.ReservedByStudentId.HasValue || string.Equals(topic.Status, "Taken", StringComparison.OrdinalIgnoreCase))
            {
                TempData["NotificationError"] = "This topic is already taken.";
                return RedirectToAction(nameof(Index));
            }

            var hasPendingInvite = await _context.SystemNotifications.AnyAsync(item =>
                item.Type == "GroupInvitation"
                && item.TopicId == topicId
                && item.RecipientUserId == studentId
                && item.InvitationStatus == "Pending");

            if (hasPendingInvite)
            {
                TempData["NotificationError"] = "This student already has a pending invitation for that topic.";
                return RedirectToAction(nameof(Index));
            }

            _context.SystemNotifications.Add(new SystemNotification
            {
                RecipientUserId = studentId,
                ActorUserId = professorId,
                Type = "GroupInvitation",
                Title = "Group invitation",
                Message = $"You were invited to join {topic.TopicBatch?.Title}: {topic.Title}.",
                InvitationStatus = "Pending",
                TopicId = topic.Id,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            await _hubContext.Clients.All.SendAsync("NotificationsUpdated");

            TempData["NotificationSuccess"] = "Invitation sent.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptInvitation(int id)
        {
            if (!TryGetCurrentUser(out var studentId, out var role)
                || !string.Equals(role, "Student", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("Login", "Account");
            }

            var invitation = await _context.SystemNotifications
                .Include(item => item.Topic)
                    .ThenInclude(topic => topic!.TopicBatch)
                .FirstOrDefaultAsync(item =>
                    item.Id == id
                    && item.RecipientUserId == studentId
                    && item.Type == "GroupInvitation");

            if (invitation?.Topic == null || invitation.InvitationStatus != "Pending")
            {
                TempData["NotificationError"] = "This invitation is no longer available.";
                return RedirectToAction(nameof(Index));
            }

            if (invitation.Topic.ReservedByStudentId.HasValue
                || string.Equals(invitation.Topic.Status, "Taken", StringComparison.OrdinalIgnoreCase))
            {
                invitation.InvitationStatus = "Declined";
                invitation.RespondedAt = DateTime.UtcNow;
                invitation.Status = "Read";
                invitation.ReadAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                TempData["NotificationError"] = "This topic has already been taken.";
                return RedirectToAction(nameof(Index));
            }

            var joinedAt = DateTime.UtcNow;

            invitation.Topic.Status = "Taken";
            invitation.Topic.ReservedByStudentId = studentId;
            invitation.Topic.ReservedAt = joinedAt;
            invitation.InvitationStatus = "Accepted";
            invitation.RespondedAt = joinedAt;
            invitation.Status = "Read";
            invitation.ReadAt = joinedAt;

            if (invitation.ActorUserId.HasValue)
            {
                _context.SystemNotifications.Add(new SystemNotification
                {
                    RecipientUserId = invitation.ActorUserId.Value,
                    ActorUserId = studentId,
                    Type = "InvitationStatus",
                    Title = "Invitation accepted",
                    Message = $"A student accepted the invitation for {invitation.Topic.TopicBatch?.Title}: {invitation.Topic.Title}.",
                    TopicId = invitation.TopicId,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
            await _hubContext.Clients.All.SendAsync("TopicTaken", invitation.Topic.Id);
            await _hubContext.Clients.All.SendAsync("NotificationsUpdated");

            TempData["NotificationSuccess"] = "Invitation accepted.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeclineInvitation(int id)
        {
            if (!TryGetCurrentUser(out var studentId, out var role)
                || !string.Equals(role, "Student", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("Login", "Account");
            }

            var invitation = await _context.SystemNotifications
                .Include(item => item.Topic)
                    .ThenInclude(topic => topic!.TopicBatch)
                .FirstOrDefaultAsync(item =>
                    item.Id == id
                    && item.RecipientUserId == studentId
                    && item.Type == "GroupInvitation");

            if (invitation == null || invitation.InvitationStatus != "Pending")
            {
                TempData["NotificationError"] = "This invitation is no longer available.";
                return RedirectToAction(nameof(Index));
            }

            invitation.InvitationStatus = "Declined";
            invitation.RespondedAt = DateTime.UtcNow;
            invitation.Status = "Read";
            invitation.ReadAt = DateTime.UtcNow;

            if (invitation.ActorUserId.HasValue)
            {
                _context.SystemNotifications.Add(new SystemNotification
                {
                    RecipientUserId = invitation.ActorUserId.Value,
                    ActorUserId = studentId,
                    Type = "InvitationStatus",
                    Title = "Invitation declined",
                    Message = $"A student declined the invitation for {invitation.Topic?.TopicBatch?.Title}: {invitation.Topic?.Title}.",
                    TopicId = invitation.TopicId,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
            await _hubContext.Clients.All.SendAsync("NotificationsUpdated");

            TempData["NotificationSuccess"] = "Invitation declined.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<List<NotificationItemViewModel>> BuildDeadlineNotificationsAsync(int studentId)
        {
            var now = DateTime.UtcNow;
            var closeDate = now.AddDays(3);

            return await _context.CourseTasks
                .Where(task =>
                    task.Deadline >= now
                    && task.Deadline <= closeDate
                    && !task.Submissions.Any(submission => submission.StudentId == studentId))
                .OrderBy(task => task.Deadline)
                .AsNoTracking()
                .Select(task => new NotificationItemViewModel
                {
                    Type = "DeadlineClose",
                    Title = "Deadline close",
                    Message = $"{task.Title} is due on {task.Deadline:yyyy-MM-dd HH:mm}.",
                    Status = "Important",
                    CreatedAt = task.Deadline
                })
                .ToListAsync();
        }

        private void PopulateNavigationUrls(IEnumerable<NotificationItemViewModel> notifications, string role)
        {
            foreach (var notification in notifications)
            {
                notification.NavigationUrl = notification.Id.HasValue
                    ? NotificationGoUrl(notification.Id.Value)
                    : ResolveTargetUrlByType(notification.Type, role);
            }
        }

        private NotificationNavigationTarget ResolveNotificationTarget(SystemNotification notification, int userId, string role)
        {
            var type = notification.Type ?? string.Empty;

            if (!IsProfessorRole(role) && !IsStudentRole(role))
            {
                return new NotificationNavigationTarget(
                    NotificationsUrl(),
                    "Please sign in again before opening that notification.");
            }

            if (IsDeadlineNotification(type))
            {
                if (notification.CourseTaskId.HasValue && notification.CourseTask == null)
                {
                    return MissingTarget();
                }

                return new NotificationNavigationTarget(TasksUrl(role));
            }

            if (IsGroupNotification(type))
            {
                if ((RequiresGroupTarget(type) && !HasTopicTarget(notification))
                    || (notification.TopicId.HasValue && !HasTopicTarget(notification)))
                {
                    return MissingTarget();
                }

                if (IsProfessorRole(role)
                    && notification.Topic?.TopicBatch != null
                    && notification.Topic.TopicBatch.CreatedByProfessorId != userId)
                {
                    return MissingTarget();
                }

                return new NotificationNavigationTarget(GroupsUrl(role));
            }

            if (IsTopicNotification(type))
            {
                if (!HasTopicTarget(notification))
                {
                    return MissingTarget();
                }

                if (IsProfessorRole(role)
                    && notification.Topic!.TopicBatch!.CreatedByProfessorId != userId)
                {
                    return MissingTarget();
                }

                return new NotificationNavigationTarget(TopicsUrl(role));
            }

            if (IsTaskNotification(type))
            {
                if (!HasTaskTarget(notification))
                {
                    return MissingTarget();
                }

                if (RequiresSubmissionTarget(type)
                    && (!notification.TaskSubmissionId.HasValue || notification.TaskSubmission == null))
                {
                    return MissingTarget();
                }

                var task = notification.CourseTask ?? notification.TaskSubmission?.CourseTask;

                if (IsProfessorRole(role) && task?.ProfessorId != userId)
                {
                    return MissingTarget();
                }

                if (IsStudentRole(role)
                    && RequiresSubmissionTarget(type)
                    && notification.TaskSubmission?.StudentId != userId)
                {
                    return MissingTarget();
                }

                return new NotificationNavigationTarget(TasksUrl(role));
            }

            return new NotificationNavigationTarget(
                NotificationsUrl(),
                "We could not find a destination for that notification.");
        }

        private IActionResult NavigationResponse(string? url)
        {
            var safeUrl = ResolveSafeLocalUrl(url);

            if (WantsJsonNavigation())
            {
                return Json(new { url = safeUrl });
            }

            return LocalRedirect(safeUrl);
        }

        private bool WantsJsonNavigation()
        {
            return Request.Headers.Accept.ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase)
                || string.Equals(Request.Headers["X-Requested-With"].ToString(), "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
        }

        private string ResolveSafeLocalUrl(string? url)
        {
            if (!string.IsNullOrWhiteSpace(url) && Url.IsLocalUrl(url))
            {
                return url;
            }

            return NotificationsUrl();
        }

        private NotificationNavigationTarget MissingTarget()
        {
            return new NotificationNavigationTarget(
                NotificationsUrl(),
                "The item linked to that notification is no longer available.");
        }

        private string ResolveTargetUrlByType(string? type, string role)
        {
            var normalizedType = type ?? string.Empty;

            if (IsDeadlineNotification(normalizedType) || IsTaskNotification(normalizedType))
            {
                return TasksUrl(role);
            }

            if (IsTopicNotification(normalizedType))
            {
                return TopicsUrl(role);
            }

            if (IsGroupNotification(normalizedType))
            {
                return GroupsUrl(role);
            }

            return NotificationsUrl();
        }

        private string NotificationGoUrl(int id)
        {
            return Url.Action(nameof(Go), "Notifications", new { id }) ?? NotificationsUrl();
        }

        private string NotificationsUrl()
        {
            return Url.Action(nameof(Index), "Notifications") ?? "/Notifications";
        }

        private string GroupsUrl(string role)
        {
            return Url.Action("Groups", "Dashboard") ?? NotificationsUrl();
        }

        private string TopicsUrl(string role)
        {
            return string.Equals(role, "Professor", StringComparison.OrdinalIgnoreCase)
                ? Url.Action("Index", "ProfessorTopics") ?? NotificationsUrl()
                : Url.Action("Index", "StudentTopics") ?? NotificationsUrl();
        }

        private string TasksUrl(string role)
        {
            return string.Equals(role, "Professor", StringComparison.OrdinalIgnoreCase)
                ? Url.Action("Index", "ProfessorTasks") ?? NotificationsUrl()
                : Url.Action("Index", "StudentTasks") ?? NotificationsUrl();
        }

        private static bool IsDeadlineNotification(string type)
        {
            return type.Contains("Deadline", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsGroupNotification(string type)
        {
            return type.Contains("Group", StringComparison.OrdinalIgnoreCase)
                || type.Contains("Invitation", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsTopicNotification(string type)
        {
            return type.Contains("Topic", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsTaskNotification(string type)
        {
            return type.Contains("Task", StringComparison.OrdinalIgnoreCase)
                || type.Contains("Submission", StringComparison.OrdinalIgnoreCase)
                || type.Contains("Feedback", StringComparison.OrdinalIgnoreCase);
        }

        private static bool HasTopicTarget(SystemNotification notification)
        {
            return notification.TopicId.HasValue
                && notification.Topic != null
                && notification.Topic.TopicBatch != null;
        }

        private static bool HasTaskTarget(SystemNotification notification)
        {
            return notification.CourseTask != null
                || notification.TaskSubmission?.CourseTask != null;
        }

        private static bool RequiresSubmissionTarget(string type)
        {
            return type.Contains("Submission", StringComparison.OrdinalIgnoreCase)
                || type.Contains("Feedback", StringComparison.OrdinalIgnoreCase);
        }

        private static bool RequiresGroupTarget(string type)
        {
            return type.Contains("GroupJoinRequest", StringComparison.OrdinalIgnoreCase)
                || type.Contains("GroupInvitation", StringComparison.OrdinalIgnoreCase)
                || type.Contains("InvitationStatus", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsProfessorRole(string role)
        {
            return string.Equals(role, "Professor", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsStudentRole(string role)
        {
            return string.Equals(role, "Student", StringComparison.OrdinalIgnoreCase);
        }

        private async Task MarkUnreadNotificationsReadAsync(int userId)
        {
            var unreadNotifications = await _context.SystemNotifications
                .Where(item => item.RecipientUserId == userId && item.Status == "Unread")
                .ToListAsync();

            if (!unreadNotifications.Any())
            {
                return;
            }

            var readAt = DateTime.UtcNow;

            foreach (var notification in unreadNotifications)
            {
                notification.Status = "Read";
                notification.ReadAt = readAt;
            }

            await _context.SaveChangesAsync();
        }

        private static NotificationItemViewModel ToViewModel(SystemNotification notification)
        {
            return new NotificationItemViewModel
            {
                Id = notification.Id,
                Type = notification.Type,
                Title = notification.Title,
                Message = notification.Message,
                Status = notification.Status,
                InvitationStatus = notification.InvitationStatus,
                CreatedAt = notification.CreatedAt,
                RequiresResponse = notification.Type == "GroupInvitation" && notification.InvitationStatus == "Pending"
            };
        }

        private bool TryGetCurrentUser(out int userId, out string role)
        {
            role = HttpContext.Session.GetString("UserRole") ?? string.Empty;
            var userIdString = HttpContext.Session.GetString("UserId");
            return int.TryParse(userIdString, out userId);
        }

        private sealed record NotificationNavigationTarget(string Url, string? Message = null);
    }
}
