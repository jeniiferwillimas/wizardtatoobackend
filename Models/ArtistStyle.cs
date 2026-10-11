namespace Backend.Models;

public class ArtistStyle
{
    public int Id { get; set; }
    public int ArtistId { get; set; }
    public string StyleName { get; set; } = string.Empty;

    public Artist Artist { get; set; } = null!;
}
