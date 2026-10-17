using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Models;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AdminOrSuperAdmin")]
public class RoleController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public RoleController(UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    [HttpGet("roles")]
    public async Task<IActionResult> GetRoles()
    {
        var roles = await _roleManager.Roles.Select(r => new { r.Id, r.Name }).ToListAsync();
        return Ok(roles);
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _userManager.Users.ToListAsync();
        var result = new List<object>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            result.Add(new
            {
                user.Id,
                user.FullName,
                user.Email,
                Role = roles.FirstOrDefault() ?? "NormalUser",
                user.CreatedAt
            });
        }

        return Ok(result);
    }

    [HttpPut("users/{userId}/role")]
    public async Task<IActionResult> UpdateUserRole(string userId, [FromBody] UpdateRoleDto dto)
    {
        var callerId = _userManager.GetUserId(User);
        if (userId == callerId)
            return BadRequest(new { error = "You cannot change your own role." });

        var caller = await _userManager.FindByIdAsync(callerId!);
        var callerRoles = caller != null ? await _userManager.GetRolesAsync(caller) : [];
        var callerIsSuperAdmin = callerRoles.Contains("SuperAdmin");

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound(new { error = "User not found." });

        var targetRoles = await _userManager.GetRolesAsync(user);
        if (targetRoles.Contains("SuperAdmin") && !callerIsSuperAdmin)
            return Forbid();

        if (dto.Role == "SuperAdmin" && !callerIsSuperAdmin)
            return BadRequest(new { error = "Only SuperAdmins can assign the SuperAdmin role." });

        var allowedRoles = new[] { "SuperAdmin", "Admin", "Artist", "NormalUser" };
        if (!allowedRoles.Contains(dto.Role))
            return BadRequest(new { error = "Invalid role. Allowed: SuperAdmin, Admin, Artist, NormalUser." });

        await _userManager.RemoveFromRolesAsync(user, targetRoles);
        await _userManager.AddToRoleAsync(user, dto.Role);

        return Ok(new { message = $"User role updated to {dto.Role}.", userId = user.Id, role = dto.Role });
    }

    [HttpDelete("users/{userId}")]
    public async Task<IActionResult> DeleteUser(string userId)
    {
        var callerId = _userManager.GetUserId(User);
        if (userId == callerId)
            return BadRequest(new { error = "You cannot delete your own account." });

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound(new { error = "User not found." });

        var caller = await _userManager.FindByIdAsync(callerId!);
        var callerRoles = caller != null ? await _userManager.GetRolesAsync(caller) : [];
        var targetRoles = await _userManager.GetRolesAsync(user);
        if (targetRoles.Contains("SuperAdmin") && !callerRoles.Contains("SuperAdmin"))
            return Forbid();

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
            return BadRequest(new { error = "Failed to delete user." });

        return Ok(new { success = true });
    }
}

public class UpdateRoleDto
{
    public string Role { get; set; } = string.Empty;
}
