using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;

namespace Backend.Controllers;

[ApiController]
[Route("api/artists")]
public class ArtistController : ControllerBase
{
    private readonly AppDbContext _db;

    public ArtistController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? style, [FromQuery] string? search)
    {
        var query = _db.Artists
            .Include(a => a.ArtistStyles)
            .Include(a => a.PortfolioImages.OrderBy(i => i.OrderNo))
            .OrderBy(a => a.OrderNo)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(style))
        {
            query = query.Where(a =>
                a.ArtistStyles.Any(s => s.StyleName.ToLower() == style.ToLower()));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var q = search.ToLower();
            query = query.Where(a =>
                a.Name.ToLower().Contains(q) || a.City.ToLower().Contains(q));
        }

        var artists = await query
            .Select(a => new
            {
                a.Name,
                a.Slug,
                Location = a.City + ", " + a.Country,
                a.Rating,
                a.IsStudio,
                a.Followers,
                a.PriceMin,
                a.PriceMax,
                a.ProfileImageUrl,
                Styles = a.ArtistStyles.Select(s => s.StyleName).ToList(),
                Images = a.PortfolioImages.Take(6).Select(i => new { i.ImageUrl, i.Price }).ToList(),
            })
            .ToListAsync();

        return Ok(artists);
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> GetBySlug(string slug)
    {
        var artist = await _db.Artists
            .Include(a => a.ArtistStyles)
            .Include(a => a.PortfolioImages.OrderBy(i => i.OrderNo))
            .FirstOrDefaultAsync(a => a.Slug == slug);

        if (artist == null)
            return NotFound(new { error = "Artist not found." });

        return Ok(new
        {
            artist.Id,
            artist.Name,
            artist.Slug,
            artist.City,
            artist.Country,
            Location = artist.City + ", " + artist.Country,
            artist.Rating,
            artist.IsStudio,
            artist.Bio,
            artist.InstagramHandle,
            artist.ProfileImageUrl,
            artist.Followers,
            artist.PriceMin,
            artist.PriceMax,
            artist.PieceCount,
            Styles = artist.ArtistStyles.Select(s => s.StyleName).ToList(),
            Images = artist.PortfolioImages.Select(i => new { i.Id, i.ImageUrl, i.Price, i.OrderNo }).ToList(),
        });
    }
}
