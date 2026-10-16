namespace Backend.Models;

public class SavedItem
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int? ArtistId { get; set; }
    public int? PortfolioImageId { get; set; }
    public string? BoardName { get; set; } = "Default";
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public AppUser User { get; set; } = null!;
    public Artist? Artist { get; set; }
    public ArtistPortfolioImage? PortfolioImage { get; set; }
}
