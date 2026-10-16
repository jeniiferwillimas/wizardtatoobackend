namespace Backend.Models;

public class Booking
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int ArtistId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? ReferenceImageUrl { get; set; }
    public string Style { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
    public string Placement { get; set; } = string.Empty;
    public decimal? EstimatedPrice { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Confirmed, Completed, Cancelled, Declined
    public string PaymentStatus { get; set; } = "Unpaid"; // Unpaid, Pending, Paid, Refunded, Failed
    public string? PaymentRef { get; set; }
    public decimal? AmountPaid { get; set; }
    public string? Currency { get; set; } = "USD";
    public DateTime? PaidAt { get; set; }
    public DateTime PreferredDate { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public AppUser User { get; set; } = null!;
    public Artist Artist { get; set; } = null!;
}
