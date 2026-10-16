using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Backend.Models;

namespace Backend.Services;

public class AuthService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IConfiguration _config;
    private readonly EmailService _email;

    public AuthService(UserManager<AppUser> userManager, IConfiguration config, EmailService email)
    {
        _userManager = userManager;
        _config = config;
        _email = email;
    }

    public async Task<(bool Success, string? Error, AuthResponseDto? Data)> RegisterAsync(RegisterDto dto)
    {
        var existingUser = await _userManager.FindByEmailAsync(dto.Email);
        if (existingUser != null)
            return (false, "An account with this email already exists.", null);

        var allowedRoles = new[] { "NormalUser", "Artist" };
        var role = allowedRoles.Contains(dto.Role) ? dto.Role : "NormalUser";

        var user = new AppUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            FullName = dto.Name
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(" ", result.Errors.Select(e => e.Description));
            return (false, errors, null);
        }

        await _userManager.AddToRoleAsync(user, role);

        var token = await GenerateJwtToken(user);
        return (true, null, new AuthResponseDto
        {
            Token = token,
            Email = user.Email!,
            Name = user.FullName,
            UserId = user.Id,
            Role = role
        });
    }

    public async Task<(bool Success, string? Error, AuthResponseDto? Data)> LoginAsync(LoginDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null)
            return (false, "Invalid email or password.", null);

        var validPassword = await _userManager.CheckPasswordAsync(user, dto.Password);
        if (!validPassword)
            return (false, "Invalid email or password.", null);

        var roles = await _userManager.GetRolesAsync(user);
        var token = await GenerateJwtToken(user);
        return (true, null, new AuthResponseDto
        {
            Token = token,
            Email = user.Email!,
            Name = user.FullName,
            UserId = user.Id,
            Role = roles.FirstOrDefault() ?? "NormalUser"
        });
    }

    public async Task<(bool Success, string? Error)> ForgotPasswordAsync(ForgotPasswordDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null)
            return (true, null);

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var frontendUrl = _config["FrontendUrl"] ?? "http://localhost:3000";
        var resetLink = $"{frontendUrl}/reset-password?email={Uri.EscapeDataString(dto.Email)}&token={Uri.EscapeDataString(token)}";

        var email = dto.Email;
        _ = Task.Run(async () =>
        {
            try
            {
                await _email.SendAsync(email, "Reset Your Password", $"""
                <div style="font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif; max-width: 560px; margin: 0 auto; padding: 32px 24px; color: #1a1a1a;">
                  <h1 style="font-size: 24px; margin: 0 0 16px;">Reset Your Password</h1>
                  <p style="color: #666; margin: 0 0 24px;">Click the button below to reset your password. This link expires in 24 hours.</p>
                  <a href="{resetLink}" style="display: inline-block; background: #16a34a; color: white; padding: 14px 28px; border-radius: 8px; text-decoration: none; font-weight: 600;">Reset Password</a>
                  <p style="color: #999; font-size: 12px; margin-top: 32px;">If you didn't request this, you can safely ignore this email.</p>
                  <p style="color: #999; font-size: 12px;">— TattooNest</p>
                </div>
                """);
            }
            catch { }
        });

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> ResetPasswordAsync(ResetPasswordDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null)
            return (false, "Invalid email or token.");

        var result = await _userManager.ResetPasswordAsync(user, dto.Token, dto.NewPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join(" ", result.Errors.Select(e => e.Description));
            return (false, errors);
        }

        return (true, null);
    }

    private async Task<string> GenerateJwtToken(AppUser user)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var roles = await _userManager.GetRolesAsync(user);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email!),
            new(ClaimTypes.Name, user.FullName)
        };

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
