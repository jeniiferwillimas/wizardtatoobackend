namespace Backend.Models;

public class Review
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int ArtistId { get; set; }
    public int? BookingId { get; set; }
    public int Rating { get; set; } // 1-5
    public string Content { get; set; } = string.Empty;
    public string? Reply { get; set; }
    public DateTime? RepliedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public AppUser User { get; set; } = null!;
    public Artist Artist { get; set; } = null!;
    public Booking? Booking { get; set; }
}
