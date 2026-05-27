using System.ComponentModel.DataAnnotations;

namespace RealTimeCollaborationSystem.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Email është i detyrueshëm.")]
        [EmailAddress(ErrorMessage = "Email nuk është valide")]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password është i detyrueshëm.")]
        [StringLength(225, MinimumLength = 6, ErrorMessage = "Passwordi duhet të ketë së paku 6 karaktere")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Emri është i detyrueshëm.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Emri duhet të ketë të paktën 2 karaktere.")]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = "Student";

        [StringLength(150, ErrorMessage = "Institucioni nuk mund të ketë më shumë se 150 karaktere.")]
        public string? UniversityOrInstitution { get; set; }

        [StringLength(150, ErrorMessage = "Fusha e studimit nuk mund të ketë më shumë se 150 karaktere.")]
        public string? FieldOfStudy { get; set; }

        public string? PhotoUrl { get; set; }
        public string? ProfilePhotoUrl { get; set; }

        public string? PasswordResetToken { get; set; }
        public DateTime? PasswordResetTokenExpiry { get; set; }
        public string Language { get; set; } = "sq";

    }
}
      


