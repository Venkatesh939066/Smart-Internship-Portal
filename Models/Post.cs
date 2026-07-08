using System;
using System.Collections.Generic;

namespace SmartInternshipPortal.Models
{
    public class Post
    {
        public int Id { get; set; }

        public string AuthorId { get; set; } = string.Empty;

        public ApplicationUser Author { get; set; } = null!;

        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public string? ImageUrl { get; set; }

        public int LikesCount { get; set; }

        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    }
}
