using Kaarigar.Models;
using Microsoft.EntityFrameworkCore;

namespace Kaarigar.Data;

public class EmployerProfileDao : IEmployerProfileDao
{
    private readonly AppDbContext _db;

    public EmployerProfileDao(AppDbContext db)
    {
        _db = db;
    }

    public async Task<(UserAccount User, EmployerProfile? Profile)?> GetProfileAsync(int userAccountId)
    {
        var user = await _db.UserAccounts.FirstOrDefaultAsync(u => u.UserAccountId == userAccountId);
        if (user == null) return null;

        var profile = await _db.EmployerProfiles
            .FirstOrDefaultAsync(p => p.UserAccountId == userAccountId);

        return (user, profile);
    }

    public Task<bool> IsContactNbrTakenByOthersAsync(string contactNbr, int excludeUserAccountId) =>
        _db.UserAccounts.AnyAsync(u => u.ContactNbr == contactNbr && u.UserAccountId != excludeUserAccountId);

    public Task<bool> IsEmailTakenByOthersAsync(string emailId, int excludeUserAccountId) =>
        _db.UserAccounts.AnyAsync(u => u.EmailId == emailId && u.UserAccountId != excludeUserAccountId);

    public async Task UpdateUserContactFieldsAsync(int userAccountId, string contactNbr, string? emailId)
    {
        var user = await _db.UserAccounts.FindAsync(userAccountId);
        if (user == null) return;

        user.ContactNbr = contactNbr;
        user.EmailId = emailId;
        user.UpdatedBy = "EMPLOYER_PROFILE_EDIT";
        user.UpdatedTs = DateTime.UtcNow;

        await _db.SaveChangesAsync();
    }

    public async Task<EmployerProfile> UpsertProfileAsync(EmployerProfile profile)
    {
        var existing = await _db.EmployerProfiles
            .FirstOrDefaultAsync(p => p.UserAccountId == profile.UserAccountId);

        if (existing == null)
        {
            profile.CreatedBy = "EMPLOYER_PROFILE_EDIT";
            profile.CreatedTs = DateTime.UtcNow;
            _db.EmployerProfiles.Add(profile);
        }
        else
        {
            existing.CompanyName = profile.CompanyName;
            existing.CityName = profile.CityName;
            existing.AreaAddressTxt = profile.AreaAddressTxt;
            existing.AddressTxt = profile.AddressTxt;
            existing.LatitudeNbr = profile.LatitudeNbr;      // <-- ADD THIS LINE
            existing.LongitudeNbr = profile.LongitudeNbr;    // <-- ADD THIS LINE
            existing.ContactPersonName = profile.ContactPersonName;
            existing.BusinessCategoryId = profile.BusinessCategoryId;
            existing.BusinessProofTypeCd = profile.BusinessProofTypeCd;
            existing.BusinessProofNumberTxt = profile.BusinessProofNumberTxt;

            // Only overwrite file-related fields when a new file was actually
            // uploaded (service layer leaves these null on the incoming
            // `profile` object when the user didn't pick a new file).
            if (!string.IsNullOrWhiteSpace(profile.LogoFilePathTxt))
                existing.LogoFilePathTxt = profile.LogoFilePathTxt;

            if (!string.IsNullOrWhiteSpace(profile.BusinessProofFilePathTxt))
            {
                existing.BusinessProofFilePathTxt = profile.BusinessProofFilePathTxt;
                existing.BusinessProofOriginalFileNameTxt = profile.BusinessProofOriginalFileNameTxt;
                // Re-uploading a proof resets it back to PENDING for admin re-review.
                existing.BusinessProofReviewStatusCd = "PENDING";
            }

            existing.UpdatedBy = "EMPLOYER_PROFILE_EDIT";
            existing.UpdatedTs = DateTime.UtcNow;
            profile = existing;
        }

        await _db.SaveChangesAsync();
        return profile;
    }

    public Task<List<BusinessCategory>> GetActiveBusinessCategoriesAsync() =>
        _db.BusinessCategories
           .AsNoTracking()
           .Where(c => c.IsActiveFl)
           .OrderBy(c => c.CategoryName)
           .ToListAsync();

    public Task<bool> IsBusinessCategoryValidAsync(int businessCategoryId) =>
        _db.BusinessCategories.AnyAsync(c => c.BusinessCategoryId == businessCategoryId && c.IsActiveFl);
}
