namespace Backend.Models;

public class ArtistSubscription
{
    public int Id { get; set; }
    public int ArtistProfileId { get; set; }
    public string Plan { get; set; } = "Basic"; // Basic, Pro, Premium
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string Status { get; set; } = "Inactive"; // Active, Inactive, Cancelled, PastDue
    public string? PaymentRef { get; set; }
    public DateTime? CurrentPeriodStart { get; set; }
    public DateTime? CurrentPeriodEnd { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ArtistProfile ArtistProfile { get; set; } = null!;
}
