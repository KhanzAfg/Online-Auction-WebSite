using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineAuction.Data;
using OnlineAuction.Models;
using OnlineAuction.ViewModels;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.IO;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace OnlineAuction.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<HomeController> _logger;

        public HomeController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<HomeController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IActionResult> Index(int page = 1)
        {
            var pageSize = 8;
            var totalAuctions = await _context.Auctions.CountAsync(a => a.IsActive);
            
            var auctions = await _context.Auctions
                .Where(a => a.IsActive)
                .Include(a => a.Seller)
                .Include(a => a.Images)
                .Include(a => a.Bids)
                .OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            
            var featuredAuctions = await _context.Auctions
                .Where(a => a.IsFeatured && a.IsActive)
                .Include(a => a.Images)
                .OrderByDescending(a => a.EndTime)
                .Take(10)
                .ToListAsync();

            var categories = await _context.Categories.ToListAsync();

            var viewModel = new HomePageViewModel
            {
                Auctions = auctions,
                FeaturedAuctions = featuredAuctions,
                Categories = categories,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(totalAuctions / (double)pageSize)
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Search(string? searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return RedirectToAction(nameof(Index));

            searchTerm = searchTerm.ToLower();
            var auctions = await _context.Auctions
                .Include(a => a.Category)
                .Include(a => a.Seller)
                .Include(a => a.Images)
                .Where(a => a.Status == AuctionStatus.Active &&
                           (EF.Functions.Like(a.Title.ToLower(), $"%{searchTerm}%") || 
                            EF.Functions.Like(a.Description.ToLower(), $"%{searchTerm}%") ||
                            EF.Functions.Like(a.Category.Name.ToLower(), $"%{searchTerm}%")))
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            ViewData["CurrentFilter"] = searchTerm;
            return View("SearchResults", auctions);
        }

        public async Task<IActionResult> AdvancedSearch(SearchViewModel model)
        {
            var query = _context.Auctions
                .Include(a => a.Category)
                .Include(a => a.Seller)
                .Include(a => a.Images)
                .Include(a => a.Bids)
                .AsQueryable();

            // Apply search term filter
            if (!string.IsNullOrWhiteSpace(model.SearchTerm))
            {
                var searchTerm = model.SearchTerm.ToLower();
                query = query.Where(a => 
                    EF.Functions.Like(a.Title.ToLower(), $"%{searchTerm}%") ||
                    EF.Functions.Like(a.Description.ToLower(), $"%{searchTerm}%") ||
                    EF.Functions.Like(a.Category.Name.ToLower(), $"%{searchTerm}%"));
            }

            // Apply category filter
            if (model.CategoryId.HasValue)
            {
                query = query.Where(a => a.CategoryId == model.CategoryId.Value);
            }

            // Apply price filters
            if (model.MinPrice.HasValue)
            {
                query = query.Where(a => a.StartingPrice >= model.MinPrice.Value);
            }

            if (model.MaxPrice.HasValue)
            {
                query = query.Where(a => a.StartingPrice <= model.MaxPrice.Value);
            }

            // Apply date filters
            if (model.StartDate.HasValue)
            {
                query = query.Where(a => a.StartDate >= model.StartDate.Value);
            }

            if (model.EndDate.HasValue)
            {
                query = query.Where(a => a.EndDate <= model.EndDate.Value);
            }

            // Apply status filters
            if (model.ShowActiveOnly)
            {
                query = query.Where(a => a.Status == AuctionStatus.Active && a.EndDate > DateTime.UtcNow);
            }
            else if (model.ShowEnded)
            {
                query = query.Where(a => a.Status == AuctionStatus.Ended || a.EndDate <= DateTime.UtcNow);
            }

            // Apply sorting
            query = model.SortBy switch
            {
                "price" => model.SortOrder == "asc" 
                    ? query.OrderBy(a => a.StartingPrice)
                    : query.OrderByDescending(a => a.StartingPrice),
                "title" => model.SortOrder == "asc"
                    ? query.OrderBy(a => a.Title)
                    : query.OrderByDescending(a => a.Title),
                "endDate" => model.SortOrder == "asc"
                    ? query.OrderBy(a => a.EndDate)
                    : query.OrderByDescending(a => a.EndDate),
                "bids" => model.SortOrder == "asc"
                    ? query.OrderBy(a => a.Bids.Count)
                    : query.OrderByDescending(a => a.Bids.Count),
                _ => model.SortOrder == "asc"
                    ? query.OrderBy(a => a.CreatedAt)
                    : query.OrderByDescending(a => a.CreatedAt)
            };

            // Apply pagination
            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / model.PageSize);
            var auctions = await query
                .Skip((model.Page - 1) * model.PageSize)
                .Take(model.PageSize)
                .ToListAsync();

            // Prepare view data
            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name");
            ViewBag.TotalCount = totalCount;
            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentPage = model.Page;

            return View("AdvancedSearch", new { Auctions = auctions, SearchModel = model });
        }

        [Authorize]
        public IActionResult SellAuction()
        {
            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name");
            return View();
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SellAuction(SellAuctionViewModel model)
        {
            _logger.LogInformation("SellAuction POST method called");
            
            if (!ModelState.IsValid)
            {
                _logger.LogWarning($"ModelState is invalid: {string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage))}");
                ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name");
                return View(model);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            _logger.LogInformation($"Creating auction: Title='{model.Title}'");

            try
            {
                var auction = new Auction
                {
                    Title = model.Title,
                    Description = model.Description,
                    StartingPrice = model.StartingPrice,
                    MinimumBidPrice = model.MinimumBidPrice,
                    BidIncrement = model.BidIncrement,
                    StartDate = model.StartDate,
                    EndDate = model.EndDate,
                    CategoryId = model.CategoryId,
                    SellerId = userId,
                    Status = AuctionStatus.Active,
                    Images = []
                };

                _logger.LogInformation($"Auction object created: EndDate={auction.EndDate}");

                // Handle specification document upload (optional)
                if (model.SpecificationDocument != null && model.SpecificationDocument.Length > 0)
                {
                    _logger.LogInformation("Processing specification document");
                    if (model.SpecificationDocument.Length > 10 * 1024 * 1024) // 10MB limit
                    {
                        ModelState.AddModelError("SpecificationDocument", "Document size should not exceed 10MB.");
                        ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name");
                        return View(model);
                    }

                    var allowedDocTypes = new[] { "application/pdf", "application/msword", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" };
                    if (!allowedDocTypes.Contains(model.SpecificationDocument.ContentType.ToLower()))
                    {
                        ModelState.AddModelError("SpecificationDocument", "Only PDF and Word documents are allowed.");
                        ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name");
                        return View(model);
                    }

                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "documents", "auctions");
                    Directory.CreateDirectory(uploadsFolder);

                    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(model.SpecificationDocument.FileName)}";
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.SpecificationDocument.CopyToAsync(stream);
                    }

                    auction.SpecificationDocumentPath = $"/documents/auctions/{fileName}";
                }

                // Handle images (optional)
                if (model.Images?.Count > 0)
                {
                    _logger.LogInformation($"Processing {model.Images.Count} images");
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "auctions");
                    Directory.CreateDirectory(uploadsFolder);

                    foreach (var image in model.Images)
                    {
                        if (image.Length > 0)
                        {
                            if (!IsValidImageType(image))
                            {
                                ModelState.AddModelError("Images", $"Unsupported image type: {image.ContentType}. Supported formats: JPG, PNG, GIF, WebP, BMP, TIFF, SVG");
                                ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name");
                                return View(model);
                            }

                            if (image.Length > 5 * 1024 * 1024) // 5MB limit
                            {
                                ModelState.AddModelError("Images", $"Image '{image.FileName}' is too large. Maximum size: 5MB");
                                ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name");
                                return View(model);
                            }

                            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(image.FileName)}";
                            var filePath = Path.Combine(uploadsFolder, fileName);

                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await image.CopyToAsync(stream);
                            }

                            auction.Images.Add(new AuctionImage
                            {
                                ImagePath = $"/images/auctions/{fileName}",
                                IsMainImage = auction.Images.Count == 0
                            });
                        }
                    }
                }

                _logger.LogInformation("About to save auction to database");
                _context.Auctions.Add(auction);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Auction created successfully: ID={auction.Id}, Title='{auction.Title}'");

                TempData["Success"] = $"Auction '{auction.Title}' created successfully! Auction ID: {auction.Id}";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating auction");
                TempData["Error"] = "An error occurred while creating the auction. Please try again.";
                ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name");
                return View(model);
            }
        }

        private static bool IsValidImageType(IFormFile file)
        {
            var allowedTypes = new[] { 
                "image/jpeg", 
                "image/png", 
                "image/gif", 
                "image/webp", 
                "image/bmp", 
                "image/tiff", 
                "image/svg+xml" 
            };
            return allowedTypes.Contains(file.ContentType.ToLower());
        }

        public async Task<IActionResult> Category(int id, int page = 1)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            var pageSize = 8;
            var totalAuctions = await _context.Auctions.CountAsync(a => a.CategoryId == id && a.IsActive);
            
            var auctions = await _context.Auctions
                .Where(a => a.CategoryId == id && a.IsActive)
                .Include(a => a.Seller)
                .Include(a => a.Images)
                .Include(a => a.Bids)
                .OrderByDescending(a => a.IsFeatured) // Featured items first
                .ThenByDescending(a => a.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var viewModel = new CategoryViewModel
            {
                Category = category,
                Auctions = auctions,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(totalAuctions / (double)pageSize)
            };

            return View(viewModel);
        }

        public async Task<IActionResult> AuctionDetails(int id)
        {
            var auction = await _context.Auctions
                .Include(a => a.Category)
                .Include(a => a.Seller)
                .Include(a => a.Images)
                .Include(a => a.Bids)
                    .ThenInclude(b => b.Bidder)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (auction == null)
            {
                return NotFound();
            }

            return View(auction);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> PlaceBid(int auctionId, decimal amount)
        {
            var auction = await _context.Auctions
                .Include(a => a.Bids)
                .FirstOrDefaultAsync(a => a.Id == auctionId);

            if (auction == null)
                return NotFound();

            if (auction.Status != AuctionStatus.Active || auction.EndDate < DateTime.UtcNow)
                return BadRequest("This auction has ended.");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            // Check if user is trying to bid on their own auction
            if (auction.SellerId == userId)
            {
                TempData["Error"] = "You cannot bid on your own auction";
                return RedirectToAction(nameof(AuctionDetails), new { id = auctionId });
            }

            // Get current highest bid
            var currentHighestBid = auction.Bids.OrderByDescending(b => b.Amount).FirstOrDefault();
            var minimumBidAmount = currentHighestBid?.Amount + auction.BidIncrement ?? auction.StartingPrice;

            // Validate bid amount
            if (amount < minimumBidAmount)
            {
                TempData["Error"] = $"Your bid must be at least ${minimumBidAmount:F2}";
                return RedirectToAction(nameof(AuctionDetails), new { id = auctionId });
            }

            // Check if user is trying to outbid themselves
            if (currentHighestBid?.BidderId == userId)
            {
                TempData["Error"] = "You already have the highest bid";
                return RedirectToAction(nameof(AuctionDetails), new { id = auctionId });
            }

            var bid = new Bid
            {
                AuctionId = auctionId,
                BidderId = userId,
                Amount = amount,
                BidTime = DateTime.UtcNow
            };

            _context.Bids.Add(bid);
            await _context.SaveChangesAsync();

            // Notify the auction seller
            var notification = new Notification
            {
                UserId = auction.SellerId,
                Title = "New Bid Placed!",
                Message = $"A new bid of ${amount:F2} has been placed on your auction: \'{auction.Title}\'.",
                Type = NotificationType.Bid,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                RelatedEntityId = auctionId
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // Add success message
            TempData["Success"] = "Your bid has been placed successfully!";

            return RedirectToAction(nameof(AuctionDetails), new { id = auctionId });
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public async Task<IActionResult> Categories()
        {
            var categories = await _context.Categories
                .Include(c => c.Auctions.Where(a => a.Status == AuctionStatus.Active))
                .ToListAsync();

            return View(categories);
        }

        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _context.Categories
                .Select(c => new { c.Id, c.Name })
                .OrderBy(c => c.Name)
                .ToListAsync();

            return Json(categories);
        }

        [Authorize]
        public async Task<IActionResult> MyAuctions()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var auctions = await _context.Auctions
                .Include(a => a.Category)
                .Include(a => a.Images)
                .Where(a => a.SellerId == userId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
            return View(auctions);
        }

        [Authorize]
        public async Task<IActionResult> MyBids()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var bids = await _context.Bids
                .Include(b => b.Auction)
                    .ThenInclude(a => a.Images)
                .Include(b => b.Auction)
                    .ThenInclude(a => a.Bids)
                .Where(b => b.BidderId == userId)
                .OrderByDescending(b => b.BidTime)
                .ToListAsync();
            return View(bids);
        }

        [Authorize]
        public async Task<IActionResult> EditAuction(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var auction = await _context.Auctions
                .Include(a => a.Images)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (auction == null)
            {
                return NotFound();
            }

            // Check if user is the seller or an Admin
            if (auction.SellerId != userId && !User.IsInRole("Admin"))
            {
                return Forbid(); // Or Redirect to Access Denied
            }

            // Check if auction is still active (Admins can edit inactive auctions)
            if (auction.Status != AuctionStatus.Active && !User.IsInRole("Admin"))
            {
                TempData["Error"] = "You can only edit active auctions";
                return RedirectToAction(nameof(AuctionDetails), new { id = id });
            }

            var viewModel = new EditAuctionViewModel
            {
                Id = auction.Id,
                Title = auction.Title,
                Description = auction.Description,
                MinimumBidPrice = auction.MinimumBidPrice,
                BidIncrement = auction.BidIncrement,
                EndDate = auction.EndDate,
                CategoryId = auction.CategoryId,
                CurrentImages = auction.Images.ToList(),
                CurrentSpecificationDocumentPath = auction.SpecificationDocumentPath
            };

            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name");
            return View(viewModel);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAuction(EditAuctionViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name");
                return View(model);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var auction = await _context.Auctions
                .Include(a => a.Images)
                .FirstOrDefaultAsync(a => a.Id == model.Id);

            if (auction == null)
            {
                return NotFound();
            }

            // Check if user is the seller or an Admin
            if (auction.SellerId != userId && !User.IsInRole("Admin"))
            {
                return Forbid(); // Or handle appropriately
            }

            // Check if auction is still active (Admins can edit inactive auctions)
            if (auction.Status != AuctionStatus.Active && !User.IsInRole("Admin"))
            {
                TempData["Error"] = "You can only edit active auctions";
                return RedirectToAction(nameof(AuctionDetails), new { id = model.Id });
            }

            try
            {
                auction.Title = model.Title;
                auction.Description = model.Description;
                auction.MinimumBidPrice = model.MinimumBidPrice;
                auction.BidIncrement = model.BidIncrement;
                auction.EndDate = model.EndDate;
                auction.CategoryId = model.CategoryId;

                // Handle specification document upload
                if (model.SpecificationDocument != null && model.SpecificationDocument.Length > 0)
                {
                    if (model.SpecificationDocument.Length > 10 * 1024 * 1024) // 10MB limit
                    {
                        ModelState.AddModelError("SpecificationDocument", "Document size should not exceed 10MB.");
                        ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name");
                        return View(model);
                    }

                    var allowedDocTypes = new[] { "application/pdf", "application/msword", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" };
                    if (!allowedDocTypes.Contains(model.SpecificationDocument.ContentType.ToLower()))
                    {
                        ModelState.AddModelError("SpecificationDocument", "Only PDF and Word documents are allowed.");
                        ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name");
                        return View(model);
                    }

                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "documents", "auctions");
                    Directory.CreateDirectory(uploadsFolder);

                    // Delete old document if exists
                    if (!string.IsNullOrEmpty(auction.SpecificationDocumentPath))
                    {
                        var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", auction.SpecificationDocumentPath.TrimStart('/'));
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }

                    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(model.SpecificationDocument.FileName)}";
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.SpecificationDocument.CopyToAsync(stream);
                    }

                    auction.SpecificationDocumentPath = $"/documents/auctions/{fileName}";
                }

                // Handle new images if any
                if (model.NewImages?.Count > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "auctions");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    foreach (var image in model.NewImages)
                    {
                        if (image != null && image.Length > 0)
                        {
                            if (!IsValidImageType(image))
                            {
                                ModelState.AddModelError("NewImages", $"Invalid image type: {image.ContentType}. Allowed types: JPEG, PNG, GIF");
                                ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name");
                                return View(model);
                            }

                            if (image.Length > 5 * 1024 * 1024) // 5MB
                            {
                                ModelState.AddModelError("NewImages", "Image size must be less than 5MB");
                                ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name");
                                return View(model);
                            }

                            var uniqueFileName = $"{Guid.NewGuid()}_{image.FileName}";
                            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await image.CopyToAsync(stream);
                            }

                            var auctionImage = new AuctionImage
                            {
                                ImagePath = $"/images/auctions/{uniqueFileName}",
                                IsMainImage = auction.Images.Count == 0
                            };

                            auction.Images.Add(auctionImage);
                        }
                    }
                }

                // Handle image deletions
                if (model.ImagesToDelete?.Count > 0)
                {
                    foreach (var imageId in model.ImagesToDelete)
                    {
                        var image = auction.Images.FirstOrDefault(i => i.Id == imageId);
                        if (image != null)
                        {
                            // Delete the file
                            var filePath = Path.Combine("wwwroot", image.ImagePath.TrimStart('/'));
                            if (System.IO.File.Exists(filePath))
                            {
                                System.IO.File.Delete(filePath);
                            }

                            _context.AuctionImages.Remove(image);
                        }
                    }
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Auction updated successfully!";
                return RedirectToAction(nameof(AuctionDetails), new { id = auction.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating auction");
                ModelState.AddModelError("", "An error occurred while updating the auction. Please try again.");
                ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name");
                return View(model);
            }
        }

        [Authorize]
        public async Task<IActionResult> ReportAuction(int id)
        {
            var auction = await _context.Auctions
                .Include(a => a.Seller)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (auction == null)
            {
                return NotFound();
            }

            // Prevent sellers from reporting their own auctions
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            if (auction.SellerId == userId)
            {
                TempData["Error"] = "You cannot report your own auction";
                return RedirectToAction(nameof(AuctionDetails), new { id });
            }

            return View(auction);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReportAuction(int auctionId, string reason)
        {
            var auction = await _context.Auctions
                .Include(a => a.Seller)
                .FirstOrDefaultAsync(a => a.Id == auctionId);

            if (auction?.Seller == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            if (auction.SellerId == userId)
            {
                return BadRequest("You cannot report your own auction.");
            }

            var report = new AuctionReport
            {
                AuctionId = auctionId,
                ReporterId = userId,
                Reason = reason,
                ReportedAt = DateTime.UtcNow
            };

            _context.AuctionReports.Add(report);

            // Notify the auction seller (existing notification)
            var notificationToSeller = new Notification
            {
                UserId = auction.SellerId,
                Title = "Auction Reported",
                Message = $"Your auction \'{auction.Title}\' has been reported. Reason: {reason}",
                Type = NotificationType.Message,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                RelatedEntityId = auctionId
            };
            _context.Notifications.Add(notificationToSeller);

            // Notify Admins about the reported auction
            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            foreach (var admin in admins)
            {
                var notificationToAdmin = new Notification
                {
                    UserId = admin.Id,
                    Title = "Auction Reported for Review",
                    Message = $"Auction \'{auction.Title}\' (ID: {auctionId}) has been reported. Reason: {reason}",
                    Type = NotificationType.Auction,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow,
                    RelatedEntityId = auctionId
                };
                _context.Notifications.Add(notificationToAdmin);
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(AuctionDetails), new { id = auctionId });
        }

        public async Task<IActionResult> SellerProfile(string id)
        {
            var seller = await _context.Users
                .Include(u => u.Auctions)
                    .ThenInclude(a => a.Bids)
                .Include(u => u.Bids)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (seller == null)
            {
                return NotFound();
            }

            return View(seller);
        }

        [Authorize]
        public async Task<IActionResult> RateUser(int auctionId, string userId, bool isRatingBuyer)
        {
            var auction = await _context.Auctions
                .Include(a => a.Seller)
                .Include(a => a.Bids)
                    .ThenInclude(b => b.Bidder)
                .FirstOrDefaultAsync(a => a.Id == auctionId);

            if (auction == null)
            {
                return NotFound();
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId))
            {
                return RedirectToAction("Login", "Account");
            }

            // Verify that the current user is either the seller or the winning bidder
            var winningBid = auction.Bids.OrderByDescending(b => b.Amount).FirstOrDefault();
            if (winningBid == null)
            {
                return BadRequest("This auction has no winning bidder.");
            }

            if (isRatingBuyer)
            {
                // Only seller can rate buyer
                if (auction.SellerId != currentUserId)
                {
                    return Forbid();
                }
                // Verify that the user being rated is the winning bidder
                if (winningBid.BidderId != userId)
                {
                    return BadRequest("Invalid user to rate.");
                }
            }
            else
            {
                // Only winning bidder can rate seller
                if (winningBid.BidderId != currentUserId)
                {
                    return Forbid();
                }
                // Verify that the user being rated is the seller
                if (auction.SellerId != userId)
                {
                    return BadRequest("Invalid user to rate.");
                }
            }

            // Check if rating already exists
            var existingRating = await _context.Ratings
                .FirstOrDefaultAsync(r => r.AuctionId == auctionId && r.RaterUserId == currentUserId && r.RatedUserId == userId);

            if (existingRating != null)
            {
                TempData["Error"] = "You have already rated this user for this auction.";
                return RedirectToAction(nameof(AuctionDetails), new { id = auctionId });
            }

            var userToRate = await _userManager.FindByIdAsync(userId);
            if (userToRate == null)
            {
                return NotFound();
            }

            var viewModel = new RateUserViewModel
            {
                AuctionId = auctionId,
                AuctionTitle = auction.Title,
                UserToRateId = userId,
                UserToRateName = userToRate.UserName ?? "Unknown User",
                IsRatingBuyer = isRatingBuyer
            };

            return View(viewModel);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RateUser(RateUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var auction = await _context.Auctions
                .Include(a => a.Seller)
                .Include(a => a.Bids)
                    .ThenInclude(b => b.Bidder)
                .FirstOrDefaultAsync(a => a.Id == model.AuctionId);

            if (auction == null)
            {
                return NotFound();
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId))
            {
                return RedirectToAction("Login", "Account");
            }

            // Verify that the current user is either the seller or the winning bidder
            var winningBid = auction.Bids.OrderByDescending(b => b.Amount).FirstOrDefault();
            if (winningBid == null)
            {
                return BadRequest("This auction has no winning bidder.");
            }

            if (model.IsRatingBuyer)
            {
                // Only seller can rate buyer
                if (auction.SellerId != currentUserId)
                {
                    return Forbid();
                }
                // Verify that the user being rated is the winning bidder
                if (winningBid.BidderId != model.UserToRateId)
                {
                    return BadRequest("Invalid user to rate.");
                }
            }
            else
            {
                // Only winning bidder can rate seller
                if (winningBid.BidderId != currentUserId)
                {
                    return Forbid();
                }
                // Verify that the user being rated is the seller
                if (auction.SellerId != model.UserToRateId)
                {
                    return BadRequest("Invalid user to rate.");
                }
            }

            // Check if rating already exists
            var existingRating = await _context.Ratings
                .FirstOrDefaultAsync(r => r.AuctionId == model.AuctionId && r.RaterUserId == currentUserId && r.RatedUserId == model.UserToRateId);

            if (existingRating != null)
            {
                TempData["Error"] = "You have already rated this user for this auction.";
                return RedirectToAction(nameof(AuctionDetails), new { id = model.AuctionId });
            }

            var userToRate = await _userManager.FindByIdAsync(model.UserToRateId);
            if (userToRate == null)
            {
                return NotFound();
            }

            // Create the rating
            var rating = new Rating
            {
                AuctionId = model.AuctionId,
                RatedUserId = model.UserToRateId,
                RaterUserId = currentUserId,
                Score = model.Score,
                Comment = model.Comment
            };

            _context.Ratings.Add(rating);

            // Update user's rating statistics
            userToRate.TotalRatings++;
            if (model.Score > 0)
            {
                userToRate.PositiveRatings++;
            }
            else if (model.Score < 0)
            {
                userToRate.NegativeRatings++;
            }

            // Calculate new average rating
            var userRatings = await _context.Ratings
                .Where(r => r.RatedUserId == model.UserToRateId)
                .ToListAsync();

            userToRate.Rating = (decimal)userRatings.Average(r => r.Score);

            await _context.SaveChangesAsync();

            // Notify the rated user
            var notification = new Notification
            {
                UserId = model.UserToRateId,
                Title = "New Rating Received",
                Message = $"You received a rating of {model.Score} for the auction '{auction.Title}'.",
                Type = NotificationType.Rating,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                RelatedEntityId = model.AuctionId
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Rating submitted successfully!";
            return RedirectToAction(nameof(AuctionDetails), new { id = model.AuctionId });
        }

        [Authorize]
        public async Task<IActionResult> DownloadSpecification(int auctionId)
        {
            var auction = await _context.Auctions.FindAsync(auctionId);
            if (auction == null || string.IsNullOrEmpty(auction.SpecificationDocumentPath))
            {
                return NotFound();
            }

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", auction.SpecificationDocumentPath.TrimStart('/'));
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound();
            }

            var fileName = Path.GetFileName(auction.SpecificationDocumentPath);
            var contentType = "application/octet-stream";
            if (fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                contentType = "application/pdf";
            }
            else if (fileName.EndsWith(".doc", StringComparison.OrdinalIgnoreCase))
            {
                contentType = "application/msword";
            }
            else if (fileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
            {
                contentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
            }

            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            return File(fileBytes, contentType, fileName);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToWatchlist(int auctionId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var auction = await _context.Auctions.FindAsync(auctionId);
            if (auction == null)
            {
                return NotFound();
            }

            // Check if already in watchlist
            var existingWatchlist = await _context.Watchlist
                .FirstOrDefaultAsync(w => w.UserId == userId && w.AuctionId == auctionId);

            if (existingWatchlist != null)
            {
                TempData["Error"] = "This auction is already in your watchlist.";
                return RedirectToAction(nameof(AuctionDetails), new { id = auctionId });
            }

            var watchlistItem = new Watchlist
            {
                UserId = userId,
                AuctionId = auctionId
            };

            _context.Watchlist.Add(watchlistItem);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Auction added to watchlist successfully!";
            return RedirectToAction(nameof(AuctionDetails), new { id = auctionId });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFromWatchlist(int auctionId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var watchlistItem = await _context.Watchlist
                .FirstOrDefaultAsync(w => w.UserId == userId && w.AuctionId == auctionId);

            if (watchlistItem == null)
            {
                return NotFound();
            }

            _context.Watchlist.Remove(watchlistItem);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Auction removed from watchlist successfully!";
            return RedirectToAction(nameof(MyWatchlist));
        }

        [Authorize]
        public async Task<IActionResult> MyWatchlist()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var watchlistItems = await _context.Watchlist
                .Include(w => w.Auction)
                    .ThenInclude(a => a.Images)
                .Include(w => w.Auction)
                    .ThenInclude(a => a.Category)
                .Include(w => w.Auction)
                    .ThenInclude(a => a.Bids)
                .Where(w => w.UserId == userId)
                .OrderByDescending(w => w.AddedAt)
                .ToListAsync();

            return View(watchlistItems);
        }

        public async Task<IActionResult> BidHistory(int id)
        {
            var auction = await _context.Auctions
                .Include(a => a.Bids)
                    .ThenInclude(b => b.Bidder)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (auction == null)
            {
                return NotFound();
            }

            var currentBid = auction.CurrentBid;
            var bids = auction.Bids.OrderByDescending(b => b.BidTime).ToList();

            // Calculate bidder statistics
            var bidderStats = auction.Bids
                .GroupBy(b => b.BidderId)
                .Select(g => new BidderStatsViewModel
                {
                    BidderId = g.Key,
                    BidderName = g.First().Bidder?.FullName ?? "Unknown User",
                    ProfileImagePath = g.First().Bidder?.ProfileImagePath,
                    TotalBids = g.Count(),
                    HighestBid = g.Max(b => b.Amount),
                    FirstBid = g.Min(b => b.BidTime),
                    LastBid = g.Max(b => b.BidTime),
                    IsCurrentWinner = currentBid != null && g.Key == currentBid.BidderId
                })
                .OrderByDescending(s => s.HighestBid)
                .ToList();

            // Mark outbid bids
            var bidDetails = new List<BidDetailViewModel>();
            var highestBidAmount = 0m;

            foreach (var bid in bids)
            {
                var isOutbid = bid.Amount < highestBidAmount;
                if (bid.Amount > highestBidAmount)
                {
                    highestBidAmount = bid.Amount;
                }

                bidDetails.Add(new BidDetailViewModel
                {
                    Id = bid.Id,
                    Amount = bid.Amount,
                    BidderName = bid.Bidder?.FullName ?? "Unknown User",
                    BidderId = bid.BidderId,
                    ProfileImagePath = bid.Bidder?.ProfileImagePath,
                    BidTime = bid.BidTime,
                    IsWinning = currentBid != null && bid.Id == currentBid.Id,
                    IsOutbid = isOutbid
                });
            }

            var viewModel = new BidHistoryViewModel
            {
                AuctionId = auction.Id,
                AuctionTitle = auction.Title,
                StartingPrice = auction.StartingPrice,
                CurrentBid = currentBid?.Amount ?? auction.StartingPrice,
                TotalBids = auction.Bids.Count,
                UniqueBidders = bidderStats.Count,
                AuctionEndDate = auction.EndDate,
                IsActive = auction.Status == AuctionStatus.Active && auction.EndDate > DateTime.UtcNow,
                Bids = bidDetails,
                BidderStats = bidderStats
            };

            return View(viewModel);
        }

        // Add this test action to check database seeding
        public async Task<IActionResult> TestSeeding()
        {
            var users = await _context.Users.ToListAsync();
            var auctions = await _context.Auctions.ToListAsync();
            var categories = await _context.Categories.ToListAsync();
            
            var result = new
            {
                TotalUsers = users.Count,
                TotalAuctions = auctions.Count,
                TotalCategories = categories.Count,
                DemoUsers = users.Where(u => u.Email.StartsWith("demo_user")).Count(),
                Users = users.Select(u => new { u.Email, u.FullName, u.CreatedAt }).ToList(),
                Auctions = auctions.Select(a => new { a.Title, a.SellerId, a.CreatedAt }).ToList()
            };
            
            return Json(result);
        }
    }
} 