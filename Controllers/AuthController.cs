using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Backend.Models;
using Backend.Services;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly UserManager<AppUser> _userManager;

    public AuthController(AuthService authService, UserManager<AppUser> userManager)
    {
        _authService = authService;
        _userManager = userManager;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        var (success, error, data) = await _authService.RegisterAsync(dto);
        if (!success)
            return BadRequest(new { error });

        return Ok(data);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var (success, error, data) = await _authService.LoginAsync(dto);
        if (!success)
            return Unauthorized(new { error });

        return Ok(data);
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
    {
        var (success, error) = await _authService.ForgotPasswordAsync(dto);
        if (!success)
            return BadRequest(new { error });

        return Ok(new { message = "If an account with that email exists, a reset link has been sent." });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        var (success, error) = await _authService.ResetPasswordAsync(dto);
        if (!success)
            return BadRequest(new { error });

        return Ok(new { message = "Password has been reset successfully." });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound();

        var roles = await _userManager.GetRolesAsync(user);

        return Ok(new
        {
            userId = user.Id,
            email = user.Email,
            name = user.FullName,
            role = roles.FirstOrDefault() ?? "User",
            createdAt = user.CreatedAt
        });
    }
}
