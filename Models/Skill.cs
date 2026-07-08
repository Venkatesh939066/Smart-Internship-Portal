using System.Collections.Generic;

namespace SmartInternshipPortal.Models
{
    public class Skill
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public ICollection<UserSkill> UserSkills { get; set; } = new List<UserSkill>();
        public ICollection<InternshipSkill> InternshipSkills { get; set; } = new List<InternshipSkill>();
    }
}