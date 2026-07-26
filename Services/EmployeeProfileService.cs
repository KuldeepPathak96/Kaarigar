using Kaarigar.Data;
using Kaarigar.Models;
using Microsoft.AspNetCore.Http;

namespace Kaarigar.Services;

public class EmployeeProfileService : IEmployeeProfileService
{
    private readonly IEmployeeProfileDao _dao;
    private readonly IFileStorageService _fileStorage;
    private readonly ILogger<EmployeeProfileService> _logger;

    private static readonly string[] AllowedIdProofExtensions = { ".jpg", ".jpeg", ".png", ".pdf" };
    private static readonly string[] AllowedCvExtensions = { ".pdf", ".doc", ".docx" };
    private const long MaxIdProofSizeBytes = 5 * 1024 * 1024; // 5 MB
    private const long MaxCvSizeBytes = 5 * 1024 * 1024;      // 5 MB

    private static readonly HashSet<string> AllowedIdProofTypes = new() { "AADHAAR", "PAN", "VOTER_ID", "DRIVING_LICENSE" };

    public EmployeeProfileService(
        IEmployeeProfileDao dao,
        IFileStorageService fileStorage,
        ILogger<EmployeeProfileService> logger)
    {
        _dao = dao;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    public async Task<EmployeeProfileViewModel?> GetProfileAsync(int userAccountId)
    {
        var loaded = await _dao.GetProfileAsync(userAccountId);
        if (loaded == null) return null;

        var (user, profile) = loaded.Value;
        var skillIds = await _dao.GetSkillIdsAsync(userAccountId);
        var skillOptions = await _dao.GetActiveSkillsAsync();
        var idProofDoc = await _dao.GetDocumentAsync(userAccountId, "ID_PROOF");
        var cvDoc = await _dao.GetDocumentAsync(userAccountId, "RESUME");

        var vm = new EmployeeProfileViewModel
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
            ContactNbr = user.ContactNbr,
            EmailId = user.EmailId,

            CityName = profile?.CityName,
            AreaAddressTxt = profile?.AreaAddressTxt,
            AddressTxt = profile?.AddressTxt,
            LatitudeNbr = profile?.LatitudeNbr,
            LongitudeNbr = profile?.LongitudeNbr,
            PreferredRadiusNbr = profile?.PreferredRadiusNbr,
            IsNotificationEnabledFl = profile?.IsNotificationEnabledFl ?? true,

            SelectedSkillIds = skillIds,
            SkillOptions = skillOptions,

            IdProofTypeCd = idProofDoc?.DocumentSubtypeCd,
            ExistingIdProofFileName = idProofDoc?.OriginalFileNameTxt,
            IdProofReviewStatusCd = idProofDoc?.ReviewStatusCd,

            ExistingCvFileName = cvDoc?.OriginalFileNameTxt,
            CvReviewStatusCd = cvDoc?.ReviewStatusCd,
        };

        vm.ProfileCompletenessPercent = ComputeCompleteness(profile, skillIds, idProofDoc, cvDoc);

        return vm;
    }

    public async Task<ServiceResult> UpdateProfileAsync(int userAccountId, EmployeeProfileViewModel model, string? ipAddress = null)
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

        // ── Skills ────────────────────────────────────────────────────────────
        if (model.SelectedSkillIds == null || model.SelectedSkillIds.Count == 0)
            return new ServiceResult(false, "Please select at least one skill.");

        // ── ID Proof (required first-time, optional after) ─────────────────
        var existingIdProof = await _dao.GetDocumentAsync(userAccountId, "ID_PROOF");

        if (string.IsNullOrWhiteSpace(model.IdProofTypeCd) || !AllowedIdProofTypes.Contains(model.IdProofTypeCd))
            return new ServiceResult(false, "Please select a valid ID proof type (Aadhaar, PAN, Voter ID, or Driving Licence).");

        if (model.IdProofFile == null && existingIdProof == null)
            return new ServiceResult(false, "Please upload an ID proof document.");

        string? idProofLastFour = null;
        if (model.IdProofFile != null)
        {
            var idValidation = ValidateFile(model.IdProofFile, AllowedIdProofExtensions, MaxIdProofSizeBytes, "ID proof");
            if (idValidation != null) return new ServiceResult(false, idValidation);
        }

        // ── CV / Resume (optional) ──────────────────────────────────────────
        if (model.CvFile != null)
        {
            var cvValidation = ValidateFile(model.CvFile, AllowedCvExtensions, MaxCvSizeBytes, "CV/Resume");
            if (cvValidation != null) return new ServiceResult(false, cvValidation);
        }

        // ── Persist basic + location fields ─────────────────────────────────
        await _dao.UpdateUserFieldsAsync(userAccountId, model.FirstName.Trim(), model.LastName?.Trim(), model.ContactNbr.Trim(), email);

        await _dao.UpsertProfileAsync(new EmployeeProfile
        {
            UserAccountId = userAccountId,
            CityName = model.CityName?.Trim(),
            AreaAddressTxt = model.AreaAddressTxt?.Trim(),
            AddressTxt = model.AddressTxt?.Trim(),
            LatitudeNbr = model.LatitudeNbr,
            LongitudeNbr = model.LongitudeNbr,
            PreferredRadiusNbr = model.PreferredRadiusNbr,
            IsNotificationEnabledFl = model.IsNotificationEnabledFl,
        });

        await _dao.ReplaceSkillsAsync(userAccountId, model.SelectedSkillIds);

        // ── Save ID proof file + row (only if a new file was uploaded) ──────
        if (model.IdProofFile != null)
        {
            var (relativePath, storedFileName) = await _fileStorage.SaveAsync(model.IdProofFile, "id-proofs");
            var oldPath = existingIdProof?.ServerFilePathTxt;

            await _dao.UpsertDocumentAsync(new EmployeeDocument
            {
                UserAccountId = userAccountId,
                DocumentTypeCd = "ID_PROOF",
                DocumentSubtypeCd = model.IdProofTypeCd,
                IdLastFourDigitTxt = idProofLastFour,
                OriginalFileNameTxt = model.IdProofFile.FileName,
                StoredFileNameTxt = storedFileName,
                ServerFilePathTxt = relativePath,
                FileSizeKbNbr = (int)(model.IdProofFile.Length / 1024),
                MimeTypeTxt = model.IdProofFile.ContentType,
            });

            if (!string.IsNullOrWhiteSpace(oldPath))
                _fileStorage.Delete(oldPath);
        }
        else if (existingIdProof != null && model.IdProofTypeCd != existingIdProof.DocumentSubtypeCd)
        {
            // Employee changed the declared ID type without re-uploading a new file —
            // still worth recording so Admin knows what to expect from the existing file.
            existingIdProof.DocumentSubtypeCd = model.IdProofTypeCd;
            await _dao.UpsertDocumentAsync(existingIdProof);
        }

        // ── Save CV file + row (only if a new file was uploaded) ────────────
        if (model.CvFile != null)
        {
            var existingCv = await _dao.GetDocumentAsync(userAccountId, "RESUME");
            var (relativePath, storedFileName) = await _fileStorage.SaveAsync(model.CvFile, "resumes");
            var oldPath = existingCv?.ServerFilePathTxt;

            await _dao.UpsertDocumentAsync(new EmployeeDocument
            {
                UserAccountId = userAccountId,
                DocumentTypeCd = "RESUME",
                OriginalFileNameTxt = model.CvFile.FileName,
                StoredFileNameTxt = storedFileName,
                ServerFilePathTxt = relativePath,
                FileSizeKbNbr = (int)(model.CvFile.Length / 1024),
                MimeTypeTxt = model.CvFile.ContentType,
            });

            if (!string.IsNullOrWhiteSpace(oldPath))
                _fileStorage.Delete(oldPath);
        }

        _logger.LogInformation("Employee profile updated for UserAccountId={Id}", userAccountId);

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

    private static int ComputeCompleteness(EmployeeProfile? profile, HashSet<int> skillIds, EmployeeDocument? idProof, EmployeeDocument? cv)
    {
        var checks = new[]
        {
            !string.IsNullOrWhiteSpace(profile?.CityName),
            profile?.LatitudeNbr != null && profile?.LongitudeNbr != null,
            skillIds.Count > 0,
            idProof != null,
            cv != null,
        };

        var completed = checks.Count(c => c);
        return (int)Math.Round(completed * 100.0 / checks.Length);
    }
}
