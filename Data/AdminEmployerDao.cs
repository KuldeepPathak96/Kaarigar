using Kaarigar.Models;
using Microsoft.EntityFrameworkCore;

namespace Kaarigar.Data;

public class AdminEmployerDao : IAdminEmployerDao
{
    private readonly AppDbContext _db;

    public AdminEmployerDao(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<EmployerListItemViewModel>> SearchEmployersAsync(string? search, string? city, string? status)
    {
        var query = _db.UserAccounts
            .AsNoTracking()
            .Where(u => u.RoleCd == "EMPLOYER")
            .Join(_db.EmployerProfiles.AsNoTracking(), u => u.UserAccountId, p => p.UserAccountId,
                  (u, p) => new { User = u, Profile = p });

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(x =>
                (x.User.FirstName + " " + x.User.LastName).ToLower().Contains(term) ||
                x.User.ContactNbr.Contains(term) ||
                (x.User.EmailId != null && x.User.EmailId.ToLower().Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(city))
            query = query.Where(x => x.Profile.CityName == city);

        if (status == "ACTIVE")
            query = query.Where(x => x.User.IsActiveFl);
        else if (status == "BLOCKED")
            query = query.Where(x => !x.User.IsActiveFl);

        var rows = await query.OrderByDescending(x => x.User.CreatedTs).ToListAsync();

        var userIds = rows.Select(r => r.User.UserAccountId).ToList();
        var jobCounts = await _db.JobPosts
            .AsNoTracking()
            .Where(jp => userIds.Contains(jp.EmployerUserAccountId))
            .GroupBy(jp => jp.EmployerUserAccountId)
            .Select(g => new { EmployerUserAccountId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.EmployerUserAccountId, x => x.Count);

        return rows.Select(x => new EmployerListItemViewModel
        {
            UserAccountId = x.User.UserAccountId,
            Name = $"{x.User.FirstName} {x.User.LastName}".Trim(),
            ContactNbr = x.User.ContactNbr,
            EmailId = x.User.EmailId,
            CityName = x.Profile.CityName,
            DateRegistered = x.User.CreatedTs,
            IsActiveFl = x.User.IsActiveFl,
            IsApprovedFl = x.User.IsApprovedFl,
            JobsPostedCount = jobCounts.TryGetValue(x.User.UserAccountId, out var c) ? c : 0,
        }).ToList();
    }

    public Task<List<string>> GetDistinctCitiesAsync() =>
        _db.EmployerProfiles
           .AsNoTracking()
           .Where(p => p.CityName != null && p.CityName != "")
           .Select(p => p.CityName!)
           .Distinct()
           .OrderBy(c => c)
           .ToListAsync();

    public async Task<EmployerDetailViewModel?> GetEmployerDetailAsync(int userAccountId)
    {
        var row = await _db.UserAccounts
            .AsNoTracking()
            .Where(u => u.UserAccountId == userAccountId && u.RoleCd == "EMPLOYER")
            .Join(_db.EmployerProfiles.AsNoTracking(), u => u.UserAccountId, p => p.UserAccountId,
                  (u, p) => new { User = u, Profile = p })
            .FirstOrDefaultAsync();

        if (row == null) return null;

        var categoryName = row.Profile.BusinessCategoryId.HasValue
            ? await _db.BusinessCategories.AsNoTracking()
                .Where(c => c.BusinessCategoryId == row.Profile.BusinessCategoryId.Value)
                .Select(c => c.CategoryName)
                .FirstOrDefaultAsync()
            : null;

        var jobsPosted = await _db.JobPosts.AsNoTracking().CountAsync(jp => jp.EmployerUserAccountId == userAccountId);
        var activeJobs = await _db.JobPosts.AsNoTracking()
            .CountAsync(jp => jp.EmployerUserAccountId == userAccountId && jp.StatusCd == "ACTIVE");

        return new EmployerDetailViewModel
        {
            UserAccountId = row.User.UserAccountId,
            Name = $"{row.User.FirstName} {row.User.LastName}".Trim(),
            ContactNbr = row.User.ContactNbr,
            EmailId = row.User.EmailId,
            DateRegistered = row.User.CreatedTs,
            LastLoginTs = row.User.LastLoginTs,
            IsActiveFl = row.User.IsActiveFl,
            IsApprovedFl = row.User.IsApprovedFl,
            CompanyName = row.Profile.CompanyName,
            ContactPersonName = row.Profile.ContactPersonName,
            CityName = row.Profile.CityName,
            AreaAddressTxt = row.Profile.AreaAddressTxt,
            AddressTxt = row.Profile.AddressTxt,
            BusinessCategoryName = categoryName,
            BusinessProofTypeCd = row.Profile.BusinessProofTypeCd,
            BusinessProofNumberTxt = row.Profile.BusinessProofNumberTxt,
            BusinessProofReviewStatusCd = row.Profile.BusinessProofReviewStatusCd,
            BusinessProofOriginalFileNameTxt = row.Profile.BusinessProofOriginalFileNameTxt,
            BusinessProofFileUrl = row.Profile.BusinessProofFilePathTxt,
            LogoFileUrl = row.Profile.LogoFilePathTxt,
            JobsPostedCount = jobsPosted,
            ActiveJobsCount = activeJobs,
        };
    }

    public async Task SetActiveStatusAsync(int userAccountId, bool isActive)
    {
        var user = await _db.UserAccounts.FirstOrDefaultAsync(u => u.UserAccountId == userAccountId && u.RoleCd == "EMPLOYER");
        if (user == null) return;

        user.IsActiveFl = isActive;
        user.UpdatedBy = "ADMIN_MANAGE_EMPLOYERS";
        user.UpdatedTs = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task SetApprovedStatusAsync(int userAccountId, bool isApproved)
    {
        var user = await _db.UserAccounts.FirstOrDefaultAsync(u => u.UserAccountId == userAccountId && u.RoleCd == "EMPLOYER");
        if (user == null) return;

        user.IsApprovedFl = isApproved;
        user.UpdatedBy = "ADMIN_MANAGE_EMPLOYERS";
        user.UpdatedTs = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public Task<int> GetJobsPostedCountAsync(int userAccountId) =>
        _db.JobPosts.AsNoTracking().CountAsync(jp => jp.EmployerUserAccountId == userAccountId);

    public async Task DeleteEmployerAsync(int userAccountId)
    {
        var user = await _db.UserAccounts.FirstOrDefaultAsync(u => u.UserAccountId == userAccountId && u.RoleCd == "EMPLOYER");
        if (user == null) return;

        // EMPLOYER_PROFILE has ON DELETE CASCADE from USER_ACCOUNT, so removing
        // the UserAccount is enough — EF/SQL Server will cascade the profile row.
        _db.UserAccounts.Remove(user);
        await _db.SaveChangesAsync();
    }

    public async Task<bool> SetBusinessProofReviewStatusAsync(int userAccountId, string reviewStatusCd)
    {
        var profile = await _db.EmployerProfiles.FirstOrDefaultAsync(p => p.UserAccountId == userAccountId);
        if (profile == null) return false;

        profile.BusinessProofReviewStatusCd = reviewStatusCd;
        profile.UpdatedBy = "ADMIN_MANAGE_EMPLOYERS";
        profile.UpdatedTs = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }
}
