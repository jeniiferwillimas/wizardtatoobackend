namespace Backend.Models;

public class SiteStat
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public int OrderNo { get; set; }
}
