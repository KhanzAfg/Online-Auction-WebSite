using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineAuction.Data;
using OnlineAuction.Models;
using OnlineAuction.ViewModels;
using System.Security.Claims;

namespace OnlineAuction.Controllers
{
    [Authorize]
    public class MessageController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public MessageController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var conversations = await _context.Conversations
                .Include(c => c.User1)
                .Include(c => c.User2)
                .Include(c => c.Messages.OrderByDescending(m => m.CreatedAt).Take(1))
                .Where(c => c.User1Id == userId || c.User2Id == userId)
                .OrderByDescending(c => c.LastMessageAt)
                .ToListAsync();

            var viewModel = conversations.Select(c => new ConversationViewModel
            {
                Id = c.Id,
                OtherUser = c.User1Id == userId ? c.User2 : c.User1,
                LastMessage = c.Messages.FirstOrDefault()?.Content ?? "No messages yet",
                LastMessageTime = c.LastMessageAt,
                UnreadCount = c.Messages.Count(m => !m.IsRead && m.ReceiverId == userId)
            }).ToList();

            return View(viewModel);
        }

        public async Task<IActionResult> Conversation(int conversationId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var conversation = await _context.Conversations
                .Include(c => c.User1)
                .Include(c => c.User2)
                .Include(c => c.Messages.OrderBy(m => m.CreatedAt))
                .FirstOrDefaultAsync(c => c.Id == conversationId && (c.User1Id == userId || c.User2Id == userId));

            if (conversation == null)
            {
                return NotFound();
            }

            // Mark messages as read
            var unreadMessages = conversation.Messages.Where(m => !m.IsRead && m.ReceiverId == userId);
            foreach (var message in unreadMessages)
            {
                message.IsRead = true;
                message.ReadAt = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();

            var otherUser = conversation.User1Id == userId ? conversation.User2 : conversation.User1;

            var viewModel = new ConversationDetailViewModel
            {
                ConversationId = conversationId,
                OtherUser = otherUser,
                Messages = conversation.Messages.ToList()
            };

            return View(viewModel);
        }

        public async Task<IActionResult> StartConversation(string userId, int? auctionId = null)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId))
            {
                return RedirectToAction("Login", "Account");
            }

            if (currentUserId == userId)
            {
                TempData["Error"] = "You cannot start a conversation with yourself.";
                return RedirectToAction("Index");
            }

            // Check if conversation already exists
            var existingConversation = await _context.Conversations
                .FirstOrDefaultAsync(c => 
                    (c.User1Id == currentUserId && c.User2Id == userId) ||
                    (c.User1Id == userId && c.User2Id == currentUserId));

            if (existingConversation != null)
            {
                return RedirectToAction("Conversation", new { conversationId = existingConversation.Id });
            }

            // Create new conversation
            var conversation = new Conversation
            {
                User1Id = currentUserId,
                User2Id = userId
            };

            _context.Conversations.Add(conversation);
            await _context.SaveChangesAsync();

            return RedirectToAction("Conversation", new { conversationId = conversation.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(int conversationId, string content, int? auctionId = null)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return BadRequest("Message content cannot be empty.");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var conversation = await _context.Conversations
                .FirstOrDefaultAsync(c => c.Id == conversationId && (c.User1Id == userId || c.User2Id == userId));

            if (conversation == null)
            {
                return NotFound();
            }

            var receiverId = conversation.User1Id == userId ? conversation.User2Id : conversation.User1Id;

            var message = new Message
            {
                SenderId = userId,
                ReceiverId = receiverId,
                Content = content.Trim(),
                AuctionId = auctionId
            };

            _context.Messages.Add(message);

            // Update conversation last message time
            conversation.LastMessageAt = DateTime.UtcNow;

            // Send notification to receiver
            var notification = new Notification
            {
                UserId = receiverId,
                Title = "New Message",
                Message = $"You have a new message from {User.Identity?.Name}.",
                Type = NotificationType.Message,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);

            await _context.SaveChangesAsync();

            return RedirectToAction("Conversation", new { conversationId });
        }

        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Json(0);
            }

            var unreadCount = await _context.Messages
                .CountAsync(m => m.ReceiverId == userId && !m.IsRead);

            return Json(unreadCount);
        }
    }
} 