using Microsoft.AspNetCore.Mvc.Rendering;
using RealTimeCollaborationSystem.Models;

namespace RealTimeCollaborationSystem.ViewModels
{
    public class NotificationsViewModel
    {
        public bool IsProfessor { get; set; }
        public List<NotificationItemViewModel> Notifications { get; set; } = new();
        public List<SystemNotification> SentInvitations { get; set; } = new();
        public List<SelectListItem> Students { get; set; } = new();
        public List<SelectListItem> Topics { get; set; } = new();
    }

    public class NotificationItemViewModel
    {
        public int? Id { get; set; }
        public string Type { get; set; } = "System";
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Status { get; set; } = "Unread";
        public string? InvitationStatus { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool RequiresResponse { get; set; }
        public string NavigationUrl { get; set; } = "/Notifications";
    }
}
