using OnlineAuction.Models;

namespace OnlineAuction.ViewModels
{
    public class ConversationViewModel
    {
        public int Id { get; set; }
        public ApplicationUser? OtherUser { get; set; }
        public string LastMessage { get; set; } = string.Empty;
        public DateTime LastMessageTime { get; set; }
        public int UnreadCount { get; set; }
    }

    public class ConversationDetailViewModel
    {
        public int ConversationId { get; set; }
        public ApplicationUser? OtherUser { get; set; }
        public List<Message> Messages { get; set; } = [];
    }

    public class SendMessageViewModel
    {
        public int ConversationId { get; set; }
        public string Content { get; set; } = string.Empty;
        public int? AuctionId { get; set; }
    }
} 