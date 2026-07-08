using System;

namespace SmartInternshipPortal.Models
{
    public class SavedJob
    {
        public int Id { get; set; }

        public string StudentId { get; set; } = string.Empty;

        public int InternshipId { get; set; }

        public Internship Internship { get; set; } = null!;
        
        // Extra fields: notes, tags and timestamp
        public string Notes { get; set; } = string.Empty;

        public string Tags { get; set; } = string.Empty;

        public DateTime SavedAt { get; set; } = DateTime.Now;
    }
}