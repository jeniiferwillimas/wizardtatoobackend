namespace Backend.Models;

public class ArtistPortfolioImage
{
    public int Id { get; set; }
    public int ArtistId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public int? Price { get; set; }
    public int OrderNo { get; set; }

    public Artist Artist { get; set; } = null!;
}
