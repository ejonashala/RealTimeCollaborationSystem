using System.ComponentModel.DataAnnotations;

namespace RealTimeCollaborationSystem.Models
{
    public class CourseTask
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        public DateTime Deadline { get; set; }

        [StringLength(500)]
        public string? MeetingLink { get; set; }

        [StringLength(500)]
        public string? AttachmentPath { get; set; }

        [StringLength(255)]
        public string? AttachmentFileName { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int ProfessorId { get; set; }
        public User? Professor { get; set; }

        public int? TopicBatchId { get; set; }
        public TopicBatch? TopicBatch { get; set; }

        public ICollection<TaskSubmission> Submissions { get; set; } = new List<TaskSubmission>();
    }
}
