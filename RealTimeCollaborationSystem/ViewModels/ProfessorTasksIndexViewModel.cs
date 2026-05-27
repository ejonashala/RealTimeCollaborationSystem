using RealTimeCollaborationSystem.Models;

namespace RealTimeCollaborationSystem.ViewModels
{
    public class ProfessorTasksIndexViewModel
    {
        public List<CourseTask> Tasks { get; set; } = new();
        public CreateCourseTaskViewModel NewTask { get; set; } = new();
        public List<TopicBatch> Groups { get; set; } = new();
        public Dictionary<int, ProfessorTaskCompletionState> CompletionStates { get; set; } = new();
    }

    public class ProfessorTaskCompletionState
    {
        public int RequiredSubmissions { get; set; }
        public int SubmittedRequiredMembers { get; set; }
        public int TotalSubmissions { get; set; }
        public bool IsComplete => RequiredSubmissions > 0 && SubmittedRequiredMembers >= RequiredSubmissions;
        public string ProgressText => RequiredSubmissions > 0
            ? $"{SubmittedRequiredMembers}/{RequiredSubmissions}"
            : TotalSubmissions.ToString();
    }
}
