using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RealTimeCollaborationSystem.Data;
using RealTimeCollaborationSystem.Hubs;
using RealTimeCollaborationSystem.Models;
using RealTimeCollaborationSystem.ViewModels;

namespace RealTimeCollaborationSystem.Controllers
{
    public class DashboardController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<CollaborationHub> _hubContext;

        public DashboardController(AppDbContext context, IHubContext<CollaborationHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var userId = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userId))
            {
                context.Result = Redirect("/Account/Login");
                return;
            }

            base.OnActionExecuting(context);
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var role = HttpContext.Session.GetString("UserRole") ?? "Student";
            var userIdString = HttpContext.Session.GetString("UserId");

            if (!int.TryParse(userIdString, out var userId))
                return Redirect("/Account/Login");

            var model = role == "Professor"
                ? await BuildProfessorDashboardAsync(userId)
                : await BuildStudentDashboardAsync(userId);

            return View(model);
        }

        [HttpGet]
        public IActionResult Projects()
        {
            return RedirectToAction(nameof(Groups));
        }

        [HttpGet]
        public async Task<IActionResult> Groups(string? search)
        {
            var role = HttpContext.Session.GetString("UserRole") ?? "Student";

            if (!string.Equals(role, "Professor", StringComparison.OrdinalIgnoreCase))
                return View(await BuildStudentGroupsModelAsync(search));

            var userIdString = HttpContext.Session.GetString("UserId");

            if (!int.TryParse(userIdString, out var professorId))
                return Redirect("/Account/Login");

            var batches = await _context.TopicBatches
                .Include(batch => batch.Topics)
                    .ThenInclude(topic => topic.ReservedByStudent)
                .Where(batch => batch.CreatedByProfessorId == professorId)
                .OrderByDescending(batch => batch.CreatedAt)
                .AsNoTracking()
                .ToListAsync();

            var pendingJoinRequests = await _context.SystemNotifications
                .Include(notification => notification.ActorUser)
                .Include(notification => notification.Topic)
                    .ThenInclude(topic => topic!.TopicBatch)
                .Where(notification =>
                    notification.Type == "GroupJoinRequest"
                    && notification.RecipientUserId == professorId
                    && notification.InvitationStatus == "Pending"
                    && notification.ActorUserId.HasValue
                    && notification.Topic != null
                    && notification.Topic.TopicBatch != null
                    && notification.Topic.TopicBatch.CreatedByProfessorId == professorId)
                .OrderByDescending(notification => notification.CreatedAt)
                .AsNoTracking()
                .ToListAsync();

            var normalizedSearch = search?.Trim() ?? string.Empty;
            var searchResults = new List<ProfessorGroupStudentSearchItem>();

            if (!string.IsNullOrWhiteSpace(normalizedSearch))
            {
                searchResults = await _context.Users
                    .Where(user =>
                        user.Role == "Student"
                        && (user.Name.Contains(normalizedSearch)
                            || user.Email.Contains(normalizedSearch)
                            || (user.UniversityOrInstitution != null && user.UniversityOrInstitution.Contains(normalizedSearch))
                            || (user.FieldOfStudy != null && user.FieldOfStudy.Contains(normalizedSearch))))
                    .OrderBy(user => user.Name)
                    .Take(10)
                    .AsNoTracking()
                    .Select(user => new ProfessorGroupStudentSearchItem
                    {
                        Id = user.Id,
                        Name = user.Name,
                        Email = user.Email,
                        UniversityOrInstitution = user.UniversityOrInstitution,
                        FieldOfStudy = user.FieldOfStudy,
                        PhotoUrl = ResolvePhotoUrl(user.ProfilePhotoUrl, user.PhotoUrl)
                    })
                    .ToListAsync();
            }

            var batchItems = batches.Select(batch =>
            {
                var topics = batch.Topics.OrderBy(topic => topic.Title).ToList();
                var members = topics
                    .Where(topic => topic.ReservedByStudent != null)
                    .Select(topic => new ProfessorGroupMemberItem
                    {
                        StudentId = topic.ReservedByStudent!.Id,
                        Name = DisplayName(topic.ReservedByStudent.Name, topic.ReservedByStudent.Email, "Student"),
                        Email = topic.ReservedByStudent.Email,
                        UniversityOrInstitution = topic.ReservedByStudent.UniversityOrInstitution,
                        FieldOfStudy = topic.ReservedByStudent.FieldOfStudy,
                        PhotoUrl = ResolvePhotoUrl(topic.ReservedByStudent.ProfilePhotoUrl, topic.ReservedByStudent.PhotoUrl),
                        JoinedAt = topic.ReservedAt
                    })
                    .OrderBy(member => member.Name)
                    .ToList();

                var takenTopicCount = topics.Count(IsTopicTaken);

                return new ProfessorGroupBatchItem
                {
                    Id = batch.Id,
                    Title = batch.Title,
                    IsPublished = batch.IsPublished,
                    TopicCount = topics.Count,
                    AvailableTopicCount = topics.Count - takenTopicCount,
                    TakenTopicCount = takenTopicCount,
                    Members = members
                };
            }).ToList();

            var model = new ProfessorGroupsViewModel
            {
                IsProfessor = true,
                Search = normalizedSearch,
                Batches = batchItems,
                SearchResults = searchResults,
                AvailableGroupOptions = batchItems
                    .Where(batch =>
                        batch.AvailableTopicCount > 0
                        && !batch.Members.Any(m => searchResults
                            .Select(s => s.Id)
                            .Contains(m.StudentId)))
                    .Select(batch => new ProfessorGroupOption
                    {
                        BatchId = batch.Id,
                        BatchTitle = batch.Title,
                        AvailableSlots = batch.AvailableTopicCount
                    })
                    .ToList(),
                PendingJoinRequests = pendingJoinRequests
                    .Select(request => new ProfessorGroupJoinRequestItem
                    {
                        NotificationId = request.Id,
                        StudentId = request.ActorUserId!.Value,
                        StudentName = DisplayName(request.ActorUser?.Name, request.ActorUser?.Email, "Student"),
                        StudentEmail = request.ActorUser?.Email ?? string.Empty,
                        BatchId = request.Topic!.TopicBatchId,
                        BatchTitle = request.Topic.TopicBatch?.Title ?? "Topic group",
                        RequestedAt = request.CreatedAt
                    })
                    .ToList()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestJoinGroup(int batchId, string? search)
        {
            var role = HttpContext.Session.GetString("UserRole") ?? "Student";

            if (!string.Equals(role, "Student", StringComparison.OrdinalIgnoreCase))
                return RedirectToAction(nameof(Index));

            var userIdString = HttpContext.Session.GetString("UserId");

            if (!int.TryParse(userIdString, out var studentId))
                return Redirect("/Account/Login");

            var batch = await _context.TopicBatches
                .Include(item => item.CreatedByProfessor)
                .Include(item => item.Topics)
                .FirstOrDefaultAsync(item => item.Id == batchId && item.IsPublished);

            if (batch?.CreatedByProfessor == null)
            {
                TempData["GroupsError"] = "Choose a valid active group.";
                return RedirectToAction(nameof(Groups), new { search });
            }

            var alreadyInBatch = batch.Topics.Any(topic => topic.ReservedByStudentId == studentId);

            if (alreadyInBatch)
            {
                TempData["GroupsError"] = "You already belong to this group.";
                return RedirectToAction(nameof(Groups), new { search });
            }

            var topicIds = batch.Topics.Select(topic => topic.Id).ToList();
            var hasPendingRequest = await _context.SystemNotifications
                .AnyAsync(item =>
                    item.Type == "GroupJoinRequest"
                    && item.ActorUserId == studentId
                    && item.RecipientUserId == batch.CreatedByProfessorId
                    && item.InvitationStatus == "Pending"
                    && item.TopicId.HasValue
                    && topicIds.Contains(item.TopicId.Value));

            if (hasPendingRequest)
            {
                TempData["GroupsError"] = "You already sent a request for this group.";
                return RedirectToAction(nameof(Groups), new { search });
            }

            var referenceTopic = batch.Topics
                .OrderBy(topic => IsTopicTaken(topic))
                .ThenBy(topic => topic.Title)
                .FirstOrDefault();

            _context.SystemNotifications.Add(new SystemNotification
            {
                RecipientUserId = batch.CreatedByProfessorId,
                ActorUserId = studentId,
                Type = "GroupJoinRequest",
                Title = "Group join request",
                Message = $"{HttpContext.Session.GetString("UserName") ?? "A student"} requested to join {batch.Title}.",
                InvitationStatus = "Pending",
                TopicId = referenceTopic?.Id,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            await _hubContext.Clients.All.SendAsync("NotificationsUpdated");

            TempData["GroupsSuccess"] = "Request sent to the professor.";
            return RedirectToAction(nameof(Groups), new { search });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddStudentToGroup(int studentId, int batchId, string? search)
        {
            var role = HttpContext.Session.GetString("UserRole") ?? "Student";

            if (!string.Equals(role, "Professor", StringComparison.OrdinalIgnoreCase))
                return RedirectToAction(nameof(Index));

            var userIdString = HttpContext.Session.GetString("UserId");

            if (!int.TryParse(userIdString, out var professorId))
                return Redirect("/Account/Login");

            var batch = await _context.TopicBatches
                .Include(item => item.Topics)
                .FirstOrDefaultAsync(item =>
                    item.Id == batchId
                    && item.CreatedByProfessorId == professorId);

            var student = await _context.Users
                .FirstOrDefaultAsync(user => user.Id == studentId && user.Role == "Student");

            if (batch == null || student == null)
            {
                TempData["GroupsError"] = "Choose a valid student and group.";
                return RedirectToAction(nameof(Groups), new { search });
            }

            var availableTopic = batch.Topics
                .OrderBy(topic => topic.Title)
                .FirstOrDefault(topic => !IsTopicTaken(topic));

            if (availableTopic == null)
            {
                TempData["GroupsError"] = "That group has no open spots.";
                return RedirectToAction(nameof(Groups), new { search });
            }

            var alreadyInBatch = await _context.Topics.AnyAsync(item =>
                item.TopicBatchId == batch.Id
                && item.ReservedByStudentId == studentId);

            if (alreadyInBatch)
            {
                TempData["GroupsError"] = "This student is already inside that group.";
                return RedirectToAction(nameof(Groups), new { search });
            }

            var joinedAt = DateTime.UtcNow;

            availableTopic.Status = "Taken";
            availableTopic.ReservedByStudentId = studentId;
            availableTopic.ReservedAt = joinedAt;

            await AcceptPendingJoinRequestsAsync(studentId, professorId, batch.Topics.Select(topic => topic.Id).ToList(), joinedAt);

            await _context.SaveChangesAsync();

            TempData["GroupsSuccess"] = "Student added to the group.";
            return RedirectToAction(nameof(Groups), new { search });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptGroupJoinRequest(int id, string? search)
        {
            var role = HttpContext.Session.GetString("UserRole") ?? "Student";

            if (!string.Equals(role, "Professor", StringComparison.OrdinalIgnoreCase))
                return RedirectToAction(nameof(Index));

            var userIdString = HttpContext.Session.GetString("UserId");

            if (!int.TryParse(userIdString, out var professorId))
                return Redirect("/Account/Login");

            var request = await _context.SystemNotifications
                .Include(notification => notification.ActorUser)
                .Include(notification => notification.Topic)
                    .ThenInclude(topic => topic!.TopicBatch)
                        .ThenInclude(batch => batch!.Topics)
                .FirstOrDefaultAsync(notification =>
                    notification.Id == id
                    && notification.Type == "GroupJoinRequest"
                    && notification.RecipientUserId == professorId);

            if (request?.Topic?.TopicBatch == null
                || request.Topic.TopicBatch.CreatedByProfessorId != professorId)
            {
                TempData["GroupsError"] = "Choose a valid join request.";
                return RedirectToAction(nameof(Groups), new { search });
            }

            if (request.InvitationStatus != "Pending")
            {
                TempData["GroupsError"] = "This request is no longer pending.";
                return RedirectToAction(nameof(Groups), new { search });
            }

            if (!request.ActorUserId.HasValue
                || request.ActorUser == null
                || !string.Equals(request.ActorUser.Role, "Student", StringComparison.OrdinalIgnoreCase))
            {
                TempData["GroupsError"] = "Choose a valid student request.";
                return RedirectToAction(nameof(Groups), new { search });
            }

            var batchId = request.Topic.TopicBatchId;
            var joinedAt = DateTime.UtcNow;
            var alreadyInBatch = await _context.Topics.AnyAsync(topic =>
                    topic.TopicBatchId == batchId
                    && topic.ReservedByStudentId == request.ActorUserId.Value);

            if (!alreadyInBatch)
            {
                var availableTopic = request.Topic.TopicBatch.Topics
                    .OrderBy(topic => topic.Title)
                    .FirstOrDefault(topic => !IsTopicTaken(topic));

                if (availableTopic == null)
                {
                    TempData["GroupsError"] = "That group has no open spots.";
                    return RedirectToAction(nameof(Groups), new { search });
                }

                availableTopic.Status = "Taken";
                availableTopic.ReservedByStudentId = request.ActorUserId.Value;
                availableTopic.ReservedAt = joinedAt;
            }

            request.InvitationStatus = "Accepted";
            request.RespondedAt = joinedAt;
            request.Status = "Read";
            request.ReadAt ??= joinedAt;

            await _context.SaveChangesAsync();
            await _hubContext.Clients.All.SendAsync("NotificationsUpdated");

            TempData["GroupsSuccess"] = "Join request accepted.";
            return RedirectToAction(nameof(Groups), new { search });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DenyGroupJoinRequest(int id, string? search)
        {
            var role = HttpContext.Session.GetString("UserRole") ?? "Student";

            if (!string.Equals(role, "Professor", StringComparison.OrdinalIgnoreCase))
                return RedirectToAction(nameof(Index));

            var userIdString = HttpContext.Session.GetString("UserId");

            if (!int.TryParse(userIdString, out var professorId))
                return Redirect("/Account/Login");

            var request = await _context.SystemNotifications
                .Include(notification => notification.Topic)
                    .ThenInclude(topic => topic!.TopicBatch)
                .FirstOrDefaultAsync(notification =>
                    notification.Id == id
                    && notification.Type == "GroupJoinRequest"
                    && notification.RecipientUserId == professorId);

            if (request?.Topic?.TopicBatch == null
                || request.Topic.TopicBatch.CreatedByProfessorId != professorId)
            {
                TempData["GroupsError"] = "Choose a valid join request.";
                return RedirectToAction(nameof(Groups), new { search });
            }

            if (request.InvitationStatus != "Pending")
            {
                TempData["GroupsError"] = "This request is no longer pending.";
                return RedirectToAction(nameof(Groups), new { search });
            }

            var deniedAt = DateTime.UtcNow;

            request.InvitationStatus = "Declined";
            request.RespondedAt = deniedAt;
            request.Status = "Read";
            request.ReadAt ??= deniedAt;

            await _context.SaveChangesAsync();
            await _hubContext.Clients.All.SendAsync("NotificationsUpdated");

            TempData["GroupsSuccess"] = "Join request denied.";
            return RedirectToAction(nameof(Groups), new { search });
        }

        [HttpGet]
        public IActionResult Tasks()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Messages()
        {
            return RedirectToAction("Index", "Notifications");
        }

        [HttpGet]
        public IActionResult Settings()
        {
            return RedirectToAction("Index", "Settings");
        }

        private async Task<DashboardViewModel> BuildProfessorDashboardAsync(int professorId)
        {
            var now = DateTime.UtcNow;
            var professorTopics = _context.Topics
                .Where(topic => topic.TopicBatch != null && topic.TopicBatch.CreatedByProfessorId == professorId);
            var professorTasks = _context.CourseTasks
                .Where(task => task.ProfessorId == professorId);
            var professorSubmissions = _context.TaskSubmissions
                .Where(submission => submission.CourseTask != null && submission.CourseTask.ProfessorId == professorId);

            var publishedGroups = await _context.TopicBatches
                .CountAsync(batch => batch.CreatedByProfessorId == professorId && batch.IsPublished);
            var totalTopics = await professorTopics.CountAsync();
            var takenTopics = await professorTopics
                .CountAsync(topic => topic.ReservedByStudentId.HasValue || topic.Status == "Taken");
            var availableTopics = await professorTopics
                .CountAsync(topic => !topic.ReservedByStudentId.HasValue && topic.Status != "Taken");
            var totalTasks = await professorTasks.CountAsync();
            var pendingSubmissions = await professorSubmissions
                .CountAsync(submission => submission.Status != "Reviewed");
            var reviewedSubmissions = await professorSubmissions
                .CountAsync(submission => submission.Status == "Reviewed");
            var upcomingDeadlineCount = await professorTasks
                .CountAsync(task => task.Deadline >= now);

            var upcomingDeadlines = await professorTasks
                .Where(task => task.Deadline >= now)
                .OrderBy(task => task.Deadline)
                .Take(5)
                .Select(task => new DashboardDeadlineItem
                {
                    Title = task.Title,
                    Subtitle = "Task deadline",
                    DueAt = task.Deadline,
                    Status = "Open"
                })
                .ToListAsync();

            var recentSubmissions = await _context.TaskSubmissions
                .Include(submission => submission.CourseTask)
                .Include(submission => submission.Student)
                .Where(submission => submission.CourseTask != null && submission.CourseTask.ProfessorId == professorId)
                .OrderByDescending(submission => submission.SubmittedAt)
                .Take(6)
                .AsNoTracking()
                .ToListAsync();

            var recentTopicSelections = await _context.Topics
                .Include(topic => topic.TopicBatch)
                .Include(topic => topic.ReservedByStudent)
                .Where(topic =>
                    topic.TopicBatch != null
                    && topic.TopicBatch.CreatedByProfessorId == professorId
                    && topic.ReservedByStudentId.HasValue
                    && topic.ReservedAt.HasValue)
                .OrderByDescending(topic => topic.ReservedAt)
                .Take(6)
                .AsNoTracking()
                .ToListAsync();

            var recentActivity = recentSubmissions
                .Select(submission => new DashboardActivityItem
                {
                    Title = $"{DisplayName(submission.Student?.Name, submission.Student?.Email, "Student")} submitted work",
                    Detail = submission.CourseTask?.Title ?? "Task submission",
                    OccurredAt = submission.SubmittedAt,
                    Tone = submission.Status == "Reviewed" ? "success" : "info"
                })
                .Concat(recentTopicSelections.Select(topic => new DashboardActivityItem
                {
                    Title = $"{DisplayName(topic.ReservedByStudent?.Name, topic.ReservedByStudent?.Email, "Student")} selected a topic",
                    Detail = $"{topic.Title} · {topic.TopicBatch?.Title ?? "Topic group"}",
                    OccurredAt = topic.ReservedAt ?? DateTime.MinValue,
                    Tone = "accent"
                }))
                .OrderByDescending(activity => activity.OccurredAt)
                .Take(6)
                .ToList();

            return new DashboardViewModel
            {
                Role = "Professor",
                Stats = new List<DashboardStatItem>
                {
                    new() { Label = "Published groups", Value = publishedGroups.ToString(), Hint = "Visible topic groups", Tone = "primary" },
                    new() { Label = "Total topics", Value = totalTopics.ToString(), Hint = "Topics you created", Tone = "neutral" },
                    new() { Label = "Taken topics", Value = takenTopics.ToString(), Hint = "Reserved by students", Tone = "warning" },
                    new() { Label = "Available topics", Value = availableTopics.ToString(), Hint = "Still open", Tone = "success" },
                    new() { Label = "Total tasks", Value = totalTasks.ToString(), Hint = "Published assignments", Tone = "primary" },
                    new() { Label = "Pending submissions", Value = pendingSubmissions.ToString(), Hint = "Need feedback", Tone = "warning" },
                    new() { Label = "Reviewed submissions", Value = reviewedSubmissions.ToString(), Hint = "Feedback saved", Tone = "success" },
                    new() { Label = "Upcoming deadlines", Value = upcomingDeadlineCount.ToString(), Hint = "Future task deadlines", Tone = "accent" }
                },
                UpcomingDeadlines = upcomingDeadlines,
                RecentActivity = recentActivity,
                QuickActions = new List<DashboardQuickAction>
                {
                    new() { Label = "Create topic list", Controller = "ProfessorTopics", Action = "CreateBatch", IsPrimary = true },
                    new() { Label = "Create task", Controller = "ProfessorTasks", Action = "Index" }
                }
            };
        }

        private async Task<DashboardViewModel> BuildStudentDashboardAsync(int studentId)
        {
            var now = DateTime.UtcNow;
            var studentSubmissions = _context.TaskSubmissions
                .Where(submission => submission.StudentId == studentId);

            var selectedTopics = await _context.Topics
                .Include(topic => topic.TopicBatch)
                    .ThenInclude(batch => batch!.CreatedByProfessor)
                .Where(topic => topic.ReservedByStudentId == studentId)
                .OrderByDescending(topic => topic.ReservedAt)
                .AsNoTracking()
                .ToListAsync();

            var assignedTasks = await _context.CourseTasks.CountAsync();
            var submittedTasks = await studentSubmissions.CountAsync();
            var reviewedSubmissions = await studentSubmissions
                .CountAsync(submission => submission.Status == "Reviewed");
            var pendingTasks = await _context.CourseTasks
                .CountAsync(task => !task.Submissions.Any(submission => submission.StudentId == studentId));
            var upcomingDeadlineCount = await _context.CourseTasks
                .CountAsync(task => task.Deadline >= now);

            var upcomingTasks = await _context.CourseTasks
                .Include(task => task.Professor)
                .Include(task => task.Submissions.Where(submission => submission.StudentId == studentId))
                .Where(task => task.Deadline >= now)
                .OrderBy(task => task.Deadline)
                .Take(5)
                .AsNoTracking()
                .ToListAsync();

            var upcomingDeadlines = upcomingTasks
                .Select(task =>
                {
                    var submission = task.Submissions.FirstOrDefault();
                    return new DashboardDeadlineItem
                    {
                        Title = task.Title,
                        Subtitle = string.IsNullOrWhiteSpace(task.Professor?.Name) ? "Task deadline" : task.Professor.Name,
                        DueAt = task.Deadline,
                        Status = submission?.Status ?? "Not submitted"
                    };
                })
                .ToList();

            var reviewedItems = await _context.TaskSubmissions
                .Include(submission => submission.CourseTask)
                .Where(submission => submission.StudentId == studentId && submission.Status == "Reviewed")
                .OrderByDescending(submission => submission.ReviewedAt ?? submission.SubmittedAt)
                .Take(5)
                .AsNoTracking()
                .ToListAsync();

            var newestTasks = await _context.CourseTasks
                .OrderByDescending(task => task.CreatedAt)
                .Take(3)
                .AsNoTracking()
                .ToListAsync();

            var recentActivity = reviewedItems
                .Select(submission => new DashboardActivityItem
                {
                    Title = "Feedback received",
                    Detail = submission.CourseTask?.Title ?? "Reviewed submission",
                    OccurredAt = submission.ReviewedAt ?? submission.SubmittedAt,
                    Tone = "success"
                })
                .Concat(newestTasks.Select(task => new DashboardActivityItem
                {
                    Title = "Task available",
                    Detail = task.Title,
                    OccurredAt = task.CreatedAt,
                    Tone = "info"
                }))
                .Concat(selectedTopics.Take(3).Select(topic => new DashboardActivityItem
                {
                    Title = "Topic selected",
                    Detail = $"{topic.Title} · {topic.TopicBatch?.Title ?? "Topic group"}",
                    OccurredAt = topic.ReservedAt ?? DateTime.MinValue,
                    Tone = "accent"
                }))
                .OrderByDescending(activity => activity.OccurredAt)
                .Take(6)
                .ToList();

            return new DashboardViewModel
            {
                Role = "Student",
                Stats = new List<DashboardStatItem>
                {
                    new() { Label = "Selected topics", Value = selectedTopics.Count.ToString(), Hint = "Reserved by you", Tone = selectedTopics.Any() ? "success" : "neutral" },
                    new() { Label = "Assigned tasks", Value = assignedTasks.ToString(), Hint = "Available tasks", Tone = "primary" },
                    new() { Label = "Submitted tasks", Value = submittedTasks.ToString(), Hint = "Work uploaded", Tone = "info" },
                    new() { Label = "Pending tasks", Value = pendingTasks.ToString(), Hint = "Need submission", Tone = pendingTasks > 0 ? "warning" : "success" },
                    new() { Label = "Reviewed feedback", Value = reviewedSubmissions.ToString(), Hint = "Professor reviewed", Tone = "success" },
                    new() { Label = "Upcoming deadlines", Value = upcomingDeadlineCount.ToString(), Hint = "Future task deadlines", Tone = "accent" }
                },
                SelectedTopics = selectedTopics.Select(topic => new DashboardTopicItem
                {
                    Title = topic.Title,
                    GroupName = topic.TopicBatch?.Title ?? "Topic group",
                    ProfessorName = DisplayName(topic.TopicBatch?.CreatedByProfessor?.Name, topic.TopicBatch?.CreatedByProfessor?.Email, "Professor"),
                    SelectedAt = topic.ReservedAt
                }).ToList(),
                UpcomingDeadlines = upcomingDeadlines,
                RecentActivity = recentActivity,
                QuickActions = new List<DashboardQuickAction>
                {
                    new() { Label = "View topics", Controller = "StudentTopics", Action = "Index", IsPrimary = true },
                    new() { Label = "View tasks", Controller = "StudentTasks", Action = "Index" }
                }
            };
        }

        private async Task<ProfessorGroupsViewModel> BuildStudentGroupsModelAsync(string? search)
        {
            var userIdString = HttpContext.Session.GetString("UserId");

            if (!int.TryParse(userIdString, out var studentId))
                return new ProfessorGroupsViewModel { IsStudent = true };

            var normalizedSearch = search?.Trim() ?? string.Empty;
            var model = new ProfessorGroupsViewModel
            {
                IsStudent = true,
                Search = normalizedSearch
            };

            var batches = await _context.TopicBatches
                .Include(batch => batch.CreatedByProfessor)
                .Include(batch => batch.Topics)
                .Where(batch => batch.IsPublished)
                .OrderByDescending(batch => batch.CreatedAt)
                .AsNoTracking()
                .ToListAsync();

            var batchTopicIds = batches
                .SelectMany(batch => batch.Topics.Select(topic => topic.Id))
                .ToList();

            var pendingRequestBatchIds = await _context.SystemNotifications
                .Include(item => item.Topic)
                .Where(item =>
                    item.Type == "GroupJoinRequest"
                    && item.ActorUserId == studentId
                    && item.InvitationStatus == "Pending"
                    && item.Topic != null
                    && item.TopicId.HasValue
                    && batchTopicIds.Contains(item.TopicId.Value))
                .Select(item => item.Topic!.TopicBatchId)
                .Distinct()
                .ToListAsync();

            var acceptedRequestBatchIds = await _context.SystemNotifications
                .Include(item => item.Topic)
                .Where(item =>
                    item.Type == "GroupJoinRequest"
                    && item.ActorUserId == studentId
                    && item.InvitationStatus == "Accepted"
                    && item.Topic != null
                    && item.TopicId.HasValue
                    && batchTopicIds.Contains(item.TopicId.Value))
                .Select(item => item.Topic!.TopicBatchId)
                .Distinct()
                .ToListAsync();

            var pendingRequestBatchIdSet = pendingRequestBatchIds.ToHashSet();
            var acceptedRequestBatchIdSet = acceptedRequestBatchIds.ToHashSet();

            var rankedGroups = batches
                .Select(batch => new
                {
                    Batch = batch,
                    Score = CalculateStudentGroupRelevance(batch, normalizedSearch)
                })
                .Where(item => string.IsNullOrWhiteSpace(normalizedSearch) || item.Score > 0)
                .OrderByDescending(item => string.IsNullOrWhiteSpace(normalizedSearch) ? 0 : item.Score)
                .ThenByDescending(item => item.Batch.CreatedAt)
                .ToList();

            model.StudentGroups = rankedGroups.Select(item =>
            {
                var batch = item.Batch;
                var topics = batch.Topics.ToList();
                var professor = batch.CreatedByProfessor;
                var selectedTopic = topics.FirstOrDefault(topic => topic.ReservedByStudentId == studentId);
                var takenTopicCount = topics.Count(IsTopicTaken);
                var isJoined = selectedTopic != null || acceptedRequestBatchIdSet.Contains(batch.Id);

                return new StudentGroupItem
                {
                    Id = batch.Id,
                    Title = batch.Title,
                    Description = batch.Description,
                    CreatedAt = batch.CreatedAt,
                    ProfessorName = DisplayName(professor?.Name, professor?.Email, "Professor"),
                    ProfessorEmail = professor?.Email ?? string.Empty,
                    ProfessorUniversityOrInstitution = professor?.UniversityOrInstitution,
                    ProfessorFieldOfStudy = professor?.FieldOfStudy,
                    ProfessorPhotoUrl = ResolvePhotoUrl(professor?.ProfilePhotoUrl, professor?.PhotoUrl),
                    TopicCount = topics.Count,
                    AvailableTopicCount = topics.Count - takenTopicCount,
                    FilledTopicCount = takenTopicCount,
                    IsJoinedByCurrentStudent = isJoined,
                    SelectedTopicTitle = selectedTopic?.Title,
                    HasPendingRequest = !isJoined && pendingRequestBatchIdSet.Contains(batch.Id)
                };
            }).ToList();

            return model;
        }

        private static int CalculateStudentGroupRelevance(TopicBatch batch, string search)
        {
            if (string.IsNullOrWhiteSpace(search))
                return 0;

            var normalizedSearch = search.Trim();
            var professor = batch.CreatedByProfessor;

            var score = 0;
            score += TextRelevance(batch.Title, normalizedSearch, 120, 90, 70);
            score += TextRelevance(batch.Description, normalizedSearch, 60, 45, 30);
            score += TextRelevance(professor?.Name, normalizedSearch, 110, 85, 65);
            score += TextRelevance(professor?.Email, normalizedSearch, 95, 70, 45);
            score += TextRelevance(professor?.UniversityOrInstitution, normalizedSearch, 70, 50, 35);
            score += TextRelevance(professor?.FieldOfStudy, normalizedSearch, 70, 50, 35);
            score += batch.Topics
                .Select(topic => TextRelevance(topic.Title, normalizedSearch, 50, 35, 20))
                .DefaultIfEmpty(0)
                .Max();

            return score;
        }

        private static int TextRelevance(string? value, string search, int exactScore, int startsWithScore, int containsScore)
        {
            if (string.IsNullOrWhiteSpace(value))
                return 0;

            if (string.Equals(value, search, StringComparison.OrdinalIgnoreCase))
                return exactScore;

            if (value.StartsWith(search, StringComparison.OrdinalIgnoreCase))
                return startsWithScore;

            return value.Contains(search, StringComparison.OrdinalIgnoreCase) ? containsScore : 0;
        }

        private static string DisplayName(string? name, string? email, string fallback)
        {
            if (!string.IsNullOrWhiteSpace(name))
                return name;

            if (!string.IsNullOrWhiteSpace(email))
                return email;

            return fallback;
        }

        private async Task AcceptPendingJoinRequestsAsync(int studentId, int professorId, List<int> topicIds, DateTime acceptedAt)
        {
            if (!topicIds.Any())
            {
                return;
            }

            var pendingRequests = await _context.SystemNotifications
                .Where(item =>
                    item.Type == "GroupJoinRequest"
                    && item.ActorUserId == studentId
                    && item.RecipientUserId == professorId
                    && item.InvitationStatus == "Pending"
                    && item.TopicId.HasValue
                    && topicIds.Contains(item.TopicId.Value))
                .ToListAsync();

            foreach (var request in pendingRequests)
            {
                request.InvitationStatus = "Accepted";
                request.RespondedAt = acceptedAt;
                request.Status = "Read";
                request.ReadAt ??= acceptedAt;
            }
        }

        private static bool IsTopicTaken(RealTimeCollaborationSystem.Models.Topic topic)
        {
            return topic.ReservedByStudentId.HasValue
                || string.Equals(topic.Status, "Taken", StringComparison.OrdinalIgnoreCase);
        }

        private static string ResolvePhotoUrl(string? profilePhotoUrl, string? photoUrl)
        {
            if (!string.IsNullOrWhiteSpace(profilePhotoUrl))
                return profilePhotoUrl;

            if (!string.IsNullOrWhiteSpace(photoUrl)
                && !string.Equals(photoUrl, "/images/users/default.png", StringComparison.OrdinalIgnoreCase))
                return photoUrl;

            return "/images/users/default-avatar.svg";
        }
    }
}
