using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace Kaarigar.Services;

public class FileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<FileStorageService> _logger;

    public FileStorageService(IWebHostEnvironment env, ILogger<FileStorageService> logger)
    {
        _env = env;
        _logger = logger;
    }

    public async Task<(string RelativePath, string StoredFileName)> SaveAsync(IFormFile file, string subFolder)
    {
        var uploadsRoot = Path.Combine(_env.WebRootPath, "uploads", subFolder);
        Directory.CreateDirectory(uploadsRoot);

        var extension = Path.GetExtension(file.FileName);
        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(uploadsRoot, storedFileName);

        using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var relativePath = $"/uploads/{subFolder}/{storedFileName}";
        return (relativePath, storedFileName);
    }

    public void Delete(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return;

        try
        {
            var fullPath = Path.Combine(_env.WebRootPath, relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(fullPath))
                File.Delete(fullPath);
        }
        catch (Exception ex)
        {
            // Deleting an old file is best-effort cleanup — never fail the
            // request (e.g. a profile update) just because cleanup failed.
            _logger.LogWarning(ex, "Failed to delete file at {Path}", relativePath);
        }
    }
}
