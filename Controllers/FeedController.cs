using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartInternshipPortal.Models;

namespace SmartInternshipPortal.Controllers
{
    [Authorize]
    public class FeedController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public FeedController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Feed
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var posts = await _context.Posts
                .Include(p => p.Author)
                .Include(p => p.Comments)
                    .ThenInclude(c => c.Author)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            // Get post IDs the current user liked
            var likedPostIds = await _context.Likes
                .Where(l => l.UserId == user.Id)
                .Select(l => l.PostId)
                .ToListAsync();

            ViewBag.LikedPostIds = likedPostIds;
            ViewBag.CurrentUser = user;

            return View(posts);
        }

        // POST: Feed/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string content, IFormFile? imageFile)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            if (string.IsNullOrWhiteSpace(content) && imageFile == null)
            {
                TempData["ErrorMessage"] = "Post content cannot be empty.";
                return RedirectToAction(nameof(Index));
            }

            var post = new Post
            {
                AuthorId = user.Id,
                Content = content ?? string.Empty,
                CreatedAt = DateTime.Now,
                LikesCount = 0
            };

            if (imageFile != null && imageFile.Length > 0)
            {
                var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
                var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                if (allowed.Contains(extension))
                {
                    var fileName = Guid.NewGuid().ToString("N") + extension;
                    var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "feed");
                    Directory.CreateDirectory(folderPath);

                    var filePath = Path.Combine(folderPath, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }
                    post.ImageUrl = "/images/feed/" + fileName;
                }
            }

            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Post shared successfully!";
            return RedirectToAction(nameof(Index));
        }

        // POST: Feed/Like
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Like(int postId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var post = await _context.Posts.FindAsync(postId);
            if (post == null) return NotFound();

            var existingLike = await _context.Likes
                .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == user.Id);

            if (existingLike != null)
            {
                _context.Likes.Remove(existingLike);
                post.LikesCount = Math.Max(0, post.LikesCount - 1);
            }
            else
            {
                var like = new Like
                {
                    PostId = postId,
                    UserId = user.Id
                };
                _context.Likes.Add(like);
                post.LikesCount += 1;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // POST: Feed/Comment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Comment(int postId, string content)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            if (string.IsNullOrWhiteSpace(content))
            {
                TempData["ErrorMessage"] = "Comment content cannot be empty.";
                return RedirectToAction(nameof(Index));
            }

            var post = await _context.Posts.FindAsync(postId);
            if (post == null) return NotFound();

            var comment = new Comment
            {
                PostId = postId,
                AuthorId = user.Id,
                Content = content,
                CreatedAt = DateTime.Now
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Comment added!";
            return RedirectToAction(nameof(Index));
        }
    }
}
