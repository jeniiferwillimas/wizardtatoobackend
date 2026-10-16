using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.Models;
using Backend.Services;

namespace Backend.Controllers;

[Route("api/payments")]
[ApiController]
[EnableRateLimiting("payment")]
public class PaymentController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly EmailService _email;

    public PaymentController(
        AppDbContext db,
        UserManager<AppUser> userManager,
        IConfiguration config,
        IHttpClientFactory httpClientFactory,
        EmailService email)
    {
        _db = db;
        _userManager = userManager;
        _config = config;
        _httpClientFactory = httpClientFactory;
        _email = email;
    }

    [HttpGet("wallet-config")]
    [AllowAnonymous]
    public IActionResult GetWalletConfig()
    {
        var address = _config["CardRamp:AlchemyPayMerchantWallet"]
                   ?? _config["Crypto:WalletAddress"] ?? "";
        return Ok(new { success = true, data = new { walletAddress = address } });
    }

    [HttpPost("crypto/checkout")]
    [AllowAnonymous]
    public async Task<IActionResult> CryptoCheckout([FromBody] CryptoCheckoutDto dto)
    {
        if (dto.Amount <= 0)
            return BadRequest(new { success = false, message = "Invalid amount." });

        var apiKey = _config["NowPayments:ApiKey"] ?? "";
        var baseUrl = _config["NowPayments:BaseUrl"] ?? "https://api.nowpayments.io/v1";
        var priceCurrency = _config["NowPayments:PriceCurrency"] ?? "usd";

        if (string.IsNullOrEmpty(apiKey))
            return StatusCode(503, new { success = false, message = "Crypto payment is not configured yet." });

        var orderId = $"PAY-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";

        if (dto.BookingId.HasValue)
        {
            var booking = await _db.Bookings.FindAsync(dto.BookingId.Value);
            if (booking != null)
            {
                booking.PaymentStatus = "Pending";
                booking.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                orderId = $"BOOKING-{booking.Id}";
            }
        }

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("x-api-key", apiKey);

        var payload = new
        {
            price_amount = dto.Amount,
            price_currency = priceCurrency,
            order_id = orderId,
            order_description = $"TattooNest payment {orderId}",
        };

        var content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        var response = await client.PostAsync($"{baseUrl}/invoice", content);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            return StatusCode(500, new { success = false, message = $"NOWPayments invoice creation failed: {body}" });

        var result = JsonSerializer.Deserialize<JsonElement>(body);
        var invoiceUrl = result.TryGetProperty("invoice_url", out var urlProp) ? urlProp.GetString() : null;

        return Ok(new { success = true, data = new { invoiceUrl, orderId, amount = dto.Amount } });
    }

    [HttpGet("card-ramp-url")]
    [AllowAnonymous]
    public IActionResult GetCardRampUrl([FromQuery] string provider, [FromQuery] decimal amount)
    {
        if (amount <= 0)
            return BadRequest(new { success = false, message = "Invalid amount." });

        var walletAddress = _config["CardRamp:AlchemyPayMerchantWallet"]
                         ?? _config["Crypto:WalletAddress"] ?? "";

        if (string.IsNullOrEmpty(walletAddress))
            return StatusCode(500, new { success = false, message = "Deposit wallet not configured." });

        var alchemyUrl = _config["CardRamp:AlchemyPayRampUrl"] ?? "https://ramp.alchemypay.org";
        var moonpayUrl = _config["CardRamp:MoonPayRampUrl"] ?? "https://buy.moonpay.com";
        var transakUrl = _config["CardRamp:TransakRampUrl"] ?? "https://transak.com/buy";

        var url = provider?.ToLower() switch
        {
            "alchemy" => $"{alchemyUrl}/?crypto=ETH&fiat=USD&amount={amount}&network=ETH",
            "moonpay" => $"{moonpayUrl}/?currencyCode=eth&baseCurrencyAmount={amount}&baseCurrencyCode=usd",
            "transak" => transakUrl,
            _ => "",
        };

        if (string.IsNullOrEmpty(url))
            return BadRequest(new { success = false, message = "Invalid provider. Use alchemy, moonpay, or transak." });

        return Ok(new { success = true, data = new { url, walletAddress, provider, amount } });
    }

    [HttpPost("booking/checkout")]
    [Authorize]
    public async Task<IActionResult> CreateBookingCheckout([FromBody] BookingCheckoutDto dto)
    {
        var userId = _userManager.GetUserId(User);
        var booking = await _db.Bookings
            .Include(b => b.Artist)
            .FirstOrDefaultAsync(b => b.Id == dto.BookingId && b.UserId == userId);

        if (booking == null)
            return NotFound(new { error = "Booking not found." });

        if (booking.PaymentStatus == "Paid")
            return BadRequest(new { error = "This booking is already paid." });

        booking.PaymentStatus = "Pending";
        booking.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new {
            bookingId = booking.Id,
            amount = booking.EstimatedPrice ?? 0,
            productName = $"Tattoo Session — {booking.Artist.Name}",
        });
    }

    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> HandleIpnWebhook()
    {
        var body = await new StreamReader(Request.Body).ReadToEndAsync();
        var signature = Request.Headers["x-nowpayments-sig"].FirstOrDefault() ?? "";
        var ipnSecret = _config["NowPayments:IpnSecret"] ?? "";

        if (!string.IsNullOrEmpty(ipnSecret) && !string.IsNullOrEmpty(signature))
        {
            var payload = JsonSerializer.Deserialize<SortedDictionary<string, JsonElement>>(body);
            if (payload != null)
            {
                var sorted = JsonSerializer.Serialize(payload);
                var hmac = HMACSHA512.HashData(
                    Encoding.UTF8.GetBytes(ipnSecret),
                    Encoding.UTF8.GetBytes(sorted));
                var expected = Convert.ToHexStringLower(hmac);

                if (!string.Equals(expected, signature, StringComparison.OrdinalIgnoreCase))
                    return StatusCode(403, new { success = false, message = "Invalid signature." });
            }
        }

        var data = JsonSerializer.Deserialize<JsonElement>(body);
        var orderId = data.TryGetProperty("order_id", out var oidProp) ? oidProp.GetString() ?? "" : "";
        var paymentStatus = data.TryGetProperty("payment_status", out var psProp) ? psProp.GetString() ?? "" : "";
        var isPaid = paymentStatus is "finished" or "confirmed";

        if (orderId.StartsWith("BOOKING-"))
        {
            if (int.TryParse(orderId.Replace("BOOKING-", ""), out var bookingId))
            {
                var booking = await _db.Bookings
                    .Include(b => b.User)
                    .Include(b => b.Artist)
                    .FirstOrDefaultAsync(b => b.Id == bookingId);
                if (booking != null)
                {
                    booking.PaymentStatus = isPaid ? "Paid" : "Failed";
                    if (isPaid)
                    {
                        booking.Status = "Confirmed";
                        booking.PaidAt = DateTime.UtcNow;

                        var paidAmount = booking.EstimatedPrice ?? 0;
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                if (booking.User?.Email != null)
                                    await _email.SendPaymentConfirmationToClient(
                                        booking.User.Email, booking.User.FullName ?? "there",
                                        booking.Artist.Name, paidAmount, booking.Id);

                                await _email.SendPaymentNotificationToAdmin(
                                    booking.Artist.Name, booking.User?.FullName ?? "A client",
                                    paidAmount, booking.Id);
                            }
                            catch { }
                        });
                    }
                    booking.UpdatedAt = DateTime.UtcNow;
                    await _db.SaveChangesAsync();
                }
            }
        }
        else if (orderId.StartsWith("SUB-"))
        {
            var parts = orderId.Replace("SUB-", "").Split('-');
            if (int.TryParse(parts[0], out var profileId))
            {
                var sub = await _db.ArtistSubscriptions
                    .FirstOrDefaultAsync(s => s.ArtistProfileId == profileId);
                if (sub != null)
                {
                    if (isPaid)
                    {
                        sub.Status = "Active";
                        sub.CurrentPeriodStart = DateTime.UtcNow;
                        sub.CurrentPeriodEnd = DateTime.UtcNow.AddMonths(1);
                    }
                    else
                    {
                        sub.Status = "Cancelled";
                    }
                    sub.UpdatedAt = DateTime.UtcNow;
                    await _db.SaveChangesAsync();
                }
            }
        }

        return Ok(new { success = true });
    }

    [HttpGet("booking/{bookingId}/status")]
    [Authorize]
    public async Task<IActionResult> GetBookingPaymentStatus(int bookingId)
    {
        var userId = _userManager.GetUserId(User);
        var booking = await _db.Bookings
            .Where(b => b.Id == bookingId && b.UserId == userId)
            .Select(b => new { b.Id, b.PaymentStatus, b.AmountPaid, b.PaidAt, b.Status })
            .FirstOrDefaultAsync();

        if (booking == null) return NotFound();
        return Ok(booking);
    }

    [HttpGet("subscription/status")]
    [Authorize]
    public async Task<IActionResult> GetSubscriptionStatus()
    {
        var userId = _userManager.GetUserId(User);
        var profile = await _db.ArtistProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        if (profile == null) return NotFound();

        var sub = await _db.ArtistSubscriptions
            .Where(s => s.ArtistProfileId == profile.Id)
            .Select(s => new
            {
                s.Plan, s.Status, s.Amount, s.Currency,
                s.CurrentPeriodStart, s.CurrentPeriodEnd,
            })
            .FirstOrDefaultAsync();

        return Ok(sub ?? (object)new { Plan = (string?)null, Status = "None" });
    }

    [HttpPost("confirm")]
    [AllowAnonymous]
    public IActionResult ConfirmPayment([FromBody] PaymentConfirmDto dto)
    {
        if (string.IsNullOrEmpty(dto.Email))
            return BadRequest(new { success = false, message = "Email is required." });
        if (dto.Amount <= 0)
            return BadRequest(new { success = false, message = "Amount is required." });

        var notifyEmail = _config["Mail:NotifyEmail"];
        if (string.IsNullOrEmpty(notifyEmail))
            return Ok(new { success = true, message = "Confirmation received." });

        var safeName = WebUtility.HtmlEncode(dto.Name ?? "—");
        var safeEmail = WebUtility.HtmlEncode(dto.Email);
        var safeMethod = WebUtility.HtmlEncode(dto.Method ?? "—");
        var safeTxId = WebUtility.HtmlEncode(dto.TransactionId ?? "—");
        var safeNotes = WebUtility.HtmlEncode(dto.Notes ?? "—");

        var subject = $"Payment Confirmation — ${dto.Amount:N2} from {safeName}";
        var html = $"""
        <div style="font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif; max-width: 560px; margin: 0 auto; padding: 32px 24px; color: #1a1a1a;">
          <h1 style="font-size: 24px; margin: 0 0 16px;">Payment Confirmation Submitted</h1>
          <table style="width: 100%; border-collapse: collapse; margin-bottom: 24px;">
            <tr><td style="padding: 10px 0; border-bottom: 1px solid #eee; color: #999; width: 120px;">Name</td><td style="padding: 10px 0; border-bottom: 1px solid #eee; font-weight: 600;">{safeName}</td></tr>
            <tr><td style="padding: 10px 0; border-bottom: 1px solid #eee; color: #999;">Email</td><td style="padding: 10px 0; border-bottom: 1px solid #eee;">{safeEmail}</td></tr>
            <tr><td style="padding: 10px 0; border-bottom: 1px solid #eee; color: #999;">Amount</td><td style="padding: 10px 0; border-bottom: 1px solid #eee; font-weight: 600;">${dto.Amount:N2}</td></tr>
            <tr><td style="padding: 10px 0; border-bottom: 1px solid #eee; color: #999;">Method</td><td style="padding: 10px 0; border-bottom: 1px solid #eee;">{safeMethod}</td></tr>
            <tr><td style="padding: 10px 0; border-bottom: 1px solid #eee; color: #999;">Tx ID</td><td style="padding: 10px 0; border-bottom: 1px solid #eee; font-family: monospace; font-size: 12px;">{safeTxId}</td></tr>
            <tr><td style="padding: 10px 0; border-bottom: 1px solid #eee; color: #999;">Notes</td><td style="padding: 10px 0; border-bottom: 1px solid #eee;">{safeNotes}</td></tr>
          </table>
          <p style="color: #666; font-size: 14px;">Please verify this payment and update the booking status.</p>
          <p style="margin-top: 32px; color: #999; font-size: 12px;">— TattooNest</p>
        </div>
        """;

        var userEmail = dto.Email;
        var userAckHtml = $"""
            <div style="font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif; max-width: 560px; margin: 0 auto; padding: 32px 24px; color: #1a1a1a;">
              <h1 style="font-size: 24px; margin: 0 0 8px;">We received your payment confirmation</h1>
              <p style="color: #666;">Hi {safeName}, we've received your payment confirmation of <strong>${dto.Amount:N2}</strong>. We'll verify it and update your booking shortly.</p>
              <p style="color: #666; margin-top: 16px;">If you have any questions, reply to this email.</p>
              <p style="margin-top: 32px; color: #999; font-size: 12px;">— TattooNest</p>
            </div>
            """;

        _ = Task.Run(async () =>
        {
            try { await _email.SendAsync(notifyEmail, subject, html); } catch { }
            try { await _email.SendAsync(userEmail, "Payment Confirmation Received", userAckHtml); } catch { }
        });

        return Ok(new { success = true, message = "Payment confirmation sent. You'll receive an email shortly." });
    }

    [HttpGet("plans")]
    [AllowAnonymous]
    public IActionResult GetPlans()
    {
        var plans = new[]
        {
            new { Name = "Basic", Price = 19.99m, Features = new[] { "Listed in marketplace", "Up to 20 portfolio images", "Basic analytics" } },
            new { Name = "Pro", Price = 39.99m, Features = new[] { "Everything in Basic", "Up to 50 portfolio images", "Priority placement", "Booking management", "Advanced analytics" } },
            new { Name = "Premium", Price = 79.99m, Features = new[] { "Everything in Pro", "Unlimited portfolio images", "Featured artist badge", "Direct messaging", "Custom profile theme" } },
        };
        return Ok(plans);
    }
}

public class BookingCheckoutDto
{
    public int BookingId { get; set; }
}

public class SubscriptionCheckoutDto
{
    public string Plan { get; set; } = "Basic";
}

public class CryptoCheckoutDto
{
    public decimal Amount { get; set; }
    public int? BookingId { get; set; }
}

public class PaymentConfirmDto
{
    public string? Name { get; set; }
    public string Email { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Method { get; set; }
    public string? TransactionId { get; set; }
    public string? Notes { get; set; }
}
