using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;

namespace Backend.Controllers;

[ApiController]
[Route("api/landing")]
public class LandingController : ControllerBase
{
    private readonly AppDbContext _db;

    public LandingController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetLandingData()
    {
        var stats = await _db.SiteStats
            .OrderBy(s => s.OrderNo)
            .Select(s => new { s.Value, s.Label })
            .ToListAsync();

        var testimonials = await _db.Testimonials
            .OrderBy(t => t.OrderNo)
            .Select(t => new
            {
                t.Quote,
                Name = t.AuthorName,
                Initials = t.AuthorInitials,
                t.City,
                Verified = t.IsVerified,
            })
            .ToListAsync();

        var popularCities = await _db.PopularCities
            .OrderBy(c => c.OrderNo)
            .Select(c => c.Name)
            .ToListAsync();

        var styles = await _db.TattooStyles
            .OrderBy(s => s.OrderNo)
            .Select(s => new
            {
                s.Name,
                s.Slug,
                s.Description,
                s.ImageUrl,
                Artists = s.ArtistCount.ToString("N0"),
                s.IsFeatured,
            })
            .ToListAsync();

        return Ok(new { stats, testimonials, popularCities, styles });
    }
}
