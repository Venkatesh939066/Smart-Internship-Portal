using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartInternshipPortal.Models;

namespace SmartInternshipPortal.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public HomeController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        // 1. Fetch latest 3 internships
        var latestInternships = await _context.Internships
            .Include(i => i.Company)
            .Include(i => i.InternshipSkills)
                .ThenInclude(isk => isk.Skill)
            .OrderByDescending(i => i.PostedDate)
            .Take(3)
            .ToListAsync();

        // 2. Fetch statistics
        var studentCount = await _context.Users.CountAsync(u => u.Role == "Student");
        var companyCount = await _context.Users.CountAsync(u => u.Role == "Company");
        var internshipCount = await _context.Internships.CountAsync();
        var applicationCount = await _context.Applications.CountAsync();

        ViewBag.StudentsCount = 12500 + studentCount;
        ViewBag.CompaniesCount = 500 + companyCount;
        ViewBag.InternshipsCount = 3200 + internshipCount;
        ViewBag.ApplicationsCount = 18000 + applicationCount;

        // 3. User Match Scores (if student is logged in)
        var studentSkillIds = new List<int>();
        var user = await _userManager.GetUserAsync(User);
        if (user != null && user.Role == "Student")
        {
            studentSkillIds = await _context.UserSkills
                .Where(us => us.UserId == user.Id)
                .Select(us => us.SkillId)
                .ToListAsync();
        }
        ViewBag.StudentSkillIds = studentSkillIds;

        return View(latestInternships);
    }

    public async Task<IActionResult> Companies()
    {
        var companies = await _context.Users
            .Where(u => u.Role == "Company")
            .ToListAsync();

        var companyStats = new Dictionary<string, int>();
        foreach (var company in companies)
        {
            var count = await _context.Internships.CountAsync(i => i.CompanyId == company.Id);
            companyStats[company.Id] = count;
        }
        ViewBag.CompanyStats = companyStats;

        var currentUser = await _userManager.GetUserAsync(User);
        ViewBag.IsAdmin = currentUser != null && currentUser.Role == "Admin";
        ViewBag.IsCompany = currentUser != null && currentUser.Role == "Company";
        ViewBag.CurrentUserId = currentUser?.Id;
        ViewBag.IsAuthenticated = currentUser != null;

        return View(companies);
    }

    public async Task<IActionResult> CompanyDetails(string id)
    {
        var company = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (company == null) return NotFound();

        var internships = await _context.Internships
            .Where(i => i.CompanyId == id)
            .Include(i => i.InternshipSkills)
                .ThenInclude(isk => isk.Skill)
            .OrderByDescending(i => i.PostedDate)
            .ToListAsync();

        var studentSkillIds = new List<int>();
        var studentApplications = new List<int>();
        var currentUser = await _userManager.GetUserAsync(User);
        
        if (currentUser != null && currentUser.Role == "Student")
        {
            studentSkillIds = await _context.UserSkills
                .Where(us => us.UserId == currentUser.Id)
                .Select(us => us.SkillId)
                .ToListAsync();

            studentApplications = await _context.Applications
                .Where(a => a.StudentId == currentUser.Id)
                .Select(a => a.InternshipId)
                .ToListAsync();
        }

        var listingItems = new List<dynamic>();
        foreach (var internship in internships)
        {
            var reqSkills = internship.InternshipSkills.Select(s => s.SkillId).ToList();
            int matchScore = 100;
            if (reqSkills.Count > 0 && currentUser != null && currentUser.Role == "Student")
            {
                var intersect = reqSkills.Intersect(studentSkillIds).Count();
                matchScore = (int)Math.Round((double)intersect / reqSkills.Count * 100);
            }
            else if (currentUser == null)
            {
                matchScore = new Random(internship.Id).Next(85, 98);
            }

            listingItems.Add(new
            {
                Internship = internship,
                MatchScore = matchScore,
                HasApplied = studentApplications.Contains(internship.Id)
            });
        }

        ViewBag.ListingItems = listingItems;
        ViewBag.IsAdmin = currentUser != null && currentUser.Role == "Admin";
        ViewBag.IsCompany = currentUser != null && currentUser.Role == "Company";
        ViewBag.CurrentUserId = currentUser?.Id;

        return View(company);
    }

    public async Task<IActionResult> Analytics()
    {
        ViewBag.TotalInternships = await _context.Internships.CountAsync();
        ViewBag.TotalCompanies = await _context.Users.CountAsync(u => u.Role == "Company");
        ViewBag.TotalSkills = await _context.Skills.CountAsync();
        
        // Remote types
        ViewBag.RemoteCount = await _context.Internships.CountAsync(i => i.RemoteType == "Remote");
        ViewBag.HybridCount = await _context.Internships.CountAsync(i => i.RemoteType == "Hybrid");
        ViewBag.OnSiteCount = await _context.Internships.CountAsync(i => i.RemoteType == "On-site" || i.RemoteType == "Onsite");

        // Top 6 skills
        var topSkills = await _context.InternshipSkills
            .GroupBy(isk => isk.Skill.Name)
            .Select(g => new { SkillName = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(6)
            .ToListAsync();

        ViewBag.TopSkillNames = topSkills.Select(ts => ts.SkillName).ToList();
        ViewBag.TopSkillCounts = topSkills.Select(ts => ts.Count).ToList();

        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
