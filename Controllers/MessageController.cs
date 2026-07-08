using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartInternshipPortal.Models;
using System;
using System.Collections.Generic;
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
        public async Task<IActionResult> Index(string? withUserId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
                return Challenge();

            if (string.IsNullOrEmpty(withUserId))
            {
                // Fetch all conversations for current user
                var userMessages = await _context.Messages
                    .Where(m => m.SenderId == currentUser.Id || m.ReceiverId == currentUser.Id)
                    .Include(m => m.Sender)
                    .Include(m => m.Receiver)
                    .OrderByDescending(m => m.SentAt)
                    .ToListAsync();

                var conversations = userMessages
                    .GroupBy(m => m.SenderId == currentUser.Id ? m.ReceiverId : m.SenderId)
                    .Select(g => {
                        var lastMsg = g.First();
                        var otherUser = lastMsg.SenderId == currentUser.Id ? lastMsg.Receiver : lastMsg.Sender;
                        var unreadCount = g.Count(m => m.ReceiverId == currentUser.Id && !m.IsRead);
                        return new ConversationViewModel
                        {
                            OtherUser = otherUser,
                            LastMessage = lastMsg,
                            UnreadCount = unreadCount
                        };
                    })
                    .ToList();

                ViewBag.CurrentUser = currentUser;
                return View("Inbox", conversations);
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

    public class ConversationViewModel
    {
        public ApplicationUser OtherUser { get; set; } = null!;
        public Message LastMessage { get; set; } = null!;
        public int UnreadCount { get; set; }
    }
}
