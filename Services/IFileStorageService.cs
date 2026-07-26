using Microsoft.AspNetCore.Http;

namespace Kaarigar.Services;

/// <summary>
/// Thin abstraction over saving uploaded files to disk under wwwroot, so
/// callers don't repeat path-building / unique-naming logic, and so this can
/// be swapped for blob storage later without touching calling services.
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Saves the file under wwwroot/uploads/{subFolder}/ with a generated
    /// unique file name, and returns the web-relative path (e.g.
    /// "/uploads/logos/8f3a...png") to store in the DB.
    /// </summary>
    Task<(string RelativePath, string StoredFileName)> SaveAsync(IFormFile file, string subFolder);

    /// <summary>Deletes a previously-saved file given the relative path returned by SaveAsync. Safe to call on a missing file.</summary>
    void Delete(string relativePath);
}
