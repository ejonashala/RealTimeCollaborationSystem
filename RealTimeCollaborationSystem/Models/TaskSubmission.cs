using System.ComponentModel.DataAnnotations;

namespace RealTimeCollaborationSystem.Models
{
    public class TaskSubmission
    {
        public int Id { get; set; }

        public int CourseTaskId { get; set; }
        public CourseTask? CourseTask { get; set; }

        public int StudentId { get; set; }
        public User? Student { get; set; }

        [Required]
        [StringLength(500)]
        public string FilePath { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string FileName { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? StudentComment { get; set; }

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Submitted";

        [StringLength(1000)]
        public string? Feedback { get; set; }

        public decimal? Grade { get; set; }

        public DateTime? ReviewedAt { get; set; }
    }
}
