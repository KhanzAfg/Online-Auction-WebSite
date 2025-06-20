using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OnlineAuction.Models;

namespace OnlineAuction.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Auction> Auctions { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Bid> Bids { get; set; }
        public DbSet<AuctionImage> AuctionImages { get; set; }
        public DbSet<AuctionReport> AuctionReports { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Rating> Ratings { get; set; }
        public DbSet<Watchlist> Watchlist { get; set; }
        
        // New DbSets
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<Shipping> Shippings { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure Auction
            builder.Entity<Auction>()
                .HasOne(a => a.Seller)
                .WithMany(u => u.Auctions)
                .HasForeignKey(a => a.SellerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Auction>()
                .HasOne(a => a.Category)
                .WithMany(c => c.Auctions)
                .HasForeignKey(a => a.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Bid
            builder.Entity<Bid>()
                .HasOne(b => b.Auction)
                .WithMany(a => a.Bids)
                .HasForeignKey(b => b.AuctionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Bid>()
                .HasOne(b => b.Bidder)
                .WithMany(u => u.Bids)
                .HasForeignKey(b => b.BidderId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure AuctionImage
            builder.Entity<AuctionImage>()
                .HasOne(i => i.Auction)
                .WithMany(a => a.Images)
                .HasForeignKey(i => i.AuctionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure AuctionReport
            builder.Entity<AuctionReport>()
                .HasOne(r => r.Auction)
                .WithMany(a => a.Reports)
                .HasForeignKey(r => r.AuctionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<AuctionReport>()
                .HasOne(r => r.Reporter)
                .WithMany(u => u.Reports)
                .HasForeignKey(r => r.ReporterId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Notification
            builder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Rating
            builder.Entity<Rating>()
                .HasOne(r => r.RatedUser)
                .WithMany(u => u.RatingsReceived)
                .HasForeignKey(r => r.RatedUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Rating>()
                .HasOne(r => r.RaterUser)
                .WithMany(u => u.RatingsGiven)
                .HasForeignKey(r => r.RaterUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Watchlist>()
                .HasOne(w => w.User)
                .WithMany(u => u.Watchlist)
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Watchlist>()
                .HasOne(w => w.Auction)
                .WithMany(a => a.Watchlist)
                .HasForeignKey(w => w.AuctionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Payment
            builder.Entity<Payment>()
                .HasOne(p => p.Auction)
                .WithMany()
                .HasForeignKey(p => p.AuctionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Payment>()
                .HasOne(p => p.Buyer)
                .WithMany(u => u.PaymentsAsBuyer)
                .HasForeignKey(p => p.BuyerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Payment>()
                .HasOne(p => p.Seller)
                .WithMany(u => u.PaymentsAsSeller)
                .HasForeignKey(p => p.SellerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Message
            builder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany(u => u.MessagesSent)
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Message>()
                .HasOne(m => m.Receiver)
                .WithMany(u => u.MessagesReceived)
                .HasForeignKey(m => m.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Message>()
                .HasOne(m => m.Auction)
                .WithMany()
                .HasForeignKey(m => m.AuctionId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure Conversation
            builder.Entity<Conversation>()
                .HasOne(c => c.User1)
                .WithMany(u => u.ConversationsAsUser1)
                .HasForeignKey(c => c.User1Id)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Conversation>()
                .HasOne(c => c.User2)
                .WithMany(u => u.ConversationsAsUser2)
                .HasForeignKey(c => c.User2Id)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Shipping
            builder.Entity<Shipping>()
                .HasOne(s => s.Auction)
                .WithMany()
                .HasForeignKey(s => s.AuctionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Shipping>()
                .HasOne(s => s.Buyer)
                .WithMany(u => u.ShippingsAsBuyer)
                .HasForeignKey(s => s.BuyerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Shipping>()
                .HasOne(s => s.Seller)
                .WithMany(u => u.ShippingsAsSeller)
                .HasForeignKey(s => s.SellerId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
} 