namespace Backend.Services;

public class FileUploadService
{
    private readonly string _uploadRoot;
    private readonly string[] _allowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];
    private const long MaxFileSize = 10 * 1024 * 1024;

    public FileUploadService(IWebHostEnvironment env)
    {
        _uploadRoot = Path.Combine(env.ContentRootPath, "wwwroot", "uploads");
        Directory.CreateDirectory(_uploadRoot);
    }

    public async Task<(bool Success, string? Url, string? Error)> UploadAsync(IFormFile file, string folder)
    {
        if (file.Length == 0)
            return (false, null, "File is empty");

        if (file.Length > MaxFileSize)
            return (false, null, "File exceeds 10MB limit");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_allowedExtensions.Contains(ext))
            return (false, null, $"File type {ext} not allowed. Use: {string.Join(", ", _allowedExtensions)}");

        var dir = Path.Combine(_uploadRoot, folder);
        Directory.CreateDirectory(dir);

        var fileName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(dir, fileName);

        using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);

        var url = $"/uploads/{folder}/{fileName}";
        return (true, url, null);
    }

    public bool Delete(string url)
    {
        if (string.IsNullOrEmpty(url)) return false;

        var relativePath = url.TrimStart('/');
        var fullPath = Path.Combine(Path.GetDirectoryName(_uploadRoot)!, relativePath);

        if (!File.Exists(fullPath)) return false;

        File.Delete(fullPath);
        return true;
    }
}
