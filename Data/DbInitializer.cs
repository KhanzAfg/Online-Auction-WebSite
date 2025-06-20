using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OnlineAuction.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace OnlineAuction.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var webHostEnvironment = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();

            // Ensure database is created
            await context.Database.EnsureCreatedAsync();

            // Create Admin role if it doesn't exist
            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                await roleManager.CreateAsync(new IdentityRole("Admin"));
            }

            // Create Moderator role if it doesn't exist
            if (!await roleManager.RoleExistsAsync("Moderator"))
            {
                await roleManager.CreateAsync(new IdentityRole("Moderator"));
            }

            // Create User role if it doesn't exist
            if (!await roleManager.RoleExistsAsync("User"))
            {
                await roleManager.CreateAsync(new IdentityRole("User"));
            }

            // Create Admin user if it doesn't exist
            var adminEmail = "admin@example.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    Rating = 5.0m,
                    FullName = "Admin User"
                };

                var result = await userManager.CreateAsync(adminUser, "Admin123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            // Ensure Admin user has the Admin role
            adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser != null && !await userManager.IsInRoleAsync(adminUser, "Admin"))
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }

            // Seed categories if they don't exist
            if (!context.Categories.Any())
            {
                var categories = new Category[]
                {
                    new Category
                    {
                        Name = "Electronics",
                        Description = "Latest gadgets, computers, and electronic devices",
                        ImagePath = "https://images.unsplash.com/photo-1498049794561-7780e7231661?w=800&auto=format&fit=crop&q=80"
                    },
                    new Category
                    {
                        Name = "Fashion",
                        Description = "Clothing, accessories, and fashion items",
                        ImagePath = "https://images.unsplash.com/photo-1445205170230-053b83016050?w=800&auto=format&fit=crop&q=80"
                    },
                    new Category
                    {
                        Name = "Home & Garden",
                        Description = "Furniture, decor, and garden items",
                        ImagePath = "https://images.unsplash.com/photo-1484154218962-a197022b5858?w=800&auto=format&fit=crop&q=80"
                    },
                    new Category
                    {
                        Name = "Sports",
                        Description = "Sports equipment and memorabilia",
                        ImagePath = "https://images.unsplash.com/photo-1461896836934-ffe607ba8211?w=800&auto=format&fit=crop&q=80"
                    },
                    new Category
                    {
                        Name = "Books",
                        Description = "Books, comics, and collectible literature",
                        ImagePath = "https://images.unsplash.com/photo-1495446815901-a7297e633e8d?w=800&auto=format&fit=crop&q=80"
                    },
                    new Category
                    {
                        Name = "Art",
                        Description = "Paintings, sculptures, and art collectibles",
                        ImagePath = "https://images.unsplash.com/photo-1579783902614-a3fb3927b6a5?w=800&auto=format&fit=crop&q=80"
                    },
                    new Category
                    {
                        Name = "Jewelry",
                        Description = "Fine jewelry, watches, and accessories",
                        ImagePath = "https://images.unsplash.com/photo-1573408301185-9146fe634ad0?w=800&auto=format&fit=crop&q=80"
                    },
                    new Category
                    {
                        Name = "Toys & Games",
                        Description = "Vintage toys, board games, and collectibles",
                        ImagePath = "https://images.unsplash.com/photo-1566576912321-d58ddd7a6088?w=800&auto=format&fit=crop&q=80"
                    }
                };

                await context.Categories.AddRangeAsync(categories);
                await context.SaveChangesAsync();
            }

            // Create test user if none exists
            if (!context.Users.Any())
            {
                var testUser = new ApplicationUser
                {
                    UserName = "test@example.com",
                    Email = "test@example.com",
                    Rating = 5.0m,
                    FullName = "Test User"
                };

                var result = await userManager.CreateAsync(testUser, "Test123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(testUser, "User");
                    // Seed random auctions for the test user if no auctions exist
                    if (!context.Auctions.Any())
                    {
                         await SeedRandomAuctionsAsync(context, testUser.Id, webHostEnvironment.WebRootPath);
                    }
                }
            }
             // Seed random auctions if no auctions exist (for subsequent runs or if users existed initially)
            if (!context.Auctions.Any())
            {
                 var testUser = await userManager.FindByEmailAsync("test@example.com");
                 if (testUser != null)
                 {
                    await SeedRandomAuctionsAsync(context, testUser.Id, webHostEnvironment.WebRootPath);
                 }
                 // Fallback if test user doesn't exist - find any user or create a temporary one
                 else
                 {
                    var anyUser = await userManager.Users.FirstOrDefaultAsync();
                    if (anyUser != null)
                    {
                        await SeedRandomAuctionsAsync(context, anyUser.Id, webHostEnvironment.WebRootPath);
                    }
                    else
                    {
                         // This case should ideally not happen if role seeding is done first, but for robustness:
                        var tempUser = new ApplicationUser
                        {
                            UserName = "temp_seeder@example.com",
                            Email = "temp_seeder@example.com",
                            EmailConfirmed = true,
                            Rating = 5.0m,
                            FullName = "Temp Seeder"
                        };
                         await userManager.CreateAsync(tempUser, "Temp123!");
                         await userManager.AddToRoleAsync(tempUser, "User");
                         await SeedRandomAuctionsAsync(context, tempUser.Id, webHostEnvironment.WebRootPath);
                    }
                 }
            }
        }

        private static async Task SeedRandomAuctionsAsync(ApplicationDbContext context, string sellerId, string webRootPath)
        {
            var random = new Random();
            var categories = await context.Categories.ToListAsync();
            var uploadsFolder = Path.Combine(webRootPath, "uploads");

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            // Sample data for each category with relevant images
            var auctionData = new Dictionary<string, (string Title, string Description, decimal MinPrice, string ImagePath)[]>
            {
                ["Electronics"] = new[]
                {
                    ("iPhone 13 Pro", "256GB, Space Gray, includes original accessories", 800m, "/sample-images/electronics_1.jpg"),
                    ("Samsung 4K Smart TV", "55-inch, HDR, built-in streaming apps", 600m, "/sample-images/electronics_2.jpg"),
                    ("MacBook Pro M1", "16GB RAM, 512GB SSD, perfect condition", 1200m, "/sample-images/electronics_3.jpg")
                },
                ["Fashion"] = new[]
                {
                    ("Designer Handbag", "Louis Vuitton Neverfull, authentic", 1200m, "/sample-images/fashion_1.jpg"),
                    ("Rolex Watch", "Datejust 41, stainless steel", 8000m, "/sample-images/fashion_2.jpg"),
                    ("Designer Suit", "Tom Ford, wool blend, size 42", 1500m, "/sample-images/fashion_3.jpg")
                },
                ["Home & Garden"] = new[]
                {
                    ("Modern Sofa Set", "3-piece sectional, gray fabric", 1200m, "/sample-images/home & garden_1.jpg"),
                    ("Kitchen Appliances", "Complete set of high-end appliances", 2000m, "/sample-images/home & garden_2.jpg"),
                    ("Garden Furniture", "Wicker set with cushions, 6 pieces", 800m, "/sample-images/home & garden_3.jpg")
                },
                ["Sports"] = new[]
                {
                    ("Mountain Bike", "Trek, full suspension, 29-inch wheels", 1500m, "/sample-images/sports_1.jpg"),
                    ("Golf Clubs Set", "Callaway, complete set with bag", 800m, "/sample-images/sports_2.jpg"),
                    ("Tennis Racket", "Wilson Pro Staff, like new", 200m, "/sample-images/sports_3.jpg")
                },
                ["Books"] = new[]
                {
                    ("First Edition Collection", "Classic novels, excellent condition", 2000m, "/sample-images/books_1.jpg"),
                    ("Signed Books", "Collection of signed first editions", 1500m, "/sample-images/books_2.jpg"),
                    ("Rare Manuscripts", "Historical documents, authenticated", 3000m, "/sample-images/books_3.jpg")
                },
                ["Art"] = new[]
                {
                    ("Original Painting", "Contemporary abstract, large canvas", 3000m, "/sample-images/art_1.jpg"),
                    ("Limited Edition Print", "Numbered, signed by artist", 800m, "/sample-images/art_2.jpg"),
                    ("Sculpture", "Bronze, modern design", 2000m, "/sample-images/art_3.jpg")
                },
                ["Jewelry"] = new[]
                {
                    ("Diamond Ring", "2 carat, VS1 clarity, G color", 5000m, "/sample-images/jewelry_1.jpg"),
                    ("Pearl Necklace", "South Sea pearls, 18K gold clasp", 2000m, "/sample-images/jewelry_2.jpg"),
                    ("Vintage Brooch", "Art Deco style, sapphire and diamond", 1500m, "/sample-images/jewelry_3.jpg")
                },
                ["Toys & Games"] = new[]
                {
                    ("Vintage Board Games", "Complete collection, excellent condition", 500m, "/sample-images/toysgames_1.jpg"),
                    ("Collectible Action Figures", "Rare set, mint in box", 800m, "/sample-images/toysgames_2.jpg"),
                    ("LEGO Star Wars Set", "Complete Millennium Falcon, sealed", 300m, "/sample-images/toysgames_3.jpg")
                }
            };

            foreach (var category in categories)
            {
                if (auctionData.TryGetValue(category.Name, out var items))
                {
                    foreach (var item in items)
                    {
                        var auction = new Auction
                        {
                            Title = item.Title,
                            Description = item.Description,
                            MinimumBidPrice = item.MinPrice,
                            StartDate = DateTime.UtcNow.AddDays(-random.Next(1, 5)),
                            EndDate = DateTime.UtcNow.AddDays(random.Next(5, 15)),
                            Status = AuctionStatus.Active,
                            CategoryId = category.Id,
                            SellerId = sellerId,
                            Images = new List<AuctionImage>()
                        };

                        // Add the main image
                        auction.Images.Add(new AuctionImage
                        {
                            ImagePath = item.ImagePath,
                            IsMainImage = true
                        });

                        context.Auctions.Add(auction);
                    }
                }
            }

            await context.SaveChangesAsync();
        }
    }
} 