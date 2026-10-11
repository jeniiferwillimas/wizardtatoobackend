namespace Backend.Models;

public class ArtistProfileStyle
{
    public int Id { get; set; }
    public int ArtistProfileId { get; set; }
    public string StyleName { get; set; } = string.Empty;

    public ArtistProfile ArtistProfile { get; set; } = null!;
}
