using System;
using System.Collections.Generic;

namespace SmartInternshipPortal.Models
{
    public class Internship
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string CompanyId { get; set; } = string.Empty;

        public ApplicationUser Company { get; set; } = null!;

        public string Location { get; set; } = string.Empty;

        public DateTime PostedDate { get; set; } = DateTime.Now;

        // Real-world fields

        public string Salary { get; set; } = string.Empty;

        public string JobType { get; set; } = string.Empty;

        public string RemoteType { get; set; } = string.Empty;

        public string? ExperienceLevel { get; set; }

        public DateTime Deadline { get; set; } = DateTime.Now.AddMonths(1);

        public string CompanyLogo { get; set; } = string.Empty;

        public string ApplyUrl { get; set; } = string.Empty;

        public ICollection<InternshipSkill> InternshipSkills { get; set; }
            = new List<InternshipSkill>();

        public ICollection<Application> Applications { get; set; }
            = new List<Application>();
    }
}