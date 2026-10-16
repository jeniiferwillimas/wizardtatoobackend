namespace Backend.Models;

public class Conversation
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int ArtistId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;

    public AppUser User { get; set; } = null!;
    public Artist Artist { get; set; } = null!;
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}

public class Message
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public string SenderId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? AttachmentUrl { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Conversation Conversation { get; set; } = null!;
    public AppUser Sender { get; set; } = null!;
}
