namespace Backend.Models;

public class StyleRelation
{
    public int Id { get; set; }
    public int TattooStyleId { get; set; }
    public string RelatedStyleName { get; set; } = string.Empty;
    public int Count { get; set; }
    public int OrderNo { get; set; }

    public TattooStyle TattooStyle { get; set; } = null!;
}
