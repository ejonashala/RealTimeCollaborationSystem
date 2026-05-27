using System.ComponentModel.DataAnnotations;

namespace RealTimeCollaborationSystem.Models
{
    public class Topic
    {
        public int Id { get; set; }

        public int TopicBatchId { get; set; }
        public TopicBatch? TopicBatch { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        public int MaxMembers { get; set; } = 1;

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Available";

        public int? ReservedByStudentId { get; set; }
        public User? ReservedByStudent { get; set; }

        public DateTime? ReservedAt { get; set; }
    }
}