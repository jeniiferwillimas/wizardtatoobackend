using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.Models;

namespace Backend.Controllers;

[Route("api/reviews")]
[ApiController]
public class ReviewController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;

    public ReviewController(AppDbContext db, UserManager<AppUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [HttpGet("artist/{artistId}")]
    public async Task<IActionResult> GetArtistReviews(int artistId)
    {
        var reviews = await _db.Reviews
            .Include(r => r.User)
            .Where(r => r.ArtistId == artistId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new
            {
                r.Id, r.Rating, r.Content, r.Reply, r.RepliedAt, r.CreatedAt,
                UserName = r.User.FullName,
            })
            .ToListAsync();

        var avg = reviews.Count > 0 ? reviews.Average(r => r.Rating) : 0;
        return Ok(new { reviews, averageRating = Math.Round(avg, 1), totalReviews = reviews.Count });
    }

    [HttpGet("my")]
    [Authorize]
    public async Task<IActionResult> GetMyReviews()
    {
        var userId = _userManager.GetUserId(User);
        var reviews = await _db.Reviews
            .Include(r => r.Artist)
            .Where(r => r.ArtistId != 0)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new
            {
                r.Id, r.Rating, r.Content, r.Reply, r.RepliedAt, r.CreatedAt,
                ArtistName = r.Artist.Name,
                ArtistSlug = r.Artist.Slug,
            })
            .ToListAsync();
        return Ok(reviews);
    }

    [HttpGet("received")]
    [Authorize]
    public async Task<IActionResult> GetReceivedReviews()
    {
        var userId = _userManager.GetUserId(User);
        var profile = await _db.ArtistProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        if (profile == null) return Forbid();

        var reviews = await _db.Reviews
            .Include(r => r.User)
            .Where(r => r.ArtistId == profile.Id)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new
            {
                r.Id, r.Rating, r.Content, r.Reply, r.RepliedAt, r.CreatedAt,
                UserName = r.User.FullName,
            })
            .ToListAsync();

        var avg = reviews.Count > 0 ? reviews.Average(r => r.Rating) : 0;
        return Ok(new { reviews, averageRating = Math.Round(avg, 1), totalReviews = reviews.Count });
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateReviewDto dto)
    {
        var userId = _userManager.GetUserId(User);
        var existing = await _db.Reviews.AnyAsync(r => r.UserId == userId && r.ArtistId == dto.ArtistId);
        if (existing) return BadRequest(new { error = "You have already reviewed this artist." });

        var review = new Review
        {
            UserId = userId!,
            ArtistId = dto.ArtistId,
            BookingId = dto.BookingId,
            Rating = Math.Clamp(dto.Rating, 1, 5),
            Content = dto.Content,
        };
        _db.Reviews.Add(review);
        await _db.SaveChangesAsync();
        return Ok(new { review.Id, message = "Review submitted." });
    }

    [HttpPut("{id}/reply")]
    [Authorize]
    public async Task<IActionResult> Reply(int id, [FromBody] ReplyDto dto)
    {
        var userId = _userManager.GetUserId(User);
        var review = await _db.Reviews.FindAsync(id);
        if (review == null) return NotFound();

        var profile = await _db.ArtistProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        if (profile == null || review.ArtistId != profile.Id) return Forbid();

        review.Reply = dto.Reply;
        review.RepliedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Reply saved." });
    }
}

public class CreateReviewDto
{
    public int ArtistId { get; set; }
    public int? BookingId { get; set; }
    public int Rating { get; set; }
    public string Content { get; set; } = string.Empty;
}

public class ReplyDto
{
    public string Reply { get; set; } = string.Empty;
}
