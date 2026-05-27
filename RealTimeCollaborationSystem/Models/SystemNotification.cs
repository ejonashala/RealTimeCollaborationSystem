using System.ComponentModel.DataAnnotations;

namespace RealTimeCollaborationSystem.Models
{
    public class SystemNotification
    {
        public int Id { get; set; }

        public int RecipientUserId { get; set; }
        public User? RecipientUser { get; set; }

        public int? ActorUserId { get; set; }
        public User? ActorUser { get; set; }

        [Required]
        [StringLength(50)]
        public string Type { get; set; } = "System";

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string Message { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Unread";

        [StringLength(50)]
        public string? InvitationStatus { get; set; }

        public int? TopicId { get; set; }
        public Topic? Topic { get; set; }

        public int? CourseTaskId { get; set; }
        public CourseTask? CourseTask { get; set; }

        public int? TaskSubmissionId { get; set; }
        public TaskSubmission? TaskSubmission { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReadAt { get; set; }
        public DateTime? RespondedAt { get; set; }
    }
}
