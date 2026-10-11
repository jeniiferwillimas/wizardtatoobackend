namespace Backend.Models;

public class ModuleType
{
    public int Id { get; set; }
    public string TextId { get; set; } = string.Empty;
    public ICollection<Module> Modules { get; set; } = new List<Module>();
}
