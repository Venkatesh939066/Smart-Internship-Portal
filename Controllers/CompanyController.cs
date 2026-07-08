using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartInternshipPortal.Models;
using SmartInternshipPortal.Services;

namespace SmartInternshipPortal.Controllers
{
    [Authorize]
    public class CompanyController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;

        public CompanyController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IEmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
        }

        // GET: Company
        public async Task<IActionResult> Index()
        {
            var company = await _userManager.GetUserAsync(User);

            if (company == null)
                return Challenge();

            var internships = await _context.Internships
                .Where(i => i.CompanyId == company.Id)
                .Include(i => i.InternshipSkills)
                    .ThenInclude(s => s.Skill)
                .Include(i => i.Applications)
                .OrderByDescending(i => i.PostedDate)
                .ToListAsync();

            ViewBag.TotalJobs = internships.Count;

            ViewBag.TotalApplications =
                internships.Sum(i => i.Applications.Count);

            ViewBag.PendingReviews =
                internships.Sum(i =>
                    i.Applications.Count(a => a.Status == "Pending"));

            return View(internships);
        }

        // GET: Company/Profile
        public async Task<IActionResult> Profile()
        {
            var company = await _userManager.GetUserAsync(User);

            if (company == null)
                return Challenge();

            return View(company);
        }

        // POST: Company/Profile
        [HttpPost]
        public async Task<IActionResult> Profile(
            string fullName,
            string companyIndustry,
            string companyWebsite,
            string companyDescription)
        {
            var company = await _userManager.GetUserAsync(User);

            if (company == null)
                return Challenge();

            company.FullName = fullName ?? "";
            company.CompanyIndustry = companyIndustry ?? "";
            company.CompanyWebsite = companyWebsite ?? "";
            company.CompanyDescription =
                string.IsNullOrWhiteSpace(companyDescription)
                  ? ""
                  : companyDescription;

            await _userManager.UpdateAsync(company);

            TempData["SuccessMessage"] =
                "Company profile updated successfully!";
            
            return RedirectToAction(nameof(Profile));
        }

        // PASTE THE CREATE METHODS HERE

        // GET: Company/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.AllSkills = await _context.Skills.ToListAsync();
            return View();
        }

        // POST: Company/Create
        [HttpPost]
        public async Task<IActionResult> Create(
            string title,
            string description,
            string location,
            string salary,
            string jobType,
            string remoteType,
            string experienceLevel,
            string applyUrl,
            DateTime deadline,
            List<int> selectedSkillIds)
        {
            var company = await _userManager.GetUserAsync(User);

            if (company == null)
                return Challenge();
                TempData["Debug"] = "Experience = " + experienceLevel;
                TempData["Debug"] += "\nTITLE = " + title;
                TempData["Debug"] += "\nSALARY = " + salary;
                TempData["Debug"] += "\nJOBTYPE = " + jobType;
                TempData["Debug"] += "\nREMOTE = " + remoteType;
            var internship = new Internship
{
    Title = title,
    Description = description,
    Location = location,

    Salary = string.IsNullOrWhiteSpace(salary)
        ? "Not Specified"
        : salary,

    JobType = string.IsNullOrWhiteSpace(jobType)
        ? "Internship"
        : jobType,

    RemoteType = string.IsNullOrWhiteSpace(remoteType)
        ? "Onsite"
        : remoteType,

    ExperienceLevel = string.IsNullOrWhiteSpace(experienceLevel)
        ? "Fresher"
        : experienceLevel,

    ApplyUrl = string.IsNullOrWhiteSpace(applyUrl)
        ? "#"
        : applyUrl,

    Deadline = deadline == DateTime.MinValue
        ? DateTime.Now.AddMonths(1)
        : deadline,

    CompanyId = company.Id,
    PostedDate = DateTime.Now
};

            _context.Internships.Add(internship);
            await _context.SaveChangesAsync();

            if (selectedSkillIds != null)
            {
                foreach (var skillId in selectedSkillIds)
                {
                    _context.InternshipSkills.Add(
                        new InternshipSkill
                        {
                            InternshipId = internship.Id,
                            SkillId = skillId
                        });
                }

                await _context.SaveChangesAsync();
            }

            // Automatically post to Community Feed
            var feedPost = new Post
            {
                AuthorId = company.Id,
                Content = $"🚀 We are hiring! We've just posted a new internship role: {internship.Title}.\n\n📍 Location: {internship.Location}\n💼 Type: {internship.JobType} ({internship.RemoteType})\n💰 Stipend: {internship.Salary}\n⏳ Deadline: {internship.Deadline.ToString("MMM dd, yyyy")}\n\nApply now directly on the portal!",
                CreatedAt = DateTime.Now,
                LikesCount = 0
            };
            _context.Posts.Add(feedPost);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Internship posted successfully!";

            await SendNewInternshipNotificationAsync(internship, company.FullName, selectedSkillIds);

            return RedirectToAction(nameof(Index));
        }

        // GET: Company/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var company = await _userManager.GetUserAsync(User);
            if (company == null)
                return Challenge();

            var internship = await _context.Internships
                .Include(i => i.InternshipSkills)
                .FirstOrDefaultAsync(i => i.Id == id && i.CompanyId == company.Id);

            if (internship == null)
                return NotFound();

            ViewBag.AllSkills = await _context.Skills.ToListAsync();
            ViewBag.SelectedSkillIds = internship.InternshipSkills
                .Select(isk => isk.SkillId)
                .ToList();

            return View(internship);
        }

        // POST: Company/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            string title,
            string description,
            string location,
            string salary,
            string jobType,
            string remoteType,
            string experienceLevel,
            string applyUrl,
            DateTime? deadline,
            List<int>? selectedSkillIds)
        {
            var company = await _userManager.GetUserAsync(User);
            if (company == null)
                return Challenge();

            var internship = await _context.Internships
                .Include(i => i.InternshipSkills)
                .FirstOrDefaultAsync(i => i.Id == id && i.CompanyId == company.Id);

            if (internship == null)
                return NotFound();

            internship.Title = title ?? internship.Title;
            internship.Description = description ?? internship.Description;
            internship.Location = location ?? internship.Location;
            internship.Salary = string.IsNullOrWhiteSpace(salary)
                ? "Not Specified"
                : salary;
            internship.JobType = string.IsNullOrWhiteSpace(jobType)
                ? "Internship"
                : jobType;
            internship.RemoteType = string.IsNullOrWhiteSpace(remoteType)
                ? "Onsite"
                : remoteType;
            internship.ExperienceLevel = string.IsNullOrWhiteSpace(experienceLevel)
                ? internship.ExperienceLevel ?? "Fresher"
                : experienceLevel;
            internship.ApplyUrl = string.IsNullOrWhiteSpace(applyUrl)
                ? internship.ApplyUrl
                : applyUrl;
            internship.Deadline = deadline ?? internship.Deadline;

            _context.Internships.Update(internship);

            var existingSkillIds = internship.InternshipSkills
                .Select(isk => isk.SkillId)
                .ToList();
            selectedSkillIds = selectedSkillIds ?? new List<int>();

            var skillsToAdd = selectedSkillIds.Except(existingSkillIds).ToList();
            var skillsToRemove = internship.InternshipSkills
                .Where(isk => !selectedSkillIds.Contains(isk.SkillId))
                .ToList();

            foreach (var skillId in skillsToAdd)
            {
                _context.InternshipSkills.Add(new InternshipSkill
                {
                    InternshipId = internship.Id,
                    SkillId = skillId
                });
            }
            if (skillsToRemove.Any())
            {
                _context.InternshipSkills.RemoveRange(skillsToRemove);
            }

            await _context.SaveChangesAsync();

            // Automatically post update to Community Feed
            var feedPost = new Post
            {
                AuthorId = company.Id,
                Content = $"🔔 Update: We have updated the details for our internship role: {internship.Title}.\n\n📍 Location: {internship.Location}\n💼 Type: {internship.JobType} ({internship.RemoteType})\n💰 Stipend: {internship.Salary}\n\nCheck out the updated listing on the portal!",
                CreatedAt = DateTime.Now,
                LikesCount = 0
            };
            _context.Posts.Add(feedPost);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Internship updated successfully!";
            return RedirectToAction(nameof(Index));
        }

// GET: Company/Applicants/5
public async Task<IActionResult> Applicants(int id)
{
    var company = await _userManager.GetUserAsync(User);

    if (company == null)
        return Challenge();

    var internship = await _context.Internships
        .Include(i => i.Applications)
            .ThenInclude(a => a.Student)
                .ThenInclude(s => s.UserSkills)
                    .ThenInclude(us => us.Skill)
        .FirstOrDefaultAsync(i =>
            i.Id == id &&
            i.CompanyId == company.Id);

    if (internship == null)
        return NotFound();
    
    ViewBag.Applicants = internship.Applications
    .Select(a => new
    {
        Application = a,
        MatchScore = 100
    })
    .ToList();

    return View(internship);
}

private async Task SendNewInternshipNotificationAsync(Internship internship, string companyName, List<int>? selectedSkillIds)
        {
            // Find students who either match required skills and opted in for notifications
            var studentQuery = _userManager.Users
                .Where(u => u.Role == "Student"
                            && !string.IsNullOrEmpty(u.Email)
                            && u.ReceiveInternshipEmails);

            if (selectedSkillIds != null && selectedSkillIds.Count > 0)
            {
                var matchedStudentIds = await _context.UserSkills
                    .Where(us => selectedSkillIds.Contains(us.SkillId))
                    .Select(us => us.UserId)
                    .Distinct()
                    .ToListAsync();

                studentQuery = studentQuery.Where(u => matchedStudentIds.Contains(u.Id));
            }

            var studentEmails = await studentQuery
                .Select(u => u.Email!)
                .Distinct()
                .ToListAsync();

            if (!studentEmails.Any())
                return;

            var internshipUrl = Url.Action(
                "Details",
                "Student",
                new { id = internship.Id },
                Request.Scheme) ?? string.Empty;

            var subject = $"New internship posted: {internship.Title}";
            var html = $@"<h1>New Internship Available</h1>
                <p>{internship.Title} has been posted by {companyName}.</p>
                <p><strong>Location:</strong> {internship.Location}</p>
                <p><strong>Deadline:</strong> {internship.Deadline:MMMM dd, yyyy}</p>
                <p><a href='{internshipUrl}'>View the internship details</a></p>";

            foreach (var email in studentEmails)
            {
                try
                {
                    await _emailService.SendEmailAsync(email, subject, html);
                }
                catch
                {
                    // Ignore failures for individual emails so posting still succeeds.
                }
            }
        }

        [HttpPost]
public async Task<IActionResult> UpdateStatus(
    int applicationId,
    string status,
    string companyFeedback)
{
    var application = await _context.Applications
        .Include(a => a.Student)
        .Include(a => a.Internship)
        .ThenInclude(i => i.Company)
        .FirstOrDefaultAsync(a => a.Id == applicationId);

    if (application == null)
        return NotFound();

    application.Status = status;
    application.CompanyFeedback = companyFeedback;

    await _context.SaveChangesAsync();

    await SendApplicationStatusNotificationAsync(application);

    TempData["SuccessMessage"] =
        $"Application {status} successfully!";

    return RedirectToAction(
        "Applicants",
        new { id = application.InternshipId });
}

private async Task SendApplicationStatusNotificationAsync(Application application)
{
    if (application.Student == null || string.IsNullOrWhiteSpace(application.Student.Email))
        return;

    if (!application.Student.ReceiveInternshipEmails)
        return;

    var internship = application.Internship;
    var studentEmail = application.Student.Email;
    var status = application.Status ?? "Updated";
    var isAccepted = string.Equals(status, "Accepted", StringComparison.OrdinalIgnoreCase);

    var headerColor = isAccepted ? "#10b981" : "#ef4444";
    var statusTitle = isAccepted ? "Application Shortlisted / Hired! 🎉" : "Application Update (Declined)";
    var statusText = isAccepted 
        ? $"Congratulations! Your application for the <strong>{internship.Title}</strong> role has been shortlisted/accepted by <strong>{internship.Company.FullName}</strong>."
        : $"Thank you for applying to the <strong>{internship.Title}</strong> position. <strong>{internship.Company.FullName}</strong> has reviewed your application and decided not to move forward at this time.";

    var feedbackSection = $@"
        <div style='background-color: #f8fafc; border-left: 4px solid {headerColor}; padding: 15px; border-radius: 4px; margin: 20px 0;'>
            <h4 style='margin: 0 0 6px 0; color: #1e293b; font-size: 14px;'>Hiring Manager Decision Notes & Reason:</h4>
            <p style='margin: 0; font-size: 13.5px; color: #475569; font-style: italic;'>""{application.CompanyFeedback ?? "No additional feedback note provided."}""</p>
        </div>";

    var dashboardUrl = Url.Action("Index", "Student", null, Request.Scheme) ?? string.Empty;

    var subject = isAccepted 
        ? $"Congratulations! Your application for {internship.Title} was accepted" 
        : $"Update regarding your application for {internship.Title}";

    var html = $@"
        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 25px; border: 1px solid #e2e8f0; border-radius: 12px; background-color: #ffffff;'>
            <div style='background-color: {headerColor}; color: #ffffff; padding: 20px; border-radius: 8px 8px 0 0; text-align: center;'>
                <h2 style='margin: 0; font-size: 20px; font-weight: 700;'>{statusTitle}</h2>
            </div>
            <div style='padding: 20px 0;'>
                <p style='font-size: 15px; color: #334155; line-height: 1.6;'>Hi <strong>{application.Student.FullName}</strong>,</p>
                <p style='font-size: 14.5px; color: #475569; line-height: 1.6;'>{statusText}</p>
                
                {feedbackSection}
                
                <p style='font-size: 14px; color: #64748b; line-height: 1.5;'>You can review details and manage all your active applications on your student workspace dashboard.</p>
                
                <div style='text-align: center; margin: 25px 0 15px;'>
                    <a href='{dashboardUrl}' style='background-color: #0284c7; color: #ffffff; padding: 12px 24px; border-radius: 6px; font-weight: 600; text-decoration: none; display: inline-block; font-size: 14px; box-shadow: 0 4px 6px rgba(2, 132, 199, 0.2);'>Go to Student Dashboard</a>
                </div>
            </div>
            <hr style='border: 0; border-top: 1px solid #e2e8f0; margin: 20px 0;' />
            <p style='font-size: 12px; color: #94a3b8; text-align: center;'>This is an automated notification from the Smart Internship Portal matching system.</p>
        </div>";

    try
    {
        await _emailService.SendEmailAsync(studentEmail, subject, html);
    }
    catch
    {
        // Ignore email failures so company status updates still work.
    }
}
    }
}