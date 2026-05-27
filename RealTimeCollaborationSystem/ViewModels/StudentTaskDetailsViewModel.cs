using RealTimeCollaborationSystem.Models;

namespace RealTimeCollaborationSystem.ViewModels
{
    public class StudentTaskDetailsViewModel
    {
        public CourseTask Task { get; set; } = new();
        public TaskSubmission? Submission { get; set; }
    }
}
