using System;

namespace SmartInternshipPortal.Models
{
    public class Application
    {
        public int Id { get; set; }
        public string StudentId { get; set; } = string.Empty;
        public ApplicationUser Student { get; set; } = null!;
        public int InternshipId { get; set; }
        public Internship Internship { get; set; } = null!;
        public DateTime AppliedDate { get; set; } = DateTime.Now;
        public string Status { get; set; } = "Pending"; // Pending, Accepted, Rejected
        public string? CompanyFeedback { get; set; }

    }
}