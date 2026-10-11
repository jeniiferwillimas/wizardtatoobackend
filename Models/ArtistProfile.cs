namespace Backend.Models;

public class ArtistProfile
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string? InstagramHandle { get; set; }
    public string? Bio { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? ProfileImageUrl { get; set; }
    public bool IsStudio { get; set; }
    public int CurrentStep { get; set; }
    public string Status { get; set; } = "draft";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public AppUser User { get; set; } = null!;
    public ICollection<ArtistImage> Images { get; set; } = new List<ArtistImage>();
    public ICollection<ArtistProfileStyle> Styles { get; set; } = new List<ArtistProfileStyle>();
}
