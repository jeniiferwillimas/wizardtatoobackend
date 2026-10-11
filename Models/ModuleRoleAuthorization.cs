namespace Backend.Models;

public class ModuleRoleAuthorization
{
    public int Id { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public bool IsGranted { get; set; } = true;
}
