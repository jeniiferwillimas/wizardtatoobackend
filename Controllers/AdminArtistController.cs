using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.Models;
using Backend.Services;

namespace Backend.Controllers;

[ApiController]
[Route("api/admin/artists")]
[Authorize(Policy = "AdminOrSuperAdmin")]
public class AdminArtistController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly FileUploadService _upload;

    public AdminArtistController(AppDbContext db, FileUploadService upload)
    {
        _db = db;
        _upload = upload;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var artists = await _db.Artists
            .Include(a => a.ArtistStyles)
            .Include(a => a.PortfolioImages.OrderBy(i => i.OrderNo))
            .OrderBy(a => a.OrderNo)
            .Select(a => new
            {
                a.Id, a.Name, a.Slug, a.City, a.Country,
                Location = a.City + ", " + a.Country,
                a.Rating, a.IsStudio, a.Bio, a.InstagramHandle,
                a.ProfileImageUrl, a.Followers, a.PriceMin, a.PriceMax, a.PieceCount,
                Styles = a.ArtistStyles.Select(s => s.StyleName).ToList(),
                Images = a.PortfolioImages.Select(i => new { i.Id, i.ImageUrl, i.Price, i.OrderNo }).ToList(),
            })
            .ToListAsync();

        return Ok(artists);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var artist = await _db.Artists
            .Include(a => a.ArtistStyles)
            .Include(a => a.PortfolioImages.OrderBy(i => i.OrderNo))
            .FirstOrDefaultAsync(a => a.Id == id);

        if (artist == null)
            return NotFound(new { error = "Artist not found" });

        return Ok(new
        {
            artist.Id, artist.Name, artist.Slug, artist.City, artist.Country,
            artist.Rating, artist.IsStudio, artist.Bio, artist.InstagramHandle,
            artist.ProfileImageUrl, artist.Followers, artist.PriceMin, artist.PriceMax, artist.PieceCount,
            Styles = artist.ArtistStyles.Select(s => s.StyleName).ToList(),
            Images = artist.PortfolioImages.Select(i => new { i.Id, i.ImageUrl, i.Price, i.OrderNo }).ToList(),
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AdminArtistDto dto)
    {
        var slug = dto.Name.ToLower().Replace(" ", "-").Replace(".", "");
        var existing = await _db.Artists.AnyAsync(a => a.Slug == slug);
        if (existing)
            slug += "-" + Guid.NewGuid().ToString()[..4];

        var maxOrder = await _db.Artists.AnyAsync() ? await _db.Artists.MaxAsync(a => a.OrderNo) : 0;

        var artist = new Artist
        {
            Name = dto.Name,
            Slug = slug,
            City = dto.City ?? "",
            Country = dto.Country ?? "",
            Rating = dto.Rating ?? 0,
            IsStudio = dto.IsStudio,
            Bio = dto.Bio,
            InstagramHandle = dto.InstagramHandle,
            Followers = dto.Followers,
            PriceMin = dto.PriceMin,
            PriceMax = dto.PriceMax,
            PieceCount = dto.PieceCount ?? 0,
            OrderNo = maxOrder + 1,
        };

        _db.Artists.Add(artist);
        await _db.SaveChangesAsync();

        if (dto.Styles != null)
        {
            foreach (var style in dto.Styles)
                _db.Set<ArtistStyle>().Add(new ArtistStyle { ArtistId = artist.Id, StyleName = style });
            await _db.SaveChangesAsync();
        }

        return Ok(new { artist.Id, artist.Slug });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] AdminArtistDto dto)
    {
        var artist = await _db.Artists
            .Include(a => a.ArtistStyles)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (artist == null)
            return NotFound(new { error = "Artist not found" });

        artist.Name = dto.Name;
        artist.City = dto.City ?? "";
        artist.Country = dto.Country ?? "";
        artist.Rating = dto.Rating ?? artist.Rating;
        artist.IsStudio = dto.IsStudio;
        artist.Bio = dto.Bio;
        artist.InstagramHandle = dto.InstagramHandle;
        artist.Followers = dto.Followers;
        artist.PriceMin = dto.PriceMin;
        artist.PriceMax = dto.PriceMax;
        artist.PieceCount = dto.PieceCount ?? artist.PieceCount;

        if (dto.Styles != null)
        {
            _db.Set<ArtistStyle>().RemoveRange(artist.ArtistStyles);
            foreach (var style in dto.Styles)
                _db.Set<ArtistStyle>().Add(new ArtistStyle { ArtistId = artist.Id, StyleName = style });
        }

        await _db.SaveChangesAsync();
        return Ok(new { success = true });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var artist = await _db.Artists
            .Include(a => a.PortfolioImages)
            .Include(a => a.ArtistStyles)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (artist == null)
            return NotFound(new { error = "Artist not found" });

        foreach (var img in artist.PortfolioImages)
            _upload.Delete(img.ImageUrl);

        _db.Artists.Remove(artist);
        await _db.SaveChangesAsync();

        return Ok(new { success = true });
    }

    [HttpPost("{id}/images")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> UploadImage(int id, IFormFile file, [FromForm] int? price)
    {
        var artist = await _db.Artists
            .Include(a => a.PortfolioImages)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (artist == null)
            return NotFound(new { error = "Artist not found" });

        var (success, url, error) = await _upload.UploadAsync(file, $"artists/{artist.Id}");
        if (!success)
            return BadRequest(new { error });

        var nextOrder = artist.PortfolioImages.Any() ? artist.PortfolioImages.Max(i => i.OrderNo) + 1 : 0;

        var image = new ArtistPortfolioImage
        {
            ArtistId = artist.Id,
            ImageUrl = url!,
            OrderNo = nextOrder,
            Price = price,
        };

        _db.Set<ArtistPortfolioImage>().Add(image);
        await _db.SaveChangesAsync();

        return Ok(new { image.Id, image.ImageUrl, image.OrderNo, image.Price });
    }

    [HttpPost("{id}/profile-image")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> UploadProfileImage(int id, IFormFile file)
    {
        var artist = await _db.Artists.FindAsync(id);
        if (artist == null)
            return NotFound(new { error = "Artist not found" });

        if (!string.IsNullOrEmpty(artist.ProfileImageUrl))
            _upload.Delete(artist.ProfileImageUrl);

        var (success, url, error) = await _upload.UploadAsync(file, $"artists/{artist.Id}/profile");
        if (!success)
            return BadRequest(new { error });

        artist.ProfileImageUrl = url;
        await _db.SaveChangesAsync();

        return Ok(new { url });
    }

    [HttpDelete("{id}/images/{imageId}")]
    public async Task<IActionResult> DeleteImage(int id, int imageId)
    {
        var image = await _db.Set<ArtistPortfolioImage>()
            .FirstOrDefaultAsync(i => i.Id == imageId && i.ArtistId == id);

        if (image == null)
            return NotFound(new { error = "Image not found" });

        _upload.Delete(image.ImageUrl);
        _db.Set<ArtistPortfolioImage>().Remove(image);
        await _db.SaveChangesAsync();

        return Ok(new { success = true });
    }
}

public record AdminArtistDto(
    string Name,
    string? City,
    string? Country,
    double? Rating,
    bool IsStudio,
    string? Bio,
    string? InstagramHandle,
    string? Followers,
    int? PriceMin,
    int? PriceMax,
    int? PieceCount,
    string[]? Styles
);
