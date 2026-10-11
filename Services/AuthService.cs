using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Backend.Models;

namespace Backend.Services;

public class AuthService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IConfiguration _config;

    public AuthService(UserManager<AppUser> userManager, IConfiguration config)
    {
        _userManager = userManager;
        _config = config;
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

    public async Task<(bool Success, string? Error, AuthResponseDto? Data)> GoogleLoginAsync(GoogleAuthDto dto)
    {
        GoogleJsonWebSignature.Payload payload;
        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { _config["Google:ClientId"]! }
            };
            payload = await GoogleJsonWebSignature.ValidateAsync(dto.Credential, settings);
        }
        catch
        {
            return (false, "Invalid Google token.", null);
        }

        var user = await _userManager.FindByEmailAsync(payload.Email);
        if (user == null)
        {
            user = new AppUser
            {
                UserName = payload.Email,
                Email = payload.Email,
                FullName = payload.Name ?? payload.Email,
                EmailConfirmed = true
            };
            var result = await _userManager.CreateAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join(" ", result.Errors.Select(e => e.Description));
                return (false, errors, null);
            }
            await _userManager.AddToRoleAsync(user, "NormalUser");
        }

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

    public async Task<(bool Success, string? Error, string? ResetToken)> ForgotPasswordAsync(ForgotPasswordDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null)
            return (true, null, null);

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        // In production, send this token via email. For dev, return it in the response.
        return (true, null, token);
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
