using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.Models;
using Backend.Services;

namespace Backend.Controllers;

[Route("api/bookings")]
[ApiController]
[Authorize]
public class BookingController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;
    private readonly EmailService _email;

    public BookingController(AppDbContext db, UserManager<AppUser> userManager, EmailService email)
    {
        _db = db;
        _userManager = userManager;
        _email = email;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyBookings()
    {
        var userId = _userManager.GetUserId(User);
        var bookings = await _db.Bookings
            .Include(b => b.Artist)
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => new
            {
                b.Id, b.Description, b.Style, b.Size, b.Placement,
                b.EstimatedPrice, b.Status, b.PreferredDate, b.Notes,
                b.CreatedAt, b.UpdatedAt,
                ArtistName = b.Artist.Name,
                ArtistSlug = b.Artist.Slug,
                ArtistCity = b.Artist.City,
            })
            .ToListAsync();
        return Ok(bookings);
    }

    [HttpGet("artist")]
    public async Task<IActionResult> GetArtistBookings()
    {
        var userId = _userManager.GetUserId(User);
        var profile = await _db.ArtistProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        if (profile == null) return Forbid();

        var bookings = await _db.Bookings
            .Include(b => b.User)
            .Where(b => b.ArtistId == profile.Id)
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => new
            {
                b.Id, b.Description, b.Style, b.Size, b.Placement,
                b.EstimatedPrice, b.Status, b.PreferredDate, b.Notes,
                b.CreatedAt, b.UpdatedAt,
                UserName = b.User.FullName,
                UserEmail = b.User.Email,
            })
            .ToListAsync();
        return Ok(bookings);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBookingDto dto)
    {
        var userId = _userManager.GetUserId(User);
        var user = await _userManager.FindByIdAsync(userId!);
        var artist = await _db.Artists.FindAsync(dto.ArtistId);
        if (artist == null) return NotFound(new { error = "Artist not found." });

        var booking = new Booking
        {
            UserId = userId!,
            ArtistId = dto.ArtistId,
            Description = dto.Description,
            Style = dto.Style,
            Size = dto.Size,
            Placement = dto.Placement,
            EstimatedPrice = dto.EstimatedPrice,
            PreferredDate = dto.PreferredDate,
            Notes = dto.Notes,
        };

        _db.Bookings.Add(booking);
        await _db.SaveChangesAsync();

        _ = Task.Run(async () =>
        {
            try
            {
                if (user?.Email != null)
                {
                    await _email.SendBookingConfirmationToClient(
                        user.Email, user.FullName ?? "there", artist.Name,
                        dto.Style, dto.Size, dto.EstimatedPrice, dto.PreferredDate);
                }

                var artistProfile = await _db.ArtistProfiles
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.Id == dto.ArtistId);
                if (artistProfile?.User?.Email != null)
                {
                    await _email.SendBookingNotificationToArtist(
                        artistProfile.User.Email, artist.Name,
                        user?.FullName ?? "A client", user?.Email ?? "",
                        dto.Description, dto.Style, dto.Size, dto.Placement,
                        dto.EstimatedPrice, dto.PreferredDate);
                }
            }
            catch { }
        });

        return Ok(new { booking.Id, message = "Booking created." });
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusDto dto)
    {
        var userId = _userManager.GetUserId(User);
        var booking = await _db.Bookings.FindAsync(id);
        if (booking == null) return NotFound();
        if (booking.UserId != userId)
        {
            var profile = await _db.ArtistProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
            if (profile == null || booking.ArtistId != profile.Id) return Forbid();
        }

        booking.Status = dto.Status;
        booking.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Status updated." });
    }
}

public class CreateBookingDto
{
    public int ArtistId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Style { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
    public string Placement { get; set; } = string.Empty;
    public decimal? EstimatedPrice { get; set; }
    public DateTime PreferredDate { get; set; }
    public string? Notes { get; set; }
}

public class UpdateStatusDto
{
    public string Status { get; set; } = string.Empty;
}
