using System.ComponentModel.DataAnnotations;

namespace RealTimeCollaborationSystem.ViewModels
{
    public class CreateCourseTaskViewModel
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public DateTime? Deadline { get; set; }

        [StringLength(500)]
        public string? MeetingLink { get; set; }

        [Required(ErrorMessage = "Choose a group.")]
        public int? TopicBatchId { get; set; }
    }
}
