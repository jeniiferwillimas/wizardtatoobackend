using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;

namespace Backend.Controllers;

[ApiController]
[Route("api/admin/messages")]
[Authorize(Policy = "AdminOrSuperAdmin")]
public class AdminMessageController : ControllerBase
{
    private readonly AppDbContext _db;

    public AdminMessageController(AppDbContext db) => _db = db;

    [HttpGet("conversations")]
    public async Task<IActionResult> GetAll([FromQuery] int? artistId)
    {
        var query = _db.Conversations
            .Include(c => c.Artist)
            .Include(c => c.User)
            .Include(c => c.Messages.OrderByDescending(m => m.CreatedAt).Take(1))
            .AsQueryable();

        if (artistId.HasValue)
            query = query.Where(c => c.ArtistId == artistId.Value);

        var conversations = await query
            .OrderByDescending(c => c.LastMessageAt)
            .Select(c => new
            {
                c.Id,
                c.ArtistId,
                ArtistName = c.Artist.Name,
                ArtistSlug = c.Artist.Slug,
                UserName = c.User.FullName,
                UserEmail = c.User.Email,
                c.LastMessageAt,
                LastMessage = c.Messages.OrderByDescending(m => m.CreatedAt).Select(m => m.Content).FirstOrDefault(),
                MessageCount = c.Messages.Count(),
                UnreadCount = c.Messages.Count(m => !m.IsRead),
            })
            .ToListAsync();

        return Ok(conversations);
    }

    [HttpGet("conversations/{id}")]
    public async Task<IActionResult> GetMessages(int id)
    {
        var conversation = await _db.Conversations
            .Include(c => c.Artist)
            .Include(c => c.User)
            .Include(c => c.Messages.OrderBy(m => m.CreatedAt))
                .ThenInclude(m => m.Sender)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (conversation == null) return NotFound();

        return Ok(new
        {
            conversation.Id,
            ArtistName = conversation.Artist.Name,
            UserName = conversation.User.FullName,
            UserEmail = conversation.User.Email,
            Messages = conversation.Messages.Select(m => new
            {
                m.Id,
                m.SenderId,
                SenderName = m.Sender.FullName,
                m.Content,
                m.AttachmentUrl,
                m.IsRead,
                m.CreatedAt,
            }),
        });
    }

    [HttpDelete("conversations/{id}")]
    public async Task<IActionResult> DeleteConversation(int id)
    {
        var conversation = await _db.Conversations
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (conversation == null) return NotFound();

        _db.Messages.RemoveRange(conversation.Messages);
        _db.Conversations.Remove(conversation);
        await _db.SaveChangesAsync();

        return Ok(new { success = true });
    }
}
