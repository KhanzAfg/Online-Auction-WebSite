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
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public PaymentController(
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

            var payments = await _context.Payments
                .Include(p => p.Auction)
                .Include(p => p.Buyer)
                .Include(p => p.Seller)
                .Where(p => p.BuyerId == userId || p.SellerId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(payments);
        }

        public async Task<IActionResult> ProcessPayment(int auctionId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var auction = await _context.Auctions
                .Include(a => a.Bids)
                .Include(a => a.Seller)
                .FirstOrDefaultAsync(a => a.Id == auctionId);

            if (auction == null)
            {
                return NotFound();
            }

            // Check if user is the winning bidder
            var winningBid = auction.Bids.OrderByDescending(b => b.Amount).FirstOrDefault();
            if (winningBid?.BidderId != userId)
            {
                TempData["Error"] = "You can only process payment for auctions you won.";
                return RedirectToAction("Index", "Home");
            }

            // Check if auction has ended
            if (auction.Status != AuctionStatus.Ended || auction.EndDate > DateTime.UtcNow)
            {
                TempData["Error"] = "This auction has not ended yet.";
                return RedirectToAction("AuctionDetails", "Home", new { id = auctionId });
            }

            // Check if payment already exists
            var existingPayment = await _context.Payments
                .FirstOrDefaultAsync(p => p.AuctionId == auctionId && p.BuyerId == userId);

            if (existingPayment != null)
            {
                TempData["Error"] = "Payment has already been processed for this auction.";
                return RedirectToAction("AuctionDetails", "Home", new { id = auctionId });
            }

            var viewModel = new ProcessPaymentViewModel
            {
                AuctionId = auctionId,
                AuctionTitle = auction.Title,
                Amount = winningBid.Amount,
                SellerName = auction.Seller?.FullName ?? "Unknown Seller"
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessPayment(ProcessPaymentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var auction = await _context.Auctions
                .Include(a => a.Bids)
                .Include(a => a.Seller)
                .FirstOrDefaultAsync(a => a.Id == model.AuctionId);

            if (auction == null)
            {
                return NotFound();
            }

            // Check if user is the winning bidder
            var winningBid = auction.Bids.OrderByDescending(b => b.Amount).FirstOrDefault();
            if (winningBid?.BidderId != userId)
            {
                TempData["Error"] = "You can only process payment for auctions you won.";
                return RedirectToAction("Index", "Home");
            }

            // Create payment record
            var payment = new Payment
            {
                AuctionId = model.AuctionId,
                BuyerId = userId,
                SellerId = auction.SellerId,
                Amount = winningBid.Amount,
                Status = PaymentStatus.Processing,
                Method = model.PaymentMethod,
                Description = $"Payment for auction: {auction.Title}",
                TransactionId = Guid.NewGuid().ToString()
            };

            _context.Payments.Add(payment);

            // Create shipping record
            var shipping = new Shipping
            {
                AuctionId = model.AuctionId,
                BuyerId = userId,
                SellerId = auction.SellerId,
                ShippingAddress = model.ShippingAddress,
                City = model.City,
                State = model.State,
                PostalCode = model.PostalCode,
                Country = model.Country,
                ShippingCost = model.ShippingCost,
                Status = ShippingStatus.Pending
            };

            _context.Shippings.Add(shipping);

            // Send notifications
            var buyerNotification = new Notification
            {
                UserId = userId,
                Title = "Payment Processed",
                Message = $"Your payment of ${winningBid.Amount:F2} for '{auction.Title}' has been processed successfully.",
                Type = NotificationType.Auction,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                RelatedEntityId = auction.Id
            };

            var sellerNotification = new Notification
            {
                UserId = auction.SellerId,
                Title = "Payment Received",
                Message = $"Payment of ${winningBid.Amount:F2} has been received for your auction '{auction.Title}'. Please ship the item.",
                Type = NotificationType.Auction,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                RelatedEntityId = auction.Id
            };

            _context.Notifications.Add(buyerNotification);
            _context.Notifications.Add(sellerNotification);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Payment processed successfully! The seller has been notified.";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> PaymentDetails(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var payment = await _context.Payments
                .Include(p => p.Auction)
                .Include(p => p.Buyer)
                .Include(p => p.Seller)
                .FirstOrDefaultAsync(p => p.Id == id && (p.BuyerId == userId || p.SellerId == userId));

            if (payment == null)
            {
                return NotFound();
            }

            return View(payment);
        }
    }
} 