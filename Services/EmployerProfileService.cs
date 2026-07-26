using Kaarigar.Data;
using Kaarigar.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Kaarigar.Services;

public class EmployerProfileService : IEmployerProfileService
{
    private readonly IEmployerProfileDao _dao;
    private readonly IFileStorageService _fileStorage;
    private readonly ILogger<EmployerProfileService> _logger;

    private static readonly string[] AllowedLogoExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
    private static readonly string[] AllowedProofExtensions = { ".jpg", ".jpeg", ".png", ".pdf" };
    private const long MaxLogoSizeBytes = 2 * 1024 * 1024;   // 2 MB
    private const long MaxProofSizeBytes = 5 * 1024 * 1024;  // 5 MB

    public EmployerProfileService(
        IEmployerProfileDao dao,
        IFileStorageService fileStorage,
        ILogger<EmployerProfileService> logger)
    {
        _dao = dao;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    public async Task<EmployerProfileViewModel?> GetProfileAsync(int userAccountId)
    {
        var loaded = await _dao.GetProfileAsync(userAccountId);
        if (loaded == null) return null;

        var (user, profile) = loaded.Value;
        var categories = await _dao.GetActiveBusinessCategoriesAsync();

        var vm = new EmployerProfileViewModel
        {
            ContactPersonName = profile?.ContactPersonName ?? $"{user.FirstName} {user.LastName}".Trim(),
            ContactNbr = user.ContactNbr,
            EmailId = user.EmailId,

            CompanyName = profile?.CompanyName ?? string.Empty,
            CityName = profile?.CityName,
            AreaAddressTxt = profile?.AreaAddressTxt,
            AddressTxt = profile?.AddressTxt,
            LatitudeNbr = profile?.LatitudeNbr,
            LongitudeNbr = profile?.LongitudeNbr,
            BusinessCategoryId = profile?.BusinessCategoryId,

            ExistingLogoPath = profile?.LogoFilePathTxt,

            BusinessProofTypeCd = profile?.BusinessProofTypeCd,
            BusinessProofNumberTxt = profile?.BusinessProofNumberTxt,
            ExistingProofFileName = profile?.BusinessProofOriginalFileNameTxt,
            ProofReviewStatusCd = profile?.BusinessProofReviewStatusCd,

            BusinessCategoryOptions = categories
                .Select(c => new SelectListItem(c.CategoryName, c.BusinessCategoryId.ToString()))
                .ToList(),
        };

        return vm;
    }

    public async Task<ServiceResult> UpdateProfileAsync(int userAccountId, EmployerProfileViewModel model, string? ipAddress = null)
    {
        var loaded = await _dao.GetProfileAsync(userAccountId);
        if (loaded == null)
            return new ServiceResult(false, "Account not found.");

        // ── Uniqueness checks ────────────────────────────────────────────────
        if (await _dao.IsContactNbrTakenByOthersAsync(model.ContactNbr.Trim(), userAccountId))
            return new ServiceResult(false, "This mobile number is already used by another account.");

        var email = string.IsNullOrWhiteSpace(model.EmailId) ? null : model.EmailId.Trim().ToLowerInvariant();
        if (email != null && await _dao.IsEmailTakenByOthersAsync(email, userAccountId))
            return new ServiceResult(false, "This email address is already used by another account.");

        // ── Business category validity ───────────────────────────────────────
        if (model.BusinessCategoryId == null || !await _dao.IsBusinessCategoryValidAsync(model.BusinessCategoryId.Value))
            return new ServiceResult(false, "Please select a valid business category.");

        // ── Logo upload (optional) ───────────────────────────────────────────
        string? newLogoPath = null;
        if (model.LogoFile != null)
        {
            var logoValidation = ValidateFile(model.LogoFile, AllowedLogoExtensions, MaxLogoSizeBytes, "Logo");
            if (logoValidation != null) return new ServiceResult(false, logoValidation);

            var (relativePath, _) = await _fileStorage.SaveAsync(model.LogoFile, "logos");
            newLogoPath = relativePath;
        }

        // ── Business proof upload (required the first time, optional after) ─
        string? newProofPath = null;
        string? newProofOriginalName = null;

        var alreadyHasProof = !string.IsNullOrWhiteSpace(loaded.Value.Profile?.BusinessProofFilePathTxt);
        if (model.ProofFile == null && !alreadyHasProof)
            return new ServiceResult(false, "Please upload your GST or Gumasthadhara certificate.");

        if (model.ProofFile != null)
        {
            var proofValidation = ValidateFile(model.ProofFile, AllowedProofExtensions, MaxProofSizeBytes, "Business proof");
            if (proofValidation != null) return new ServiceResult(false, proofValidation);

            var (relativePath, _) = await _fileStorage.SaveAsync(model.ProofFile, "business-proofs");
            newProofPath = relativePath;
            newProofOriginalName = model.ProofFile.FileName;
        }

        // ── Persist ───────────────────────────────────────────────────────────
        await _dao.UpdateUserContactFieldsAsync(userAccountId, model.ContactNbr.Trim(), email);

        var oldLogoPath = loaded.Value.Profile?.LogoFilePathTxt;
        var oldProofPath = loaded.Value.Profile?.BusinessProofFilePathTxt;

        await _dao.UpsertProfileAsync(new EmployerProfile
        {
            UserAccountId = userAccountId,
            CompanyName = model.CompanyName.Trim(),
            CityName = model.CityName?.Trim(),
            AreaAddressTxt = model.AreaAddressTxt?.Trim(),
            AddressTxt = model.AddressTxt?.Trim(),
            LatitudeNbr = model.LatitudeNbr,
            LongitudeNbr = model.LongitudeNbr,
            ContactPersonName = model.ContactPersonName.Trim(),
            BusinessCategoryId = model.BusinessCategoryId,
            BusinessProofTypeCd = model.BusinessProofTypeCd,
            BusinessProofNumberTxt = model.BusinessProofNumberTxt?.Trim(),
            LogoFilePathTxt = newLogoPath ?? string.Empty,          // empty = "don't overwrite", see Dao
            BusinessProofFilePathTxt = newProofPath ?? string.Empty,
            BusinessProofOriginalFileNameTxt = newProofOriginalName ?? string.Empty,
        });

        // Clean up replaced files only after the DB write succeeds.
        if (newLogoPath != null && !string.IsNullOrWhiteSpace(oldLogoPath))
            _fileStorage.Delete(oldLogoPath);

        if (newProofPath != null && !string.IsNullOrWhiteSpace(oldProofPath))
            _fileStorage.Delete(oldProofPath);

        _logger.LogInformation("Employer profile updated for UserAccountId={Id}", userAccountId);

        return new ServiceResult(true, "Your profile has been updated successfully.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string? ValidateFile(IFormFile file, string[] allowedExtensions, long maxSizeBytes, string label)
    {
        if (file.Length == 0)
            return $"{label} file is empty.";

        if (file.Length > maxSizeBytes)
            return $"{label} file must be smaller than {maxSizeBytes / (1024 * 1024)} MB.";

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
            return $"{label} must be one of: {string.Join(", ", allowedExtensions)}.";

        return null;
    }
}
