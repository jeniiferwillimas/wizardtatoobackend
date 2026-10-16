using System.Net;
using System.Net.Mail;

namespace Backend.Services;

public class EmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string htmlBody)
    {
        var host = _config["Mail:Host"];
        var port = int.Parse(_config["Mail:Port"] ?? "587");
        var username = _config["Mail:Username"];
        var password = _config["Mail:Password"];
        var fromAddress = _config["Mail:FromAddress"] ?? "noreply@tattonest.com";
        var fromName = _config["Mail:FromName"] ?? "TattooNest";

        if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(password))
        {
            _logger.LogWarning("Mail not configured, skipping email to {To}: {Subject}", to, subject);
            return;
        }

        using var client = new SmtpClient(host, port)
        {
            Credentials = new NetworkCredential(username, password),
            EnableSsl = true,
            Timeout = 15000,
        };

        var message = new MailMessage
        {
            From = new MailAddress(fromAddress, fromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true,
        };
        message.To.Add(to);

        try
        {
            await client.SendMailAsync(message);
            _logger.LogInformation("Email sent to {To}: {Subject}", to, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}: {Subject}", to, subject);
        }
    }

    public async Task SendBookingConfirmationToClient(string clientEmail, string clientName, string artistName, string style, string size, decimal? price, DateTime preferredDate)
    {
        var subject = $"Booking Confirmed — {artistName}";
        var html = $"""
        <div style="font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif; max-width: 560px; margin: 0 auto; padding: 32px 24px; color: #1a1a1a;">
          <h1 style="font-size: 24px; margin: 0 0 8px;">Booking Confirmed</h1>
          <p style="color: #666; margin: 0 0 24px;">Hi {clientName}, your tattoo appointment has been booked.</p>
          <table style="width: 100%; border-collapse: collapse; margin-bottom: 24px;">
            <tr><td style="padding: 10px 0; border-bottom: 1px solid #eee; color: #999; width: 120px;">Artist</td><td style="padding: 10px 0; border-bottom: 1px solid #eee; font-weight: 600;">{artistName}</td></tr>
            <tr><td style="padding: 10px 0; border-bottom: 1px solid #eee; color: #999;">Style</td><td style="padding: 10px 0; border-bottom: 1px solid #eee;">{style}</td></tr>
            <tr><td style="padding: 10px 0; border-bottom: 1px solid #eee; color: #999;">Size</td><td style="padding: 10px 0; border-bottom: 1px solid #eee;">{size}</td></tr>
            <tr><td style="padding: 10px 0; border-bottom: 1px solid #eee; color: #999;">Est. Price</td><td style="padding: 10px 0; border-bottom: 1px solid #eee; font-weight: 600;">${price?.ToString("N2") ?? "TBD"}</td></tr>
            <tr><td style="padding: 10px 0; border-bottom: 1px solid #eee; color: #999;">Date</td><td style="padding: 10px 0; border-bottom: 1px solid #eee;">{preferredDate:MMMM d, yyyy}</td></tr>
          </table>
          <p style="color: #666; font-size: 14px;">Your artist will reach out to confirm the details. If you have any questions, reply to this email.</p>
          <p style="margin-top: 32px; color: #999; font-size: 12px;">— TattooNest</p>
        </div>
        """;
        await SendAsync(clientEmail, subject, html);
    }

    public async Task SendBookingNotificationToArtist(string artistEmail, string artistName, string clientName, string clientEmail, string description, string style, string size, string placement, decimal? price, DateTime preferredDate)
    {
        var subject = $"New Booking from {clientName}";
        var html = $"""
        <div style="font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif; max-width: 560px; margin: 0 auto; padding: 32px 24px; color: #1a1a1a;">
          <h1 style="font-size: 24px; margin: 0 0 8px;">New Booking Request</h1>
          <p style="color: #666; margin: 0 0 24px;">Hi {artistName}, you have a new tattoo booking.</p>
          <table style="width: 100%; border-collapse: collapse; margin-bottom: 24px;">
            <tr><td style="padding: 10px 0; border-bottom: 1px solid #eee; color: #999; width: 120px;">Client</td><td style="padding: 10px 0; border-bottom: 1px solid #eee; font-weight: 600;">{clientName}</td></tr>
            <tr><td style="padding: 10px 0; border-bottom: 1px solid #eee; color: #999;">Email</td><td style="padding: 10px 0; border-bottom: 1px solid #eee;">{clientEmail}</td></tr>
            <tr><td style="padding: 10px 0; border-bottom: 1px solid #eee; color: #999;">Description</td><td style="padding: 10px 0; border-bottom: 1px solid #eee;">{description}</td></tr>
            <tr><td style="padding: 10px 0; border-bottom: 1px solid #eee; color: #999;">Style</td><td style="padding: 10px 0; border-bottom: 1px solid #eee;">{style}</td></tr>
            <tr><td style="padding: 10px 0; border-bottom: 1px solid #eee; color: #999;">Size</td><td style="padding: 10px 0; border-bottom: 1px solid #eee;">{size}</td></tr>
            <tr><td style="padding: 10px 0; border-bottom: 1px solid #eee; color: #999;">Placement</td><td style="padding: 10px 0; border-bottom: 1px solid #eee;">{placement}</td></tr>
            <tr><td style="padding: 10px 0; border-bottom: 1px solid #eee; color: #999;">Est. Price</td><td style="padding: 10px 0; border-bottom: 1px solid #eee; font-weight: 600;">${price?.ToString("N2") ?? "TBD"}</td></tr>
            <tr><td style="padding: 10px 0; border-bottom: 1px solid #eee; color: #999;">Date</td><td style="padding: 10px 0; border-bottom: 1px solid #eee;">{preferredDate:MMMM d, yyyy}</td></tr>
          </table>
          <p style="color: #666; font-size: 14px;">Log in to your dashboard to accept or manage this booking.</p>
          <p style="margin-top: 32px; color: #999; font-size: 12px;">— TattooNest</p>
        </div>
        """;
        await SendAsync(artistEmail, subject, html);
    }

    public async Task SendPaymentConfirmationToClient(string clientEmail, string clientName, string artistName, decimal amount, int bookingId)
    {
        var subject = $"Payment Received — Booking #{bookingId}";
        var html = $"""
        <div style="font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif; max-width: 560px; margin: 0 auto; padding: 32px 24px; color: #1a1a1a;">
          <div style="text-align: center; margin-bottom: 24px;">
            <div style="display: inline-block; background: #16a34a; color: white; width: 48px; height: 48px; border-radius: 50%; line-height: 48px; font-size: 24px;">✓</div>
          </div>
          <h1 style="font-size: 24px; margin: 0 0 8px; text-align: center;">Payment Confirmed</h1>
          <p style="color: #666; margin: 0 0 24px; text-align: center;">Your payment of <strong style="color: #1a1a1a;">${amount:N2}</strong> has been received.</p>
          <table style="width: 100%; border-collapse: collapse; margin-bottom: 24px;">
            <tr><td style="padding: 10px 0; border-bottom: 1px solid #eee; color: #999; width: 120px;">Booking</td><td style="padding: 10px 0; border-bottom: 1px solid #eee; font-weight: 600;">#{bookingId}</td></tr>
            <tr><td style="padding: 10px 0; border-bottom: 1px solid #eee; color: #999;">Artist</td><td style="padding: 10px 0; border-bottom: 1px solid #eee;">{artistName}</td></tr>
            <tr><td style="padding: 10px 0; border-bottom: 1px solid #eee; color: #999;">Amount Paid</td><td style="padding: 10px 0; border-bottom: 1px solid #eee; font-weight: 600; color: #16a34a;">${amount:N2}</td></tr>
            <tr><td style="padding: 10px 0; border-bottom: 1px solid #eee; color: #999;">Status</td><td style="padding: 10px 0; border-bottom: 1px solid #eee; font-weight: 600; color: #16a34a;">Confirmed</td></tr>
          </table>
          <p style="color: #666; font-size: 14px;">Your appointment is confirmed. The artist will reach out with next steps.</p>
          <p style="margin-top: 32px; color: #999; font-size: 12px;">— TattooNest</p>
        </div>
        """;
        await SendAsync(clientEmail, subject, html);
    }

    public async Task SendPaymentNotificationToAdmin(string artistName, string clientName, decimal amount, int bookingId)
    {
        var notifyEmail = _config["Mail:NotifyEmail"];
        if (string.IsNullOrEmpty(notifyEmail)) return;

        var subject = $"Payment Received — ${amount:N2} for Booking #{bookingId}";
        var html = $"""
        <div style="font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif; max-width: 560px; margin: 0 auto; padding: 32px 24px; color: #1a1a1a;">
          <h1 style="font-size: 24px; margin: 0 0 8px;">Payment Received</h1>
          <table style="width: 100%; border-collapse: collapse; margin-bottom: 24px;">
            <tr><td style="padding: 10px 0; border-bottom: 1px solid #eee; color: #999; width: 120px;">Booking</td><td style="padding: 10px 0; border-bottom: 1px solid #eee;">#{bookingId}</td></tr>
            <tr><td style="padding: 10px 0; border-bottom: 1px solid #eee; color: #999;">Client</td><td style="padding: 10px 0; border-bottom: 1px solid #eee;">{clientName}</td></tr>
            <tr><td style="padding: 10px 0; border-bottom: 1px solid #eee; color: #999;">Artist</td><td style="padding: 10px 0; border-bottom: 1px solid #eee;">{artistName}</td></tr>
            <tr><td style="padding: 10px 0; border-bottom: 1px solid #eee; color: #999;">Amount</td><td style="padding: 10px 0; border-bottom: 1px solid #eee; font-weight: 600; color: #16a34a;">${amount:N2}</td></tr>
          </table>
          <p style="margin-top: 32px; color: #999; font-size: 12px;">— TattooNest</p>
        </div>
        """;
        await SendAsync(notifyEmail, subject, html);
    }
}
