using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.Models;
using Backend.Services;

namespace Backend.Controllers;

[ApiController]
[Route("api/artist-profile")]
[Authorize]
public class ArtistProfileController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly FileUploadService _upload;

    public ArtistProfileController(AppDbContext db, FileUploadService upload)
    {
        _db = db;
        _upload = upload;
    }

    private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet]
    public async Task<IActionResult> GetProfile()
    {
        var profile = await _db.ArtistProfiles
            .Include(p => p.Images.OrderBy(i => i.OrderNo))
            .Include(p => p.Styles)
            .FirstOrDefaultAsync(p => p.UserId == GetUserId());

        if (profile == null)
            return Ok(new { exists = false });

        return Ok(new
        {
            exists = true,
            profile = new
            {
                profile.Id,
                profile.InstagramHandle,
                profile.Bio,
                profile.City,
                profile.Country,
                profile.ProfileImageUrl,
                profile.IsStudio,
                profile.CurrentStep,
                profile.Status,
                Images = profile.Images.Select(i => new { i.Id, i.ImageUrl, i.FileName, i.OrderNo, i.Price }),
                Styles = profile.Styles.Select(s => s.StyleName),
            }
        });
    }

    [HttpPost]
    public async Task<IActionResult> CreateProfile([FromBody] CreateProfileDto dto)
    {
        var userId = GetUserId();
        var existing = await _db.ArtistProfiles.AnyAsync(p => p.UserId == userId);
        if (existing)
            return BadRequest(new { error = "Profile already exists" });

        var profile = new ArtistProfile
        {
            UserId = userId,
            InstagramHandle = dto.InstagramHandle,
            IsStudio = dto.IsStudio,
            CurrentStep = 1,
            Status = "draft",
        };

        _db.ArtistProfiles.Add(profile);
        await _db.SaveChangesAsync();

        return Ok(new { profile.Id, profile.CurrentStep, profile.Status });
    }

    [HttpPut("details")]
    public async Task<IActionResult> UpdateDetails([FromBody] UpdateDetailsDto dto)
    {
        var profile = await _db.ArtistProfiles
            .Include(p => p.Styles)
            .FirstOrDefaultAsync(p => p.UserId == GetUserId());

        if (profile == null)
            return NotFound(new { error = "Profile not found" });

        profile.Bio = dto.Bio;
        profile.City = dto.City;
        profile.Country = dto.Country;
        profile.IsStudio = dto.IsStudio;
        if (profile.CurrentStep < 4) profile.CurrentStep = 4;
        profile.UpdatedAt = DateTime.UtcNow;

        _db.ArtistProfileStyles.RemoveRange(profile.Styles);
        foreach (var style in dto.Styles ?? [])
            profile.Styles.Add(new ArtistProfileStyle { StyleName = style });

        await _db.SaveChangesAsync();

        return Ok(new { success = true });
    }

    [HttpPut("step")]
    public async Task<IActionResult> UpdateStep([FromBody] UpdateStepDto dto)
    {
        var profile = await _db.ArtistProfiles
            .FirstOrDefaultAsync(p => p.UserId == GetUserId());

        if (profile == null)
            return NotFound(new { error = "Profile not found" });

        if (dto.Step > profile.CurrentStep)
            profile.CurrentStep = dto.Step;

        profile.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new { profile.CurrentStep });
    }

    [HttpPost("images")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> UploadImage(IFormFile file, [FromForm] int? price)
    {
        var profile = await _db.ArtistProfiles
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.UserId == GetUserId());

        if (profile == null)
            return NotFound(new { error = "Profile not found" });

        var (success, url, error) = await _upload.UploadAsync(file, $"artists/{profile.Id}");
        if (!success)
            return BadRequest(new { error });

        var nextOrder = profile.Images.Any() ? profile.Images.Max(i => i.OrderNo) + 1 : 0;

        var image = new ArtistImage
        {
            ArtistProfileId = profile.Id,
            ImageUrl = url!,
            FileName = file.FileName,
            OrderNo = nextOrder,
            Price = price,
        };

        _db.ArtistImages.Add(image);

        if (profile.CurrentStep < 3) profile.CurrentStep = 3;
        profile.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new { image.Id, image.ImageUrl, image.FileName, image.OrderNo, image.Price });
    }

    [HttpPut("images/{imageId}/price")]
    public async Task<IActionResult> UpdateImagePrice(int imageId, [FromBody] UpdatePriceDto dto)
    {
        var profile = await _db.ArtistProfiles
            .FirstOrDefaultAsync(p => p.UserId == GetUserId());

        if (profile == null)
            return NotFound(new { error = "Profile not found" });

        var image = await _db.ArtistImages
            .FirstOrDefaultAsync(i => i.Id == imageId && i.ArtistProfileId == profile.Id);

        if (image == null)
            return NotFound(new { error = "Image not found" });

        image.Price = dto.Price;
        await _db.SaveChangesAsync();

        return Ok(new { image.Id, image.Price });
    }

    [HttpDelete("images/{imageId}")]
    public async Task<IActionResult> DeleteImage(int imageId)
    {
        var profile = await _db.ArtistProfiles
            .FirstOrDefaultAsync(p => p.UserId == GetUserId());

        if (profile == null)
            return NotFound(new { error = "Profile not found" });

        var image = await _db.ArtistImages
            .FirstOrDefaultAsync(i => i.Id == imageId && i.ArtistProfileId == profile.Id);

        if (image == null)
            return NotFound(new { error = "Image not found" });

        _upload.Delete(image.ImageUrl);
        _db.ArtistImages.Remove(image);
        await _db.SaveChangesAsync();

        return Ok(new { success = true });
    }

    [HttpPost("submit")]
    public async Task<IActionResult> SubmitForReview()
    {
        var profile = await _db.ArtistProfiles
            .Include(p => p.Images)
            .Include(p => p.Styles)
            .FirstOrDefaultAsync(p => p.UserId == GetUserId());

        if (profile == null)
            return NotFound(new { error = "Profile not found" });

        if (profile.Images.Count < 6)
            return BadRequest(new { error = "At least 6 photos required" });

        if (string.IsNullOrEmpty(profile.Bio))
            return BadRequest(new { error = "Bio is required" });

        if (string.IsNullOrEmpty(profile.City))
            return BadRequest(new { error = "City is required" });

        if (!profile.Styles.Any())
            return BadRequest(new { error = "At least one style is required" });

        profile.Status = "pending_review";
        profile.CurrentStep = 5;
        profile.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new { profile.Status, profile.CurrentStep });
    }
}

public record CreateProfileDto(string? InstagramHandle, bool IsStudio);
public record UpdateDetailsDto(string? Bio, string? City, string? Country, bool IsStudio, string[]? Styles);
public record UpdateStepDto(int Step);
public record UpdatePriceDto(int? Price);
