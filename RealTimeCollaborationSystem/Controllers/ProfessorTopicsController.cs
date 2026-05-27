using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RealTimeCollaborationSystem.Data;
using RealTimeCollaborationSystem.Models;
using RealTimeCollaborationSystem.Services;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace RealTimeCollaborationSystem.Controllers
{
    public class ProfessorTopicsController : Controller
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

        private static readonly HashSet<string> AllowedTopicDocumentExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".txt",
            ".md",
            ".csv",
            ".rtf",
            ".docx"
        };

        private readonly AppDbContext _context;
        private readonly UploadStorageService _uploadStorage;

        public ProfessorTopicsController(AppDbContext context, UploadStorageService uploadStorage)
        {
            _context = context;
            _uploadStorage = uploadStorage;
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
                TempData["DashboardError"] = "Only professors can manage topic lists.";
                return RedirectToAction("Index", "Dashboard");
            }

            return null;
        }

        public async Task<IActionResult> Index(string? search, string? status)
        {
            if (RedirectIfNotProfessor() is IActionResult redirect)
                return redirect;

            if (!TryGetCurrentProfessorId(out var professorId))
                return RedirectToAction("Login", "Account");

            var query = _context.TopicBatches
                .Include(tb => tb.CreatedByProfessor)
                .Include(tb => tb.Topics)
                .Where(tb => tb.CreatedByProfessorId == professorId)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var normalizedSearch = search.Trim();
                query = query.Where(batch =>
                    batch.Title.Contains(normalizedSearch)
                    || batch.Topics.Any(topic => topic.Title.Contains(normalizedSearch)));
            }

            if (string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(batch => batch.IsPublished);
            }
            else if (string.Equals(status, "Draft", StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, "Pending", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(batch => !batch.IsPublished);
            }

            ViewBag.Search = search ?? string.Empty;
            ViewBag.Status = status ?? string.Empty;

            var batches = await query
                .OrderByDescending(tb => tb.CreatedAt)
                .ToListAsync();

            return View(batches);
        }

        [HttpGet]
        public async Task<IActionResult> BatchTopics(int id)
        {
            if (RedirectIfNotProfessor() is IActionResult redirect)
                return redirect;

            if (!TryGetCurrentProfessorId(out var professorId))
                return RedirectToAction("Login", "Account");

            var batch = await _context.TopicBatches
                .Include(tb => tb.CreatedByProfessor)
                .Include(tb => tb.Topics)
                    .ThenInclude(topic => topic.ReservedByStudent)
                .FirstOrDefaultAsync(tb => tb.Id == id && tb.CreatedByProfessorId == professorId);

            if (batch == null)
                return NotFound();

            ViewBag.TopicFiles = batch.Topics.ToDictionary(
                topic => topic.Id,
                topic => GetTopicFiles(topic.Id));

            return View(batch);
        }

        [HttpGet]
        public IActionResult CreateBatch()
        {
            if (RedirectIfNotProfessor() is IActionResult redirect)
                return redirect;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBatch(TopicBatch batch, string? topicsText)
        {
            if (RedirectIfNotProfessor() is IActionResult redirect)
                return redirect;

            var topicLines = ParseTopicLines(topicsText);

            if (topicLines.Count == 0)
                ModelState.AddModelError("TopicsText", "At least one topic is required.");

            if (topicLines.Any(topic => topic.Length > 200))
                ModelState.AddModelError("TopicsText", "Each topic must be 200 characters or fewer.");

            if (!ModelState.IsValid)
            {
                ViewBag.TopicsText = topicsText;
                return View(batch);
            }

            var userIdString = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userIdString))
                return RedirectToAction("Login", "Account");

            batch.Description = null;
            batch.CreatedByProfessorId = int.Parse(userIdString);
            batch.CreatedAt = DateTime.UtcNow;
            batch.IsPublished = false;

            foreach (var topicTitle in topicLines)
            {
                batch.Topics.Add(new Topic
                {
                    Title = topicTitle,
                    Status = "Available",
                    MaxMembers = 1
                });
            }

            _context.TopicBatches.Add(batch);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(BatchTopics), new { id = batch.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExtractTopicsFromDocument(IFormFile? topicDocument)
        {
            if (RedirectIfNotProfessor() is IActionResult redirect)
                return redirect;

            if (topicDocument == null || topicDocument.Length == 0)
                return BadRequest(new { message = "Upload a text document to extract topics." });

            if (topicDocument.Length > 2 * 1024 * 1024)
                return BadRequest(new { message = "Document must be 2 MB or smaller." });

            var extension = Path.GetExtension(topicDocument.FileName);

            if (!AllowedTopicDocumentExtensions.Contains(extension))
                return BadRequest(new { message = "Supported documents are TXT, MD, CSV, RTF, and DOCX." });

            var text = await ReadTopicDocumentTextAsync(topicDocument, extension);
            var topics = ParseTopicLines(text)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(100)
                .ToList();

            if (topics.Count == 0)
                return BadRequest(new { message = "No topics were detected in this document." });

            return Json(new
            {
                topics,
                message = $"{topics.Count} topic{(topics.Count == 1 ? "" : "s")} detected."
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditBatch(int id, string? title)
        {
            if (RedirectIfNotProfessor() is IActionResult redirect)
                return redirect;

            if (!TryGetCurrentProfessorId(out var professorId))
                return RedirectToAction("Login", "Account");

            var batch = await _context.TopicBatches
                .FirstOrDefaultAsync(item => item.Id == id && item.CreatedByProfessorId == professorId);

            if (batch == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(title) || title.Trim().Length > 200)
            {
                TempData["ProjectError"] = "Group name must be between 1 and 200 characters.";
                return RedirectToAction(nameof(Index));
            }

            batch.Title = title.Trim();
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActivateBatch(int id)
        {
            if (RedirectIfNotProfessor() is IActionResult redirect)
                return redirect;

            if (!TryGetCurrentProfessorId(out var professorId))
                return RedirectToAction("Login", "Account");

            var batch = await _context.TopicBatches
                .FirstOrDefaultAsync(item => item.Id == id && item.CreatedByProfessorId == professorId);

            if (batch == null)
                return NotFound();

            batch.IsPublished = true;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBatch(int id)
        {
            if (RedirectIfNotProfessor() is IActionResult redirect)
                return redirect;

            if (!TryGetCurrentProfessorId(out var professorId))
                return RedirectToAction("Login", "Account");

            var batch = await _context.TopicBatches
                .Include(item => item.Topics)
                .FirstOrDefaultAsync(item => item.Id == id && item.CreatedByProfessorId == professorId);

            if (batch == null)
                return NotFound();

            var topicIds = batch.Topics.Select(topic => topic.Id).ToList();
            var notifications = await _context.SystemNotifications
                .Where(item => item.TopicId.HasValue && topicIds.Contains(item.TopicId.Value))
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.TopicId = null;
            }

            var linkedTasks = await _context.CourseTasks
                .Where(task => task.TopicBatchId == batch.Id)
                .ToListAsync();

            foreach (var task in linkedTasks)
            {
                task.TopicBatchId = null;
            }

            _context.TopicBatches.Remove(batch);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTopic(int batchId, string? title, string? topicsText, string? topicAddMode, IFormFile? topicFile)
        {
            if (RedirectIfNotProfessor() is IActionResult redirect)
                return redirect;

            if (!TryGetCurrentProfessorId(out var professorId))
                return RedirectToAction("Login", "Account");

            var batchExists = await _context.TopicBatches
                .AnyAsync(item => item.Id == batchId && item.CreatedByProfessorId == professorId);

            if (!batchExists)
                return NotFound();

            var topicLines = ParseTopicLines(topicsText);

            if (!string.IsNullOrWhiteSpace(title))
                topicLines.Insert(0, CleanTopicLine(title));

            topicLines = topicLines
                .Where(topic => !string.IsNullOrWhiteSpace(topic))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (topicLines.Count == 0 || topicLines.Any(topic => topic.Length > 200))
            {
                TempData["ProjectError"] = "Topic names must be between 1 and 200 characters.";
                return RedirectToAction(nameof(BatchTopics), new { id = batchId });
            }

            var isSingleTopicMode = !string.Equals(topicAddMode, "multiple", StringComparison.OrdinalIgnoreCase);
            var hasSingleTopicFile = isSingleTopicMode && topicFile is { Length: > 0 };

            if (hasSingleTopicFile)
            {
                var fileError = ValidateAttachment(topicFile);

                if (!string.IsNullOrWhiteSpace(fileError))
                {
                    TempData["ProjectError"] = fileError;
                    return RedirectToAction(nameof(BatchTopics), new { id = batchId });
                }
            }

            Topic? topicForFile = null;

            foreach (var topicTitle in topicLines)
            {
                var topic = new Topic
                {
                    TopicBatchId = batchId,
                    Title = topicTitle,
                    Status = "Available",
                    MaxMembers = 1
                };

                _context.Topics.Add(topic);

                if (hasSingleTopicFile && topicLines.Count == 1)
                {
                    topicForFile = topic;
                }
            }

            await _context.SaveChangesAsync();

            if (topicForFile != null)
            {
                await SaveTopicFileAsync(topicForFile.Id, topicFile!);
            }

            return RedirectToAction(nameof(BatchTopics), new { id = batchId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTopic(int id, string? title)
        {
            if (RedirectIfNotProfessor() is IActionResult redirect)
                return redirect;

            if (!TryGetCurrentProfessorId(out var professorId))
                return RedirectToAction("Login", "Account");

            var topic = await _context.Topics
                .Include(item => item.TopicBatch)
                .FirstOrDefaultAsync(item =>
                    item.Id == id
                    && item.TopicBatch != null
                    && item.TopicBatch.CreatedByProfessorId == professorId);

            if (topic?.TopicBatch == null)
                return NotFound();

            if (IsTopicTaken(topic))
            {
                TempData["ProjectError"] = "Taken topics cannot be edited.";
                return RedirectToAction(nameof(BatchTopics), new { id = topic.TopicBatchId });
            }

            if (string.IsNullOrWhiteSpace(title) || title.Trim().Length > 200)
            {
                TempData["ProjectError"] = "Topic name must be between 1 and 200 characters.";
                return RedirectToAction(nameof(BatchTopics), new { id = topic.TopicBatchId });
            }

            topic.Title = title.Trim();
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(BatchTopics), new { id = topic.TopicBatchId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadTopicFile(int id, IFormFile? topicFile)
        {
            if (RedirectIfNotProfessor() is IActionResult redirect)
                return redirect;

            if (!TryGetCurrentProfessorId(out var professorId))
                return RedirectToAction("Login", "Account");

            var topic = await _context.Topics
                .Include(item => item.TopicBatch)
                .FirstOrDefaultAsync(item =>
                    item.Id == id
                    && item.TopicBatch != null
                    && item.TopicBatch.CreatedByProfessorId == professorId);

            if (topic?.TopicBatch == null)
                return NotFound();

            var fileError = ValidateAttachment(topicFile);

            if (!string.IsNullOrWhiteSpace(fileError))
            {
                TempData["ProjectError"] = fileError;
                return RedirectToAction(nameof(BatchTopics), new { id = topic.TopicBatchId });
            }

            await SaveTopicFileAsync(topic.Id, topicFile!);
            return RedirectToAction(nameof(BatchTopics), new { id = topic.TopicBatchId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTopic(int id)
        {
            if (RedirectIfNotProfessor() is IActionResult redirect)
                return redirect;

            if (!TryGetCurrentProfessorId(out var professorId))
                return RedirectToAction("Login", "Account");

            var topic = await _context.Topics
                .Include(item => item.TopicBatch)
                .FirstOrDefaultAsync(item =>
                    item.Id == id
                    && item.TopicBatch != null
                    && item.TopicBatch.CreatedByProfessorId == professorId);

            if (topic?.TopicBatch == null)
                return NotFound();

            var batchId = topic.TopicBatchId;

            if (IsTopicTaken(topic))
            {
                TempData["ProjectError"] = "Taken topics cannot be deleted.";
                return RedirectToAction(nameof(BatchTopics), new { id = batchId });
            }

            var notifications = await _context.SystemNotifications
                .Where(item => item.TopicId == topic.Id)
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.TopicId = null;
            }

            _context.Topics.Remove(topic);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(BatchTopics), new { id = batchId });
        }

        private static bool IsTopicTaken(Topic topic)
        {
            return topic.ReservedByStudentId.HasValue
                || string.Equals(topic.Status, "Taken", StringComparison.OrdinalIgnoreCase);
        }

        private static List<string> ParseTopicLines(string? topicsText)
        {
            return (topicsText ?? string.Empty)
                .Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None)
                .Select(CleanTopicLine)
                .Where(topic => !string.IsNullOrWhiteSpace(topic))
                .ToList();
        }

        private static string CleanTopicLine(string topic)
        {
            var cleanTopic = Regex.Replace(topic.Trim(), @"^\s*([-*•]|\d+[\).:-])\s*", string.Empty);
            cleanTopic = Regex.Replace(cleanTopic, @"\s+", " ").Trim(' ', '\t', '.', ';');
            return cleanTopic.Length > 200 ? cleanTopic[..200].Trim() : cleanTopic;
        }

        private static string? ValidateAttachment(IFormFile? attachment)
        {
            if (attachment == null || attachment.Length == 0)
                return "Choose a file before uploading.";

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
            var uploadsFolder = _uploadStorage.GetUploadsDirectory("topics");

            var originalFileName = Path.GetFileName(attachment.FileName);
            var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
            var storedFileName = $"{Guid.NewGuid():N}{extension}";
            var filePath = Path.Combine(uploadsFolder, storedFileName);

            await using var stream = System.IO.File.Create(filePath);
            await attachment.CopyToAsync(stream);

            return ($"/uploads/topics/{storedFileName}", originalFileName);
        }

        private async Task SaveTopicFileAsync(int topicId, IFormFile topicFile)
        {
            var uploadsFolder = _uploadStorage.GetUploadsDirectory("topic-files", topicId.ToString());

            var originalFileName = Path.GetFileName(topicFile.FileName);
            var safeFileName = string.Join("_", originalFileName.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
            var storedFileName = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}_{safeFileName}";
            var filePath = Path.Combine(uploadsFolder, storedFileName);

            await using var stream = System.IO.File.Create(filePath);
            await topicFile.CopyToAsync(stream);
        }

        private List<TopicFileView> GetTopicFiles(int topicId)
        {
            var uploadsFolder = _uploadStorage.GetUploadsPath("topic-files", topicId.ToString());

            if (!Directory.Exists(uploadsFolder))
                return new List<TopicFileView>();

            return Directory.GetFiles(uploadsFolder)
                .OrderByDescending(System.IO.File.GetLastWriteTimeUtc)
                .Select(path =>
                {
                    var storedFileName = Path.GetFileName(path);
                    var displayName = storedFileName.Length > 18 && storedFileName[17] == '_'
                        ? storedFileName[18..]
                        : storedFileName;

                    return new TopicFileView
                    {
                        FileName = displayName,
                        Url = $"/uploads/topic-files/{topicId}/{storedFileName}"
                    };
                })
                .ToList();
        }

        private static async Task<string> ReadTopicDocumentTextAsync(IFormFile document, string extension)
        {
            if (string.Equals(extension, ".docx", StringComparison.OrdinalIgnoreCase))
                return await ReadDocxTextAsync(document);

            using var reader = new StreamReader(document.OpenReadStream(), Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            var text = await reader.ReadToEndAsync();

            if (string.Equals(extension, ".rtf", StringComparison.OrdinalIgnoreCase))
            {
                text = Regex.Replace(text, @"\\'[0-9a-fA-F]{2}", " ");
                text = Regex.Replace(text, @"\\[a-zA-Z]+\d* ?", " ");
                text = Regex.Replace(text, @"[{}]", " ");
            }
            else if (string.Equals(extension, ".csv", StringComparison.OrdinalIgnoreCase))
            {
                text = text.Replace(",", "\n").Replace(";", "\n");
            }

            return text;
        }

        private static async Task<string> ReadDocxTextAsync(IFormFile document)
        {
            using var archive = new ZipArchive(document.OpenReadStream(), ZipArchiveMode.Read);
            var entry = archive.GetEntry("word/document.xml");

            if (entry == null)
                return string.Empty;

            await using var stream = entry.Open();
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var xml = await reader.ReadToEndAsync();

            xml = Regex.Replace(xml, @"</w:p>", "\n", RegexOptions.IgnoreCase);
            xml = Regex.Replace(xml, "<[^>]+>", " ");

            return WebUtility.HtmlDecode(xml);
        }

        public class TopicFileView
        {
            public string FileName { get; set; } = string.Empty;
            public string Url { get; set; } = string.Empty;
        }
    }
}
