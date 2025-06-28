using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineAuction.Data;
using OnlineAuction.Models;
using OnlineAuction.ViewModels;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace OnlineAuction.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<AdminController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var viewModel = new AdminDashboardViewModel
            {
                TotalUsers = await _context.Users.CountAsync(),
                TotalAuctions = await _context.Auctions.CountAsync(),
                TotalCategories = await _context.Categories.CountAsync(),
                RecentReports = await _context.AuctionReports
                    .Include(r => r.Auction)
                    .Include(r => r.Reporter)
                    .OrderByDescending(r => r.ReportedAt)
                    .Take(5)
                    .ToListAsync()
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Users()
        {
            var users = await _context.Users
                .Include(u => u.Auctions)
                .Include(u => u.Bids)
                .Include(u => u.RatingsReceived)
                .ToListAsync();

            return View(users);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUserBlock(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            user.IsBlocked = !user.IsBlocked;
            await _userManager.UpdateAsync(user);

            // Notify the user
            var notification = new Notification
            {
                UserId = userId,
                Title = user.IsBlocked ? "Account Blocked" : "Account Unblocked",
                Message = user.IsBlocked 
                    ? "Your account has been blocked by an administrator. Please contact support for more information."
                    : "Your account has been unblocked. You can now use the platform again.",
                Type = NotificationType.System,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"User {(user.IsBlocked ? "blocked" : "unblocked")} successfully.";
            return RedirectToAction(nameof(Users));
        }

        public async Task<IActionResult> Categories()
        {
            var categories = await _context.Categories
                .Include(c => c.Auctions)
                .ToListAsync();

            return View(categories);
        }

        public IActionResult AddCategory()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCategory(CategoryViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var category = new Category
            {
                Name = model.Name,
                Description = model.Description
            };

            if (model.Image != null && model.Image.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "categories");
                Directory.CreateDirectory(uploadsFolder);

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(model.Image.FileName)}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.Image.CopyToAsync(stream);
                }

                category.ImagePath = $"/images/categories/{fileName}";
            }

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Category added successfully.";
            return RedirectToAction(nameof(Categories));
        }

        public async Task<IActionResult> MergeCategories()
        {
            var categories = await _context.Categories.ToListAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MergeCategories(MergeCategoriesViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var categories = await _context.Categories.ToListAsync();
                ViewBag.Categories = new SelectList(categories, "Id", "Name");
                return View(model);
            }

            var sourceCategory = await _context.Categories
                .Include(c => c.Auctions)
                .FirstOrDefaultAsync(c => c.Id == model.SourceCategoryId);

            var targetCategory = await _context.Categories
                .FirstOrDefaultAsync(c => c.Id == model.TargetCategoryId);

            if (sourceCategory == null || targetCategory == null)
            {
                return NotFound();
            }

            // Move all auctions from source to target category
            foreach (var auction in sourceCategory.Auctions)
            {
                auction.CategoryId = targetCategory.Id;
            }

            // Delete the source category
            _context.Categories.Remove(sourceCategory);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Categories merged successfully.";
            return RedirectToAction(nameof(Categories));
        }

        public async Task<IActionResult> Reports()
        {
            var reports = await _context.AuctionReports
                .Include(r => r.Auction)
                .Include(r => r.Reporter)
                .OrderByDescending(r => r.ReportedAt)
                .ToListAsync();

            return View(reports);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteReport(int id)
        {
            var report = await _context.AuctionReports.FindAsync(id);
            if (report == null)
            {
                return NotFound();
            }

            _context.AuctionReports.Remove(report);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Report deleted successfully.";
            return RedirectToAction(nameof(Reports));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResolveReport(int id)
        {
            var report = await _context.AuctionReports.FindAsync(id);
            if (report == null)
            {
                return NotFound();
            }

            report.IsResolved = true;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Report marked as resolved successfully.";
            return RedirectToAction(nameof(Reports));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnresolveReport(int id)
        {
            var report = await _context.AuctionReports.FindAsync(id);
            if (report == null)
            {
                return NotFound();
            }

            report.IsResolved = false;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Report marked as unresolved.";
            return RedirectToAction(nameof(Reports));
        }

        public async Task<IActionResult> EditCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            var viewModel = new CategoryViewModel
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                ExistingImagePath = category.ImagePath
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCategory(CategoryViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var category = await _context.Categories.FindAsync(model.Id);
            if (category == null)
            {
                return NotFound();
            }

            category.Name = model.Name;
            category.Description = model.Description;

            if (model.Image != null && model.Image.Length > 0)
            {
                // Validate file type
                var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif" };
                if (!allowedTypes.Contains(model.Image.ContentType.ToLower()))
                {
                    ModelState.AddModelError("Image", "Only JPG, PNG, and GIF images are allowed.");
                    return View(model);
                }

                // Validate file size (5MB limit)
                if (model.Image.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("Image", "Image size should not exceed 5MB.");
                    return View(model);
                }

                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "categories");
                Directory.CreateDirectory(uploadsFolder);

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(model.Image.FileName)}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.Image.CopyToAsync(stream);
                }

                // Delete old image if it exists
                if (!string.IsNullOrEmpty(category.ImagePath))
                {
                    var oldImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", category.ImagePath.TrimStart('/'));
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                category.ImagePath = $"/images/categories/{fileName}";
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Category updated successfully.";
            return RedirectToAction(nameof(Categories));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories
                .Include(c => c.Auctions)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
            {
                return NotFound();
            }

            if (category.Auctions.Any())
            {
                TempData["Error"] = "Cannot delete category with existing auctions. Please merge or reassign the auctions first.";
                return RedirectToAction(nameof(Categories));
            }

            // Delete category image if it exists
            if (!string.IsNullOrEmpty(category.ImagePath))
            {
                var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", category.ImagePath.TrimStart('/'));
                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Category deleted successfully.";
            return RedirectToAction(nameof(Categories));
        }

        public async Task<IActionResult> Auctions()
        {
            var auctions = await _context.Auctions
                .Include(a => a.Seller)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
            return View(auctions);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleFeatured(int id)
        {
            var auction = await _context.Auctions.FindAsync(id);
            if (auction == null)
            {
                return NotFound();
            }

            auction.IsFeatured = !auction.IsFeatured;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Auction '{(auction.Title.Length > 20 ? auction.Title.Substring(0, 20) + "..." : auction.Title)}' has been {(auction.IsFeatured ? "featured" : "un-featured")}.";
            return RedirectToAction(nameof(Auctions));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var auction = await _context.Auctions.FindAsync(id);
            if (auction == null)
            {
                return NotFound();
            }

            auction.IsActive = !auction.IsActive;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Auction '{(auction.Title.Length > 20 ? auction.Title.Substring(0, 20) + "..." : auction.Title)}' has been {(auction.IsActive ? "activated" : "deactivated")}.";
            return RedirectToAction(nameof(Auctions));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PromoteToModerator(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }
            if (!await _userManager.IsInRoleAsync(user, "Moderator"))
            {
                await _userManager.AddToRoleAsync(user, "Moderator");
                TempData["Success"] = "User promoted to Moderator.";
            }
            else
            {
                TempData["Success"] = "User is already a Moderator.";
            }
            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PromoteToAdmin(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }
            if (!await _userManager.IsInRoleAsync(user, "Admin"))
            {
                await _userManager.AddToRoleAsync(user, "Admin");
                TempData["Success"] = "User promoted to Admin.";
            }
            else
            {
                TempData["Success"] = "User is already an Admin.";
            }
            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleRole(string userId, string role, bool? isInRole)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }
            if (isInRole == true)
            {
                if (!await _userManager.IsInRoleAsync(user, role))
                {
                    await _userManager.AddToRoleAsync(user, role);
                    TempData["Success"] = $"User promoted to {role}.";
                }
            }
            else
            {
                if (await _userManager.IsInRoleAsync(user, role))
                {
                    await _userManager.RemoveFromRoleAsync(user, role);
                    TempData["Success"] = $"User demoted from {role}.";
                }
            }
            return RedirectToAction(nameof(Users));
        }

        // Add this endpoint to allow reseeding demo users and auctions
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReseedDemoData()
        {
            await DbInitializer.SeedDemoUsersAndAuctionsAsync(HttpContext.RequestServices);
            TempData["Success"] = "Demo users and auctions reseeded successfully.";
            return RedirectToAction("Index");
        }
    }
} 