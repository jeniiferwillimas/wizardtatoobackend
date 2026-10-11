namespace Backend.Models;

public class TattooStyle
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public int ArtistCount { get; set; }
    public int StudioCount { get; set; }
    public bool IsFeatured { get; set; }
    public int OrderNo { get; set; }

    public ICollection<StyleRelation> RelatedStyles { get; set; } = new List<StyleRelation>();
}
