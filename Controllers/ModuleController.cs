using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.Models;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ModuleController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;

    public ModuleController(AppDbContext db, UserManager<AppUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [Authorize]
    [HttpGet("authorized")]
    public async Task<IActionResult> GetAuthorizedModules()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return Unauthorized();

        var roles = await _userManager.GetRolesAsync(user);

        var authorizedSubjects = await _db.ModuleRoleAuthorizations
            .Where(a => roles.Contains(a.RoleName) && a.IsGranted)
            .Select(a => a.Subject)
            .Distinct()
            .ToListAsync();

        var allModules = await _db.Modules
            .Where(m => m.IsActive && authorizedSubjects.Contains(m.TextId))
            .OrderBy(m => m.OrderNo)
            .ToListAsync();

        var topLevel = allModules.Where(m => m.ParentId == null).ToList();

        var result = topLevel.Select(m => MapModule(m, allModules)).ToList();

        return Ok(new { success = true, data = result });
    }

    [Authorize(Policy = "SuperAdminOnly")]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var modules = await _db.Modules
            .Include(m => m.ModuleType)
            .OrderBy(m => m.OrderNo)
            .ToListAsync();

        return Ok(new { success = true, data = modules });
    }

    private static object MapModule(Module module, List<Module> all)
    {
        var children = all
            .Where(m => m.ParentId == module.Id)
            .OrderBy(m => m.OrderNo)
            .Select(c => MapModule(c, all))
            .ToList();

        return new
        {
            module.Id,
            module.Name,
            module.TextId,
            module.Description,
            module.Header,
            module.UrlPath,
            module.Icon,
            module.OrderNo,
            module.ParentId,
            Children = children
        };
    }
}
