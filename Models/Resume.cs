using System;

namespace SmartInternshipPortal.Models
{
    public class Resume
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;

        public string FilePath { get; set; } = string.Empty;

        public DateTime UploadedAt { get; set; } = DateTime.Now;
    }
}