using System.ComponentModel.DataAnnotations;

namespace RealTimeCollaborationSystem.Models.Dtos
{
    public class ForgotPasswordDto
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        public string Email { get; set; } = string.Empty;
    }
}
