using RealTimeCollaborationSystem.Models;

namespace RealTimeCollaborationSystem.ViewModels
{
    public class ProfessorTaskSubmissionsViewModel
    {
        public CourseTask Task { get; set; } = new();
        public List<TaskSubmission> Submissions { get; set; } = new();
    }
}
