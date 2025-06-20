using Microsoft.EntityFrameworkCore;
using OnlineAuction.Data;
using OnlineAuction.Models;

namespace OnlineAuction.Services
{
    public class AuctionEndCheckService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AuctionEndCheckService> _logger;

        public AuctionEndCheckService(
            IServiceProvider serviceProvider,
            ILogger<AuctionEndCheckService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                        // Find auctions that have just ended
                        var endedAuctions = await context.Auctions
                            .Include(a => a.Seller)
                            .Include(a => a.Bids)
                                .ThenInclude(b => b.Bidder)
                            .Where(a => a.Status == AuctionStatus.Active && a.EndDate <= DateTime.UtcNow)
                            .ToListAsync(stoppingToken);

                        foreach (var auction in endedAuctions)
                        {
                            // Update auction status
                            auction.Status = AuctionStatus.Ended;
                            context.Auctions.Update(auction);

                            // Get the winning bid
                            var winningBid = auction.Bids.OrderByDescending(b => b.Amount).FirstOrDefault();

                            // Notify the seller
                            var sellerNotification = new Notification
                            {
                                UserId = auction.SellerId,
                                Title = "Auction Ended",
                                Message = $"Your auction '{auction.Title}' has ended. " +
                                         (winningBid != null
                                             ? $"Winning bid: ${winningBid.Amount:F2} by {winningBid.Bidder?.UserName}. Please contact the buyer to arrange shipping."
                                             : "No bids were placed."),
                                Type = NotificationType.Auction,
                                IsRead = false,
                                CreatedAt = DateTime.UtcNow,
                                RelatedEntityId = auction.Id
                            };
                            context.Notifications.Add(sellerNotification);

                            // Notify the winning bidder
                            if (winningBid != null)
                            {
                                var winnerNotification = new Notification
                                {
                                    UserId = winningBid.BidderId,
                                    Title = "You Won an Auction!",
                                    Message = $"Congratulations! You won the auction for '{auction.Title}' with a bid of ${winningBid.Amount:F2}. " +
                                             "Please contact the seller to arrange shipping.",
                                    Type = NotificationType.Auction,
                                    IsRead = false,
                                    CreatedAt = DateTime.UtcNow,
                                    RelatedEntityId = auction.Id
                                };
                                context.Notifications.Add(winnerNotification);
                            }
                        }

                        await context.SaveChangesAsync(stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while checking for ended auctions");
                }

                // Wait for 1 minute before checking again
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
} 