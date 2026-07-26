using Kaarigar.Models;
using Microsoft.EntityFrameworkCore;

namespace Kaarigar.Data;

public class EmployeeProfileDao : IEmployeeProfileDao
{
    private readonly AppDbContext _db;

    public EmployeeProfileDao(AppDbContext db)
    {
        _db = db;
    }

    public async Task<(UserAccount User, EmployeeProfile? Profile)?> GetProfileAsync(int userAccountId)
    {
        var user = await _db.UserAccounts.FirstOrDefaultAsync(u => u.UserAccountId == userAccountId);
        if (user == null) return null;

        var profile = await _db.EmployeeProfiles
            .FirstOrDefaultAsync(p => p.UserAccountId == userAccountId);

        return (user, profile);
    }

    public async Task<HashSet<int>> GetSkillIdsAsync(int userAccountId)
    {
        var ids = await _db.EmployeeSkills.AsNoTracking()
            .Where(s => s.UserAccountId == userAccountId)
            .Select(s => s.SkillId)
            .ToListAsync();

        return ids.ToHashSet();
    }

    public Task<EmployeeDocument?> GetDocumentAsync(int userAccountId, string documentTypeCd) =>
        _db.EmployeeDocuments.AsNoTracking()
           .FirstOrDefaultAsync(d => d.UserAccountId == userAccountId && d.DocumentTypeCd == documentTypeCd);

    public Task<bool> IsContactNbrTakenByOthersAsync(string contactNbr, int excludeUserAccountId) =>
        _db.UserAccounts.AnyAsync(u => u.ContactNbr == contactNbr && u.UserAccountId != excludeUserAccountId);

    public Task<bool> IsEmailTakenByOthersAsync(string emailId, int excludeUserAccountId) =>
        _db.UserAccounts.AnyAsync(u => u.EmailId == emailId && u.UserAccountId != excludeUserAccountId);

    public async Task UpdateUserFieldsAsync(int userAccountId, string firstName, string? lastName, string contactNbr, string? emailId)
    {
        var user = await _db.UserAccounts.FindAsync(userAccountId);
        if (user == null) return;

        user.FirstName = firstName;
        user.LastName = lastName;
        user.ContactNbr = contactNbr;
        user.EmailId = emailId;
        user.UpdatedBy = "EMPLOYEE_PROFILE_EDIT";
        user.UpdatedTs = DateTime.UtcNow;

        await _db.SaveChangesAsync();
    }

    public async Task UpsertProfileAsync(EmployeeProfile profile)
    {
        var existing = await _db.EmployeeProfiles
            .FirstOrDefaultAsync(p => p.UserAccountId == profile.UserAccountId);

        if (existing == null)
        {
            profile.CreatedBy = "EMPLOYEE_PROFILE_EDIT";
            profile.CreatedTs = DateTime.UtcNow;
            _db.EmployeeProfiles.Add(profile);
        }
        else
        {
            existing.CityName = profile.CityName;
            existing.AreaAddressTxt = profile.AreaAddressTxt;
            existing.AddressTxt = profile.AddressTxt;
            existing.LatitudeNbr = profile.LatitudeNbr;
            existing.LongitudeNbr = profile.LongitudeNbr;
            existing.PreferredRadiusNbr = profile.PreferredRadiusNbr;
            existing.IsNotificationEnabledFl = profile.IsNotificationEnabledFl;
            existing.UpdatedBy = "EMPLOYEE_PROFILE_EDIT";
            existing.UpdatedTs = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
    }

    public async Task ReplaceSkillsAsync(int userAccountId, HashSet<int> skillIds)
    {
        var existing = await _db.EmployeeSkills
            .Where(s => s.UserAccountId == userAccountId)
            .ToListAsync();

        _db.EmployeeSkills.RemoveRange(existing);

        foreach (var skillId in skillIds)
        {
            _db.EmployeeSkills.Add(new EmployeeSkill
            {
                UserAccountId = userAccountId,
                SkillId = skillId,
                CreatedBy = "EMPLOYEE_PROFILE_EDIT",
                CreatedTs = DateTime.UtcNow,
            });
        }

        await _db.SaveChangesAsync();
    }

    public Task<List<Skill>> GetActiveSkillsAsync() =>
        _db.Skills.AsNoTracking()
           .Where(s => s.IsActiveFl)
           .OrderBy(s => s.CategoryName).ThenBy(s => s.SkillName)
           .ToListAsync();

    public async Task UpsertDocumentAsync(EmployeeDocument document)
    {
        var existing = await _db.EmployeeDocuments
            .FirstOrDefaultAsync(d => d.UserAccountId == document.UserAccountId && d.DocumentTypeCd == document.DocumentTypeCd);

        if (existing == null)
        {
            document.CreatedBy = "EMPLOYEE_PROFILE_EDIT";
            document.CreatedTs = DateTime.UtcNow;
            _db.EmployeeDocuments.Add(document);
        }
        else
        {
            existing.DocumentSubtypeCd = document.DocumentSubtypeCd;
            existing.IdLastFourDigitTxt = document.IdLastFourDigitTxt;
            existing.OriginalFileNameTxt = document.OriginalFileNameTxt;
            existing.StoredFileNameTxt = document.StoredFileNameTxt;
            existing.ServerFilePathTxt = document.ServerFilePathTxt;
            existing.FileSizeKbNbr = document.FileSizeKbNbr;
            existing.MimeTypeTxt = document.MimeTypeTxt;
            existing.UploadedTs = DateTime.UtcNow;

            // Re-uploading resets it back to PENDING for Admin re-review.
            existing.ReviewStatusCd = "PENDING";
            existing.ReviewedByUserAccountId = null;
            existing.ReviewedTs = null;
            existing.RejectionReasonTxt = null;

            existing.UpdatedBy = "EMPLOYEE_PROFILE_EDIT";
            existing.UpdatedTs = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
    }
}
