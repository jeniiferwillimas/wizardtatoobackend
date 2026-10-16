using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.Models;

namespace Backend.Controllers;

[Route("api/saved")]
[ApiController]
[Authorize]
public class SavedController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;

    public SavedController(AppDbContext db, UserManager<AppUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> GetSavedItems()
    {
        var userId = _userManager.GetUserId(User);
        var items = await _db.SavedItems
            .Include(s => s.Artist)
            .Include(s => s.PortfolioImage)
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new
            {
                s.Id, s.BoardName, s.Note, s.CreatedAt,
                ArtistId = s.ArtistId,
                ArtistName = s.Artist != null ? s.Artist.Name : null,
                ArtistSlug = s.Artist != null ? s.Artist.Slug : null,
                ImageUrl = s.PortfolioImage != null ? s.PortfolioImage.ImageUrl : null,
            })
            .ToListAsync();
        return Ok(items);
    }

    [HttpGet("boards")]
    public async Task<IActionResult> GetBoards()
    {
        var userId = _userManager.GetUserId(User);
        var boards = await _db.SavedItems
            .Where(s => s.UserId == userId)
            .GroupBy(s => s.BoardName)
            .Select(g => new
            {
                Name = g.Key,
                Count = g.Count(),
                LastAdded = g.Max(s => s.CreatedAt),
            })
            .ToListAsync();
        return Ok(boards);
    }

    [HttpPost]
    public async Task<IActionResult> Save([FromBody] SaveItemDto dto)
    {
        var userId = _userManager.GetUserId(User);
        var item = new SavedItem
        {
            UserId = userId!,
            ArtistId = dto.ArtistId,
            PortfolioImageId = dto.PortfolioImageId,
            BoardName = dto.BoardName ?? "Default",
            Note = dto.Note,
        };
        _db.SavedItems.Add(item);
        await _db.SaveChangesAsync();
        return Ok(new { item.Id, message = "Saved." });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Remove(int id)
    {
        var userId = _userManager.GetUserId(User);
        var item = await _db.SavedItems.FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);
        if (item == null) return NotFound();

        _db.SavedItems.Remove(item);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Removed." });
    }
}

public class SaveItemDto
{
    public int? ArtistId { get; set; }
    public int? PortfolioImageId { get; set; }
    public string? BoardName { get; set; }
    public string? Note { get; set; }
}
