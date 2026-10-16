using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.Models;

namespace Backend.Controllers;

[Route("api/messages")]
[ApiController]
[Authorize]
public class MessageController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;

    public MessageController(AppDbContext db, UserManager<AppUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations()
    {
        var userId = _userManager.GetUserId(User);
        var conversations = await _db.Conversations
            .Include(c => c.Artist)
            .Include(c => c.User)
            .Include(c => c.Messages.OrderByDescending(m => m.CreatedAt).Take(1))
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.LastMessageAt)
            .Select(c => new
            {
                c.Id,
                ArtistName = c.Artist.Name,
                ArtistSlug = c.Artist.Slug,
                c.LastMessageAt,
                LastMessage = c.Messages.OrderByDescending(m => m.CreatedAt).Select(m => m.Content).FirstOrDefault(),
                UnreadCount = c.Messages.Count(m => !m.IsRead && m.SenderId != userId),
            })
            .ToListAsync();
        return Ok(conversations);
    }

    [HttpGet("conversations/{id}")]
    public async Task<IActionResult> GetMessages(int id)
    {
        var userId = _userManager.GetUserId(User);
        var conversation = await _db.Conversations
            .Include(c => c.Messages.OrderBy(m => m.CreatedAt))
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

        if (conversation == null) return NotFound();

        var unread = conversation.Messages.Where(m => !m.IsRead && m.SenderId != userId);
        foreach (var msg in unread) msg.IsRead = true;
        await _db.SaveChangesAsync();

        return Ok(conversation.Messages.Select(m => new
        {
            m.Id, m.SenderId, m.Content, m.AttachmentUrl, m.IsRead, m.CreatedAt,
            IsOwn = m.SenderId == userId,
        }));
    }

    [HttpPost("conversations/{id}")]
    public async Task<IActionResult> SendMessage(int id, [FromBody] SendMessageDto dto)
    {
        var userId = _userManager.GetUserId(User);
        var conversation = await _db.Conversations.FindAsync(id);
        if (conversation == null || conversation.UserId != userId) return NotFound();

        var message = new Message
        {
            ConversationId = id,
            SenderId = userId!,
            Content = dto.Content,
        };

        conversation.LastMessageAt = DateTime.UtcNow;
        _db.Messages.Add(message);
        await _db.SaveChangesAsync();
        return Ok(new { message.Id, message = "Message sent." });
    }

    [HttpPost("conversations")]
    public async Task<IActionResult> StartConversation([FromBody] StartConversationDto dto)
    {
        var userId = _userManager.GetUserId(User);
        var existing = await _db.Conversations.FirstOrDefaultAsync(c => c.UserId == userId && c.ArtistId == dto.ArtistId);
        if (existing != null) return Ok(new { existing.Id });

        var conversation = new Conversation
        {
            UserId = userId!,
            ArtistId = dto.ArtistId,
        };
        _db.Conversations.Add(conversation);

        if (!string.IsNullOrWhiteSpace(dto.InitialMessage))
        {
            var msg = new Message
            {
                Conversation = conversation,
                SenderId = userId!,
                Content = dto.InitialMessage,
            };
            _db.Messages.Add(msg);
        }

        await _db.SaveChangesAsync();
        return Ok(new { conversation.Id });
    }
}

public class SendMessageDto
{
    public string Content { get; set; } = string.Empty;
}

public class StartConversationDto
{
    public int ArtistId { get; set; }
    public string? InitialMessage { get; set; }
}
