namespace Backend.Models;

public class Artist
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public double Rating { get; set; }
    public bool IsStudio { get; set; }
    public int OrderNo { get; set; }
    public string? Bio { get; set; }
    public string? InstagramHandle { get; set; }
    public string? ProfileImageUrl { get; set; }
    public string? Followers { get; set; }
    public int? PriceMin { get; set; }
    public int? PriceMax { get; set; }
    public int PieceCount { get; set; }

    public ICollection<ArtistStyle> ArtistStyles { get; set; } = new List<ArtistStyle>();
    public ICollection<ArtistPortfolioImage> PortfolioImages { get; set; } = new List<ArtistPortfolioImage>();
}
