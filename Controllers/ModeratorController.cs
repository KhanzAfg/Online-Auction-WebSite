using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineAuction.Data;
using OnlineAuction.Models;
using System.Threading.Tasks;

namespace OnlineAuction.Controllers
{
    [Authorize(Roles = "Admin,Moderator")]
    public class ModeratorController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ModeratorController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var pendingAuctions = await _context.Auctions
                .Include(a => a.Seller)
                .Include(a => a.Category)
                .Include(a => a.Images)
                .Where(a => a.Status == AuctionStatus.Pending)
                .ToListAsync();

            return View(pendingAuctions);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveAuction(int id)
        {
            var auction = await _context.Auctions.FindAsync(id);
            if (auction == null)
            {
                return NotFound();
            }

            auction.Status = AuctionStatus.Active;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Auction approved successfully.";
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Reports()
        {
            // You can add logic to fetch reports here if needed
            return View();
        }

        public async Task<IActionResult> Auctions()
        {
            var auctions = await _context.Auctions
                .Include(a => a.Seller)
                .Include(a => a.Category)
                .Include(a => a.Images)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
            return View(auctions);
        }

        // Additional moderator actions can be added here, e.g., RejectAuction, SuspendUser
    }
} 