namespace RealTimeCollaborationSystem.ViewModels
{
    public class DashboardViewModel
    {
        public string Role { get; set; } = "Student";
        public bool IsProfessor => Role == "Professor";
        public List<DashboardStatItem> Stats { get; set; } = new();
        public List<DashboardDeadlineItem> UpcomingDeadlines { get; set; } = new();
        public List<DashboardActivityItem> RecentActivity { get; set; } = new();
        public List<DashboardQuickAction> QuickActions { get; set; } = new();
        public List<DashboardTopicItem> SelectedTopics { get; set; } = new();
    }

    public class DashboardStatItem
    {
        public string Label { get; set; } = string.Empty;
        public string Value { get; set; } = "0";
        public string Hint { get; set; } = string.Empty;
        public string Tone { get; set; } = "neutral";
    }

    public class DashboardDeadlineItem
    {
        public string Title { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
        public DateTime DueAt { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class DashboardActivityItem
    {
        public string Title { get; set; } = string.Empty;
        public string Detail { get; set; } = string.Empty;
        public DateTime OccurredAt { get; set; }
        public string Tone { get; set; } = "neutral";
    }

    public class DashboardQuickAction
    {
        public string Label { get; set; } = string.Empty;
        public string Controller { get; set; } = string.Empty;
        public string Action { get; set; } = "Index";
        public bool IsPrimary { get; set; }
    }

    public class DashboardTopicItem
    {
        public string Title { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
        public string ProfessorName { get; set; } = string.Empty;
        public DateTime? SelectedAt { get; set; }
    }
}
