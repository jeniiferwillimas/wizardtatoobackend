namespace Backend.Models;

public class Module
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string TextId { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Header { get; set; }
    public bool IsActive { get; set; } = true;
    public int ModuleTypeId { get; set; }
    public int? ParentId { get; set; }
    public string? UrlPath { get; set; }
    public string? Icon { get; set; }
    public int OrderNo { get; set; }

    public ModuleType ModuleType { get; set; } = null!;
    public Module? Parent { get; set; }
    public ICollection<Module> Children { get; set; } = new List<Module>();
    public ICollection<ModuleRoleAuthorization> RoleAuthorizations { get; set; } = new List<ModuleRoleAuthorization>();
}
