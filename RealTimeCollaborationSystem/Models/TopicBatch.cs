using System.ComponentModel.DataAnnotations;

namespace RealTimeCollaborationSystem.Models
{
    public class TopicBatch
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        public int CreatedByProfessorId { get; set; }
        public User? CreatedByProfessor { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsPublished { get; set; } = false;

        [StringLength(500)]
        public string? AttachmentPath { get; set; }

        [StringLength(255)]
        public string? AttachmentFileName { get; set; }

        public ICollection<Topic> Topics { get; set; } = new List<Topic>();
    }
}
