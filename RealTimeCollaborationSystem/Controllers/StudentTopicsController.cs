using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RealTimeCollaborationSystem.Data;
using RealTimeCollaborationSystem.Hubs;
using RealTimeCollaborationSystem.Models;

namespace RealTimeCollaborationSystem.Controllers
{
    public class StudentTopicsController : Controller
    {
        private const string AlreadySelectedTopicMessage = "Keni zgjedhur tashm\u00eb nj\u00eb tem\u00eb n\u00eb k\u00ebt\u00eb grup. N\u00ebse doni t\u00eb selektoni tjet\u00ebr tem\u00eb, hiqni tem\u00ebn e selektuar.";
        private readonly AppDbContext _context;
        private readonly IHubContext<CollaborationHub> _hubContext;

        public StudentTopicsController(AppDbContext context, IHubContext<CollaborationHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
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
                TempData["DashboardError"] = "Only students can choose a topic.";
                return RedirectToAction("Index", "Dashboard");
            }

            return null;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (RedirectIfNotStudent() is IActionResult redirect)
                return redirect;

            var userIdString = HttpContext.Session.GetString("UserId");

            if (!int.TryParse(userIdString, out var studentId))
                return RedirectToAction("Login", "Account");

            var selectedTopicBatchIds = await _context.Topics
                .Where(topic => topic.ReservedByStudentId == studentId)
                .Select(topic => topic.TopicBatchId)
                .Distinct()
                .ToListAsync();

            var pendingRequestBatchIds = await _context.SystemNotifications
                .Include(notification => notification.Topic)
                .Where(notification =>
                    notification.Type == "GroupJoinRequest"
                    && notification.ActorUserId == studentId
                    && notification.InvitationStatus == "Pending"
                    && notification.Topic != null)
                .Select(notification => notification.Topic!.TopicBatchId)
                .Distinct()
                .ToListAsync();

            var acceptedRequestBatchIds = await _context.SystemNotifications
                .Include(notification => notification.Topic)
                .Where(notification =>
                    notification.InvitationStatus == "Accepted"
                    && notification.Topic != null
                    && ((notification.Type == "GroupJoinRequest" && notification.ActorUserId == studentId)
                        || (notification.Type == "GroupInvitation" && notification.RecipientUserId == studentId)))
                .Select(notification => notification.Topic!.TopicBatchId)
                .Distinct()
                .ToListAsync();

            var acceptedBatchIds = acceptedRequestBatchIds;

            var visibleBatchIds = selectedTopicBatchIds
                .Concat(pendingRequestBatchIds)
                .Concat(acceptedBatchIds)
                .Distinct()
                .ToList();

            var batches = await _context.TopicBatches
                .Include(batch => batch.Topics)
                .Include(batch => batch.CreatedByProfessor)
                .Where(batch => batch.IsPublished && visibleBatchIds.Contains(batch.Id))
                .OrderByDescending(batch => batch.CreatedAt)
                .AsNoTracking()
                .ToListAsync();

            ViewBag.CurrentStudentId = studentId;
            ViewBag.PendingRequestBatchIds = pendingRequestBatchIds.ToHashSet();
            ViewBag.AcceptedBatchIds = acceptedBatchIds.ToHashSet();

            return View(batches);
        }

        [HttpGet]
        public async Task<IActionResult> MyTopic()
        {
            if (RedirectIfNotStudent() is IActionResult redirect)
                return redirect;

            var userIdString = HttpContext.Session.GetString("UserId");

            if (!int.TryParse(userIdString, out var studentId))
                return RedirectToAction("Login", "Account");

            var topics = await _context.Topics
                .Include(topic => topic.TopicBatch)
                    .ThenInclude(batch => batch!.CreatedByProfessor)
                .Where(topic => topic.ReservedByStudentId == studentId)
                .OrderByDescending(topic => topic.ReservedAt)
                .AsNoTracking()
                .ToListAsync();

            return View(topics);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SelectTopic(int id)
        {
            if (!IsStudent())
                return Unauthorized(new { message = "Please sign in as a student." });

            var userIdString = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrWhiteSpace(userIdString) || !int.TryParse(userIdString, out var studentId))
                return Unauthorized(new { message = "Please sign in again." });

            var topic = await _context.Topics
                .Include(item => item.TopicBatch)
                .FirstOrDefaultAsync(item => item.Id == id);

            if (topic == null)
                return NotFound(new { message = "Topic was not found." });

            if (topic.TopicBatch == null || !topic.TopicBatch.IsPublished)
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "This topic is not available for selection." });

            var existingTopic = await _context.Topics
                .AsNoTracking()
                .FirstOrDefaultAsync(item =>
                    item.ReservedByStudentId == studentId
                    && item.TopicBatchId == topic.TopicBatchId
                    && item.Id != topic.Id);

            if (existingTopic != null)
            {
                return Conflict(new
                {
                    code = "AlreadySelectedTopic",
                    message = AlreadySelectedTopicMessage,
                    topicId = existingTopic.Id,
                    batchId = existingTopic.TopicBatchId,
                    status = "Taken"
                });
            }

            if (topic.ReservedByStudentId.HasValue || string.Equals(topic.Status, "Taken", StringComparison.OrdinalIgnoreCase))
            {
                return Conflict(new
                {
                    code = "TopicTaken",
                    message = "This topic has already been taken.",
                    topicId = topic.Id,
                    batchId = topic.TopicBatchId,
                    status = "Taken"
                });
            }

            topic.Status = "Taken";
            topic.ReservedByStudentId = studentId;
            topic.ReservedAt = DateTime.UtcNow;

            if (topic.TopicBatch != null)
            {
                _context.SystemNotifications.Add(new SystemNotification
                {
                    RecipientUserId = topic.TopicBatch.CreatedByProfessorId,
                    ActorUserId = studentId,
                    Type = "TopicSelected",
                    Title = "Topic selected",
                    Message = $"{topic.Title} was selected from {topic.TopicBatch.Title}.",
                    TopicId = topic.Id,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();

            await _hubContext.Clients.All.SendAsync("TopicTaken", topic.Id);
            await _hubContext.Clients.All.SendAsync("NotificationsUpdated");

            return Ok(new
            {
                topicId = topic.Id,
                batchId = topic.TopicBatchId,
                status = "Taken",
                message = "Topic selected successfully."
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReleaseTopic(int id)
        {
            if (!IsStudent())
                return Unauthorized(new { message = "Please sign in as a student." });

            var userIdString = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrWhiteSpace(userIdString) || !int.TryParse(userIdString, out var studentId))
                return Unauthorized(new { message = "Please sign in again." });

            var topic = await _context.Topics
                .Include(item => item.TopicBatch)
                .FirstOrDefaultAsync(item => item.Id == id);

            if (topic == null)
                return NotFound(new { message = "Topic was not found." });

            if (topic.TopicBatch == null || !topic.TopicBatch.IsPublished)
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "This topic is not available." });

            var isTaken = topic.ReservedByStudentId.HasValue
                || string.Equals(topic.Status, "Taken", StringComparison.OrdinalIgnoreCase);

            if (!isTaken)
            {
                return Conflict(new
                {
                    code = "TopicAvailable",
                    message = "This topic is already available.",
                    topicId = topic.Id,
                    status = "Available"
                });
            }

            if (topic.ReservedByStudentId != studentId)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    code = "NotYourTopic",
                    message = "You can release only the topic selected by your account.",
                    topicId = topic.Id,
                    status = "Taken"
                });
            }

            topic.Status = "Available";
            topic.ReservedByStudentId = null;
            topic.ReservedAt = null;

            if (topic.TopicBatch != null)
            {
                _context.SystemNotifications.Add(new SystemNotification
                {
                    RecipientUserId = topic.TopicBatch.CreatedByProfessorId,
                    ActorUserId = studentId,
                    Type = "TopicReleased",
                    Title = "Topic released",
                    Message = $"{topic.Title} was released in {topic.TopicBatch.Title}.",
                    TopicId = topic.Id,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();

            await _hubContext.Clients.All.SendAsync("TopicAvailable", topic.Id);
            await _hubContext.Clients.All.SendAsync("NotificationsUpdated");

            return Ok(new
            {
                topicId = topic.Id,
                batchId = topic.TopicBatchId,
                status = "Available",
                message = "Topic released. You can select another topic."
            });
        }
    }
}
