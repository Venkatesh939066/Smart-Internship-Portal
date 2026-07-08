using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartInternshipPortal.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.IO.Compression;
using System.Xml.Linq;

namespace SmartInternshipPortal.Controllers
{
    [Authorize(Roles = "Student,Admin")]
    public class StudentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public StudentController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Student/Index (Dashboard)
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // 1. Stats
            var applications = await _context.Applications
                .Where(a => a.StudentId == user.Id)
                .ToListAsync();

            ViewBag.TotalApplications = applications.Count;
            ViewBag.AcceptedApplications = applications.Count(a => a.Status == "Accepted");
            ViewBag.PendingApplications = applications.Count(a => a.Status == "Pending");
            ViewBag.RejectedApplications = applications.Count(a => a.Status == "Rejected");

            // 2. Student Skills
            var studentSkillIds = await _context.UserSkills
                .Where(us => us.UserId == user.Id)
                .Select(us => us.SkillId)
                .ToListAsync();

            // 3. Recommended Internships (Smart Matching)
            var allInternships = await _context.Internships
                .Include(i => i.Company)
                .Include(i => i.InternshipSkills)
                    .ThenInclude(isk => isk.Skill)
                .ToListAsync();

            var recommendations = new List<RecommendationViewModel>();
            foreach (var internship in allInternships)
            {
                var reqSkills = internship.InternshipSkills.Select(s => s.SkillId).ToList();
                int matchScore = 100;
                
                if (reqSkills.Count > 0)
                {
                    var intersect = reqSkills.Intersect(studentSkillIds).Count();
                    matchScore = (int)Math.Round((double)intersect / reqSkills.Count * 100);
                }

                var hasApplied = applications.Any(a => a.InternshipId == internship.Id);
                recommendations.Add(new RecommendationViewModel
                {
                    Internship = internship,
                    MatchScore = matchScore,
                    HasApplied = hasApplied
                });
            }

            ViewBag.Recommendations = recommendations
                .OrderByDescending(r => r.MatchScore)
                .ThenByDescending(r => r.Internship.PostedDate)
                .Take(5)
                .ToList();

            ViewBag.TotalInternships = allInternships.Count;
            ViewBag.Applications = applications.Count;
            ViewBag.ApplicationsByStatus = new Dictionary<string, int>
            {
                ["Pending"] = applications.Count(a => a.Status == "Pending"),
                ["Accepted"] = applications.Count(a => a.Status == "Accepted"),
                ["Rejected"] = applications.Count(a => a.Status == "Rejected")
            };

            // 4. Detailed applications log
            var detailedApps = await _context.Applications
                .Where(a => a.StudentId == user.Id)
                .Include(a => a.Internship)
                    .ThenInclude(i => i.Company)
                .Include(a => a.Internship)
                    .ThenInclude(i => i.InternshipSkills)
                        .ThenInclude(isk => isk.Skill)
                .OrderByDescending(a => a.AppliedDate)
                .ToListAsync();

            ViewBag.TopRequiredSkills = detailedApps
                .SelectMany(a => a.Internship.InternshipSkills.Select(isk => isk.Skill.Name))
                .GroupBy(skill => skill)
                .Select(g => new { Name = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToList();

            var months = Enumerable.Range(0, 6)
                .Select(offset => DateTime.Now.AddMonths(offset - 5))
                .ToList();

            ViewBag.MonthlyApplications = months
                .Select(dt => new
                {
                    Label = dt.ToString("MMM yyyy"),
                    Count = detailedApps.Count(a => a.AppliedDate.Year == dt.Year && a.AppliedDate.Month == dt.Month)
                })
                .ToList();

            return View(detailedApps);
        }

        // GET: Student/Profile
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var userSkills = await _context.UserSkills
                .Where(us => us.UserId == user.Id)
                .Include(us => us.Skill)
                .ToListAsync();

            var allSkills = await _context.Skills.ToListAsync();

            ViewBag.AllSkills = allSkills;
            ViewBag.UserSkillIds = userSkills.Select(us => us.SkillId).ToList();

            var resume = await _context.Resumes
                .Where(r => r.UserId == user.Id)
                .OrderByDescending(r => r.UploadedAt)
                .FirstOrDefaultAsync();

            ViewBag.Resume = resume;

            return View(user);
        }

        // GET: Student/EditProfile
        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var userSkills = await _context.UserSkills
                  .Where(us => us.UserId == user.Id)
                  .Include(us => us.Skill)
                  .ToListAsync();

            user.UserSkills = userSkills;
            return View(user);
        }

        // GET: Student/ManageSkills
        [HttpGet]
        public async Task<IActionResult> ManageSkills()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var allSkills = await _context.Skills
                .OrderBy(s => s.Name)
                .ToListAsync();

            var selectedSkillIds = await _context.UserSkills
                .Where(us => us.UserId == user.Id)
                .Select(us => us.SkillId)
                .ToListAsync();

            ViewBag.AllSkills = allSkills;
            ViewBag.SelectedSkillIds = selectedSkillIds;

            return View();
        }

        // POST: Student/UpdateProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(
            string fullName,
            string professionalTitle,
            string location,
            string phoneNumber, 
            string about,
            string college,
            string degree,
            string startMonth,
            string startYearVal,
            string endMonth,
            string endYearVal,
            string educationPercentage,
            string certifications,
            IFormFile? profilePhoto, 
            IFormFile? resumeFile,
            IFormFile? certificatePhoto,
            string? projectTitle,
            string? projectDescription,
            bool receiveInternshipEmails = false)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // Update standard text fields
            if (string.IsNullOrWhiteSpace(fullName))
            {
                ModelState.AddModelError("fullName", "Full Name is required.");
                var userSkills = await _context.UserSkills
                      .Where(us => us.UserId == user.Id)
                      .Include(us => us.Skill)
                      .ToListAsync();
                user.UserSkills = userSkills;
                return View("EditProfile", user);
            }
            user.FullName = fullName;
            user.ProfessionalTitle = professionalTitle;
            user.Location = location;
            user.PhoneNumber2 = phoneNumber;
            user.About = about;
            user.College = college ?? string.Empty;
            user.Degree = degree ?? string.Empty;
            user.EducationPercentage = educationPercentage ?? string.Empty;
            user.Certifications = certifications ?? string.Empty;
            user.ProjectTitle = projectTitle ?? string.Empty;
            user.ProjectDescription = projectDescription ?? string.Empty;

            // Combine month and year dropdown values for display/storage (e.g. "Aug 2026")
            user.StartYear = (!string.IsNullOrEmpty(startMonth) && !string.IsNullOrEmpty(startYearVal))
                ? $"{startMonth} {startYearVal}"
                : (startYearVal ?? string.Empty);

            user.EndYear = (!string.IsNullOrEmpty(endMonth) && !string.IsNullOrEmpty(endYearVal))
                ? $"{endMonth} {endYearVal}"
                : (endYearVal ?? string.Empty);
            
            // Process Profile Picture Upload
            if (profilePhoto != null && profilePhoto.Length > 0)
            {
                var fileName = Guid.NewGuid() + Path.GetExtension(profilePhoto.FileName);
                var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "profiles");

                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                var filePath = Path.Combine(folderPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await profilePhoto.CopyToAsync(stream);
                }

                user.ProfileImage = "/images/profiles/" + fileName;
            }

            // Process Resume Upload
            if (resumeFile != null && resumeFile.Length > 0)
            {
                var fileName = Guid.NewGuid() + Path.GetExtension(resumeFile.FileName);
                var folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "resumes");

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                var filePath = Path.Combine(folder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await resumeFile.CopyToAsync(stream);
                }

                user.ResumePath = "/resumes/" + fileName;
            }

            // Process Project Certificate Photo Upload
            if (certificatePhoto != null && certificatePhoto.Length > 0)
            {
                var fileName = Guid.NewGuid() + Path.GetExtension(certificatePhoto.FileName);
                var folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "certificates");

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                var filePath = Path.Combine(folder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await certificatePhoto.CopyToAsync(stream);
                }

                user.CertificateImage = "/images/certificates/" + fileName;
            }

            user.ReceiveInternshipEmails = receiveInternshipEmails;

            // FIX: Explicitly tell EF Core to track custom ApplicationUser fields as modified
            _context.Entry(user).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            // Update Identity-specific properties securely
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Failed to update identity background settings.");
                return View("EditProfile", user); 
            }

            TempData["SuccessMessage"] = "Profile updated successfully.";
            return RedirectToAction(nameof(EditProfile));
        }

        // GET: Student/Settings
        public async Task<IActionResult> Settings()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            return View(user);
        }

        // POST: Student/UpdateSettings
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSettings(bool receiveInternshipEmails = false)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            user.ReceiveInternshipEmails = receiveInternshipEmails;
            _context.Entry(user).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            await _userManager.UpdateAsync(user);

            TempData["SuccessMessage"] = "Notification settings updated successfully!";
            return RedirectToAction(nameof(Settings));
        }

        // POST: Student/UpdateSkills
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSkills(List<int> selectedSkillIds)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            if (selectedSkillIds == null)
            {
                TempData["ErrorMessage"] = "selectedSkillIds is NULL";
                return RedirectToAction(nameof(ManageSkills));
            }

            if (selectedSkillIds.Count == 0)
            {
                TempData["ErrorMessage"] = "No skills were selected";
                return RedirectToAction(nameof(ManageSkills));
            }

            var existingSkills = await _context.UserSkills.Where(us => us.UserId == user.Id).ToListAsync();
            _context.UserSkills.RemoveRange(existingSkills);
            await _context.SaveChangesAsync();

            if (selectedSkillIds.Count > 0)
            {
                foreach (var skillId in selectedSkillIds)
                {
                    _context.UserSkills.Add(new UserSkill
                    {
                        UserId = user.Id,
                        SkillId = skillId
                    });
                }
                await _context.SaveChangesAsync();
            }
            TempData["SuccessMessage"] = "Skills updated successfully!";
            return RedirectToAction(nameof(EditProfile));
        }

        // GET: Student/Browse
        public async Task<IActionResult> Browse(string searchString, string locationFilter, int? skillFilter)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var studentSkillIds = await _context.UserSkills
                .Where(us => us.UserId == user.Id)
                .Select(us => us.SkillId)
                .ToListAsync();

            var query = _context.Internships
                .Include(i => i.Company)
                .Include(i => i.InternshipSkills)
                    .ThenInclude(isk => isk.Skill)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                searchString = searchString.ToLower();
                query = query.Where(i =>
                    i.Title.ToLower().Contains(searchString) ||
                    i.Description.ToLower().Contains(searchString) ||
                    i.Company.FullName.ToLower().Contains(searchString));
            }
            
            if (!string.IsNullOrEmpty(locationFilter))
            {
                query = query.Where(i => i.Location.Contains(locationFilter));
            }

            if (skillFilter.HasValue)
            {
                query = query.Where(i => i.InternshipSkills.Any(isk => isk.SkillId == skillFilter.Value));
            }

            var internships = await query.ToListAsync();
            var studentApplications = await _context.Applications
                .Where(a => a.StudentId == user.Id)
                .Select(a => a.InternshipId)
                .ToListAsync();

            var browseItems = new List<dynamic>();
            foreach (var internship in internships)
            {
                var reqSkills = internship.InternshipSkills.Select(s => s.SkillId).ToList();
                int matchScore = 100;
                if (reqSkills.Count > 0)
                {
                    var intersect = reqSkills.Intersect(studentSkillIds).Count();
                    matchScore = (int)Math.Round((double)intersect / reqSkills.Count * 100);
                }
                browseItems.Add(new
                {
                    Internship = internship,
                    MatchScore = matchScore,
                    HasApplied = studentApplications.Contains(internship.Id)
                });
            }

            ViewBag.BrowseItems = browseItems.OrderByDescending(x => x.MatchScore).ToList();
            ViewBag.AllSkills = await _context.Skills.ToListAsync();
            ViewBag.SelectedSkill = skillFilter;
            ViewBag.SearchString = searchString;
            ViewBag.LocationFilter = locationFilter;
            return View();
        }

        // GET: Student/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var internship = await _context.Internships
                .Include(i => i.Company)
                .Include(i => i.InternshipSkills)
                    .ThenInclude(isk => isk.Skill)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (internship == null) return NotFound();

            var studentSkillIds = await _context.UserSkills
                .Where(us => us.UserId == user.Id)
                .Select(us => us.SkillId)
                .ToListAsync();

            var requiredSkillIds = internship.InternshipSkills
                .Select(isk => isk.SkillId)
                .ToList();

            var matchScore = 100;
            if (requiredSkillIds.Count > 0)
            {
                var matchedSkills = requiredSkillIds.Intersect(studentSkillIds).Count();
                matchScore = (int)Math.Round((double)matchedSkills / requiredSkillIds.Count * 100);
            }

            var application = await _context.Applications
                .FirstOrDefaultAsync(a => a.StudentId == user.Id && a.InternshipId == id);

            ViewBag.MatchScore = matchScore;
            ViewBag.HasApplied = application != null;
            ViewBag.ApplicationStatus = application?.Status ?? string.Empty;

            return View(internship);
        }

        // POST: Student/Apply
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply(int internshipId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var alreadyApplied = await _context.Applications
                .AnyAsync(a => a.StudentId == user.Id && a.InternshipId == internshipId);

            if (!alreadyApplied)
            {
                var application = new Application
                {
                    StudentId = user.Id,
                    InternshipId = internshipId,
                    AppliedDate = DateTime.Now,
                    Status = "Pending"
                };
                _context.Applications.Add(application);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Successfully applied for the internship!";
            }
            else
            {
                TempData["ErrorMessage"] = "You have already applied for this internship.";
            }
            return RedirectToAction(nameof(Details), new { id = internshipId });
        }

        // GET: Student/CompanyDetails/guid
        public async Task<IActionResult> CompanyDetails(string id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var company = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (company == null) return NotFound();

            var internships = await _context.Internships
                .Where(i => i.CompanyId == id)
                .Include(i => i.InternshipSkills)
                    .ThenInclude(isk => isk.Skill)
                .OrderByDescending(i => i.PostedDate)
                .ToListAsync();

            var studentSkillIds = await _context.UserSkills
                .Where(us => us.UserId == user.Id)
                .Select(us => us.SkillId)
                .ToListAsync();

            var studentApplications = await _context.Applications
                .Where(a => a.StudentId == user.Id)
                .Select(a => a.InternshipId)
                .ToListAsync();

            var listingItems = new List<dynamic>();
            foreach (var internship in internships)
            {
                var reqSkills = internship.InternshipSkills.Select(s => s.SkillId).ToList();
                int matchScore = 100;
                
                if (reqSkills.Count > 0)
                {
                    var intersect = reqSkills.Intersect(studentSkillIds).Count();
                    matchScore = (int)Math.Round((double)intersect / reqSkills.Count * 100);
                }
                listingItems.Add(new
                {
                    Internship = internship,
                    MatchScore = matchScore,
                    HasApplied = studentApplications.Contains(internship.Id)
                });
            }
            ViewBag.ListingItems = listingItems;
            return View(company);
        }

        // GET: Student/MySavedJobs
        public async Task<IActionResult> MySavedJobs()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var savedJobs = await _context.SavedJobs
                .Where(s => s.StudentId == user.Id)
                .Include(s => s.Internship)
                    .ThenInclude(i => i.Company)
                .ToListAsync();

            return View(savedJobs);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveJob(int internshipId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var exists = await _context.SavedJobs
                .AnyAsync(s => s.StudentId == user.Id && s.InternshipId == internshipId);
            if (!exists)
            {
                _context.SavedJobs.Add(new Models.SavedJob
                {
                    StudentId = user.Id,
                    InternshipId = internshipId,
                    SavedAt = DateTime.Now
                });
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Job saved to your bookmarks.";
            }
            else
            {
                TempData["ErrorMessage"] = "This job is already in your saved list.";
            }

            return RedirectToAction(nameof(Browse));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveSavedJob(int savedJobId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var saved = await _context.SavedJobs.FirstOrDefaultAsync(s => s.Id == savedJobId && s.StudentId == user.Id);
            if (saved != null)
            {
                _context.SavedJobs.Remove(saved);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Removed from saved jobs.";
            }
            else
            {
                TempData["ErrorMessage"] = "Saved job not found.";
            }

            return RedirectToAction(nameof(MySavedJobs));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSavedJob(int savedJobId, string notes, string tags)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var saved = await _context.SavedJobs.FirstOrDefaultAsync(s => s.Id == savedJobId && s.StudentId == user.Id);
            if (saved != null)
            {
                saved.Notes = notes ?? string.Empty;
                saved.Tags = tags ?? string.Empty;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Saved job updated.";
            }
            else
            {
                TempData["ErrorMessage"] = "Saved job not found.";
            }

            return RedirectToAction(nameof(MySavedJobs));
        }

        [HttpPost]
        public async Task<IActionResult> UploadResume(IFormFile resumeFile)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            if (resumeFile == null || resumeFile.Length == 0)
            {
                TempData["ErrorMessage"] = "Please select a resume file to upload.";
                return RedirectToAction(nameof(Profile));
            }

            var allowedExtensions = new[] { ".pdf", ".doc", ".docx" };
            var extension = Path.GetExtension(resumeFile.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                TempData["ErrorMessage"] = "Unsupported file type. Upload a PDF, DOC, or DOCX file.";
                return RedirectToAction(nameof(Profile));
            }

            var fileData = new MemoryStream();
            await resumeFile.CopyToAsync(fileData);
            fileData.Position = 0;

            var detectedSkills = DetectResumeSkills(resumeFile.FileName, extension, fileData);
            fileData.Position = 0;

            var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "resumes");
            Directory.CreateDirectory(folderPath);
            var fileName = Guid.NewGuid().ToString("N") + "_" + Path.GetFileName(resumeFile.FileName);
            var filePath = Path.Combine(folderPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await fileData.CopyToAsync(stream);
            }

            var existingResume = await _context.Resumes
                .Where(r => r.UserId == user.Id)
                .OrderByDescending(r => r.UploadedAt)
                .FirstOrDefaultAsync();

            if (existingResume != null)
            {
                var oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", existingResume.FilePath.TrimStart('/', '\\').Replace('/', '\\'));
                if (System.IO.File.Exists(oldPath))
                {
                    System.IO.File.Delete(oldPath);
                }

                existingResume.FilePath = "/resumes/" + fileName;
                existingResume.UploadedAt = DateTime.Now;
                _context.Resumes.Update(existingResume);
            }
            else
            {
                var resume = new Resume
                {
                    UserId = user.Id,
                    FilePath = "/resumes/" + fileName,
                    UploadedAt = DateTime.Now
                };
                _context.Resumes.Add(resume);
            }

            await _context.SaveChangesAsync();

            if (detectedSkills.Count > 0)
            {
                await ApplyDetectedSkillsAsync(user.Id, detectedSkills);
                TempData["DetectedSkills"] = string.Join(";", detectedSkills);
                TempData["SuccessMessage"] = $"Resume uploaded successfully! Skills automatically added: {string.Join(", ", detectedSkills)}";
            }
            else
            {
                TempData["DetectedSkills"] = string.Empty;
                TempData["SuccessMessage"] = "Resume uploaded successfully!";
            }

            return RedirectToAction(nameof(Profile));
        }

        private static readonly Dictionary<string, string[]> ResumeSkillKeywords = new()
        {
            { "Python", new[] { "python" } },
            { "SQL", new[] { "sql", "structured query language" } },
            { "Azure", new[] { "azure" } },
            { "Spark", new[] { "spark", "pyspark", "databricks" } },
            { "Power BI", new[] { "power bi", "powerbi", "power platform" } }
        };

        private static List<string> DetectResumeSkills(string fileName, string extension, Stream fileStream)
        {
            var detected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var normalizedText = fileName + " ";
            normalizedText += ExtractTextFromResume(fileStream, extension);
            normalizedText = normalizedText.ToLowerInvariant();

            foreach (var (skill, keywords) in ResumeSkillKeywords)
            {
                foreach (var keyword in keywords)
                {
                    if (normalizedText.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    {
                        detected.Add(skill);
                        break;
                    }
                }
            }

            return detected.ToList();
        }

        private static string ExtractTextFromResume(Stream stream, string extension)
        {
            try
            {
                stream.Position = 0;
                return extension switch
                {
                    ".docx" => ExtractTextFromDocx(stream),
                    ".pdf" => ExtractTextFromPdf(stream),
                    ".doc" => ExtractTextFromBinary(stream),
                    _ => string.Empty,
                };
            }
            catch
            {
                return string.Empty;
            }
            finally
            {
                stream.Position = 0;
            }
        }

        private static string ExtractTextFromDocx(Stream stream)
        {
            try
            {
                using var archive = new ZipArchive(stream, ZipArchiveMode.Read, true);
                var entry = archive.GetEntry("word/document.xml");
                if (entry == null) return string.Empty;

                using var entryStream = entry.Open();
                var document = XDocument.Load(entryStream);
                return string.Join(" ", document.Descendants().Where(e => e.Name.LocalName == "t").Select(e => e.Value));
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string ExtractTextFromPdf(Stream stream)
        {
            try
            {
                stream.Position = 0;
                using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
                var raw = reader.ReadToEnd();
                if (string.IsNullOrWhiteSpace(raw))
                {
                    stream.Position = 0;
                    using var fallback = new StreamReader(stream, Encoding.Latin1, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
                    raw = fallback.ReadToEnd();
                }
                return raw;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string ExtractTextFromBinary(Stream stream)
        {
            try
            {
                stream.Position = 0;
                using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
                var text = reader.ReadToEnd();
                if (string.IsNullOrWhiteSpace(text))
                {
                    stream.Position = 0;
                    using var fallback = new StreamReader(stream, Encoding.Latin1, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
                    text = fallback.ReadToEnd();
                }
                return text;
            }
            catch
            {
                return string.Empty;
            }
        }

        private async Task ApplyDetectedSkillsAsync(string userId, List<string> skillNames)
        {
            var normalizedSkillNames = skillNames.Select(s => s.Trim()).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var existingSkills = await _context.Skills
                .Where(s => normalizedSkillNames.Contains(s.Name))
                .ToDictionaryAsync(s => s.Name, s => s.Id, StringComparer.OrdinalIgnoreCase);

            foreach (var skillName in normalizedSkillNames)
            {
                if (!existingSkills.TryGetValue(skillName, out var skillId))
                {
                    var newSkill = new Skill { Name = skillName };
                    _context.Skills.Add(newSkill);
                    await _context.SaveChangesAsync();
                    skillId = newSkill.Id;
                    existingSkills[skillName] = skillId;
                }

                var hasUserSkill = await _context.UserSkills
                    .AnyAsync(us => us.UserId == userId && us.SkillId == skillId);

                if (!hasUserSkill)
                {
                    _context.UserSkills.Add(new UserSkill
                    {
                        UserId = userId,
                        SkillId = skillId
                    });
                }
            }

            await _context.SaveChangesAsync();
        }

        [HttpGet]
        public async Task<IActionResult> ExportResume(string? id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            var targetUserId = string.IsNullOrEmpty(id) ? currentUser.Id : id;

            var user = await _context.Users
                .Include(u => u.UserSkills)
                    .ThenInclude(us => us.Skill)
                .FirstOrDefaultAsync(u => u.Id == targetUserId);

            if (user == null)
            {
                return NotFound("Student not found.");
            }

            return View(user);
        }

        [HttpGet]
        public async Task<IActionResult> DownloadResume()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var resume = await _context.Resumes
                .Where(r => r.UserId == user.Id)
                .OrderByDescending(r => r.UploadedAt)
                .FirstOrDefaultAsync();

            if (resume == null) return NotFound();

            var physicalPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", resume.FilePath.TrimStart('/', '\\').Replace('/', '\\'));
            if (!System.IO.File.Exists(physicalPath)) return NotFound();

            var contentType = resume.FilePath.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)
                ? "application/pdf"
                : resume.FilePath.EndsWith(".docx", StringComparison.OrdinalIgnoreCase)
                    ? "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                    : resume.FilePath.EndsWith(".doc", StringComparison.OrdinalIgnoreCase)
                        ? "application/msword"
                        : "application/octet-stream";

            return PhysicalFile(physicalPath, contentType, Path.GetFileName(physicalPath));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteResume()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var resume = await _context.Resumes
                .Where(r => r.UserId == user.Id)
                .OrderByDescending(r => r.UploadedAt)
                .FirstOrDefaultAsync();

            if (resume == null)
            {
                TempData["ErrorMessage"] = "No resume found to delete.";
                return RedirectToAction(nameof(Profile));
            }

            var physicalPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", resume.FilePath.TrimStart('/', '\\').Replace('/', '\\'));
            if (System.IO.File.Exists(physicalPath))
            {
                System.IO.File.Delete(physicalPath);
            }

            _context.Resumes.Remove(resume);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Resume deleted.";
            return RedirectToAction(nameof(EditProfile));
        }
    }

    public class RecommendationViewModel
    {
        public Internship Internship { get; set; } = null!;
        public int MatchScore { get; set; }
        public bool HasApplied { get; set; }
    }
}