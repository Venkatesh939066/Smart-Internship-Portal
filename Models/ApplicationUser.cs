using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace SmartInternshipPortal.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;

        // ===========================
        // Student Profile
        // ===========================

        public string? ProfessionalTitle { get; set; }

        public string? About { get; set; }

        public string? Location { get; set; }

        public string? PhoneNumber2 { get; set; }

        public string? ProfileImage { get; set; }

        public string ResumePath { get; set; } = string.Empty;

        // ===========================
        // Education
        // ===========================

        public string College { get; set; } = string.Empty;

        public string Degree { get; set; } = string.Empty;

        public string StartYear { get; set; } = string.Empty;

        public string EndYear { get; set; } = string.Empty;

        public string EducationPercentage { get; set; } = string.Empty;

        public string Certifications { get; set; } = string.Empty;

        public string? CertificateImage { get; set; }

        public string ProjectTitle { get; set; } = string.Empty;

        public string ProjectDescription { get; set; } = string.Empty;

        // ===========================
        // Experience
        // ===========================

        public string CompanyName { get; set; } = string.Empty;

        public string Position { get; set; } = string.Empty;

        public string ExperienceDescription { get; set; } = string.Empty;

        // ===========================
        // Social Links
        // ===========================

        public string LinkedInUrl { get; set; } = string.Empty;

        public string GitHubUrl { get; set; } = string.Empty;

        public string PortfolioUrl { get; set; } = string.Empty;

        // ===========================
        // Preferences
        // ===========================

        public string PreferredRole { get; set; } = string.Empty;

        public string PreferredLocation { get; set; } = string.Empty;

        public string WorkMode { get; set; } = string.Empty;

        // ===========================
        // Company Profile
        // ===========================

        public string CompanyDescription { get; set; } = string.Empty;

        public string CompanyLogo { get; set; } = string.Empty;

        public string CompanyWebsite { get; set; } = string.Empty;

        public string CompanyIndustry { get; set; } = string.Empty;

        public bool ReceiveInternshipEmails { get; set; } = true;

        public ICollection<UserSkill> UserSkills { get; set; }
            = new List<UserSkill>();
    }
}