namespace Backend.Models;

public class ArtistImage
{
    public int Id { get; set; }
    public int ArtistProfileId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public int OrderNo { get; set; }
    public int? Price { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public ArtistProfile ArtistProfile { get; set; } = null!;
}
