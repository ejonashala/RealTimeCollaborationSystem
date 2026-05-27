namespace RealTimeCollaborationSystem.ViewModels
{
    public class ProfessorGroupsViewModel
    {
        public bool IsProfessor { get; set; }
        public bool IsStudent { get; set; }
        public string Search { get; set; } = string.Empty;
        public List<ProfessorGroupBatchItem> Batches { get; set; } = new();
        public List<ProfessorGroupStudentSearchItem> SearchResults { get; set; } = new();
        public List<ProfessorGroupOption> AvailableGroupOptions { get; set; } = new();
        public List<ProfessorGroupJoinRequestItem> PendingJoinRequests { get; set; } = new();
        public List<StudentGroupItem> StudentGroups { get; set; } = new();
    }

    public class ProfessorGroupBatchItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public bool IsPublished { get; set; }
        public int TopicCount { get; set; }
        public int AvailableTopicCount { get; set; }
        public int TakenTopicCount { get; set; }
        public List<ProfessorGroupMemberItem> Members { get; set; } = new();
    }

    public class ProfessorGroupMemberItem
    {
        public int StudentId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? UniversityOrInstitution { get; set; }
        public string? FieldOfStudy { get; set; }
        public string PhotoUrl { get; set; } = "/images/users/default-avatar.svg";
        public DateTime? JoinedAt { get; set; }
    }

    public class ProfessorGroupStudentSearchItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? UniversityOrInstitution { get; set; }
        public string? FieldOfStudy { get; set; }
        public string PhotoUrl { get; set; } = "/images/users/default-avatar.svg";
    }

    public class ProfessorGroupOption
    {
        public int BatchId { get; set; }
        public string BatchTitle { get; set; } = string.Empty;
        public int AvailableSlots { get; set; }
    }

    public class ProfessorGroupJoinRequestItem
    {
        public int NotificationId { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentEmail { get; set; } = string.Empty;
        public int BatchId { get; set; }
        public string BatchTitle { get; set; } = string.Empty;
        public DateTime? RequestedAt { get; set; }
    }

    public class StudentGroupItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public string ProfessorName { get; set; } = string.Empty;
        public string ProfessorEmail { get; set; } = string.Empty;
        public string? ProfessorUniversityOrInstitution { get; set; }
        public string? ProfessorFieldOfStudy { get; set; }
        public string ProfessorPhotoUrl { get; set; } = "/images/users/default-avatar.svg";
        public int TopicCount { get; set; }
        public int AvailableTopicCount { get; set; }
        public int FilledTopicCount { get; set; }
        public bool IsJoinedByCurrentStudent { get; set; }
        public string? SelectedTopicTitle { get; set; }
        public bool HasPendingRequest { get; set; }
    }
}
