using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;

namespace Backend.Controllers;

[ApiController]
[Route("api/styles")]
public class TattooStyleController : ControllerBase
{
    private readonly AppDbContext _db;

    public TattooStyleController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var styles = await _db.TattooStyles
            .OrderBy(s => s.OrderNo)
            .Select(s => new
            {
                s.Name,
                s.Slug,
                s.Description,
                s.ImageUrl,
                s.ArtistCount,
                s.StudioCount,
                s.IsFeatured,
                s.OrderNo,
            })
            .ToListAsync();

        return Ok(styles);
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> GetBySlug(string slug)
    {
        var style = await _db.TattooStyles
            .Include(s => s.RelatedStyles.OrderBy(r => r.OrderNo))
            .FirstOrDefaultAsync(s => s.Slug == slug);

        if (style == null) return NotFound();

        return Ok(new
        {
            style.Name,
            style.Slug,
            style.Description,
            style.ImageUrl,
            style.ArtistCount,
            style.StudioCount,
            style.IsFeatured,
            RelatedStyles = style.RelatedStyles.Select(r => new
            {
                r.RelatedStyleName,
                r.Count,
            }),
        });
    }
}
