using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartInternshipPortal.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SmartInternshipPortal.Controllers
{
    [Authorize]
    public class MessageController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public MessageController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Message?withUserId=xxx
        public async Task<IActionResult> Index(string withUserId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
                return Challenge();

            if (string.IsNullOrEmpty(withUserId))
            {
                return RedirectToAction("Index", currentUser.Role == "Company" ? "Company" : "Student");
            }

            var otherUser = await _userManager.FindByIdAsync(withUserId);
            if (otherUser == null)
                return NotFound("User not found.");

            // Get messages
            var messages = await _context.Messages
                .Where(m => (m.SenderId == currentUser.Id && m.ReceiverId == withUserId) ||
                            (m.SenderId == withUserId && m.ReceiverId == currentUser.Id))
                .OrderBy(m => m.SentAt)
                .ToListAsync();

            // Mark unread messages as read
            var unreadMessages = messages.Where(m => m.ReceiverId == currentUser.Id && !m.IsRead).ToList();
            if (unreadMessages.Any())
            {
                foreach (var msg in unreadMessages)
                {
                    msg.IsRead = true;
                }
                await _context.SaveChangesAsync();
            }

            ViewBag.CurrentUser = currentUser;
            ViewBag.OtherUser = otherUser;

            return View(messages);
        }

        // POST: Message/Send
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(string receiverId, string content)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
                return Challenge();

            if (string.IsNullOrEmpty(receiverId) || string.IsNullOrWhiteSpace(content))
            {
                return RedirectToAction("Index", new { withUserId = receiverId });
            }

            var message = new Message
            {
                SenderId = currentUser.Id,
                ReceiverId = receiverId,
                Content = content.Trim(),
                SentAt = DateTime.Now,
                IsRead = false
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", new { withUserId = receiverId });
        }
    }
}
