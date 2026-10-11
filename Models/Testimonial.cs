namespace Backend.Models;

public class Testimonial
{
    public int Id { get; set; }
    public string Quote { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public string AuthorInitials { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public bool IsVerified { get; set; }
    public int OrderNo { get; set; }
}
