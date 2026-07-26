using Kaarigar.Models;
using Microsoft.EntityFrameworkCore;

namespace Kaarigar.Data;

public class AdminEmployeeDao : IAdminEmployeeDao
{
    private readonly AppDbContext _db;

    public AdminEmployeeDao(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<EmployeeListItemViewModel>> SearchEmployeesAsync(string? search, string? city, string? skill, string? status)
    {
        var query = _db.UserAccounts
            .AsNoTracking()
            .Where(u => u.RoleCd == "EMPLOYEE")
            .Join(_db.EmployeeProfiles.AsNoTracking(), u => u.UserAccountId, p => p.UserAccountId,
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

        if (!string.IsNullOrWhiteSpace(skill))
        {
            var matchingUserIds = _db.EmployeeSkills
                .Where(es => _db.Skills.Any(s => s.SkillId == es.SkillId && s.SkillName == skill))
                .Select(es => es.UserAccountId);

            query = query.Where(x => matchingUserIds.Contains(x.User.UserAccountId));
        }

        var rows = await query.OrderByDescending(x => x.User.CreatedTs).ToListAsync();

        var userIds = rows.Select(r => r.User.UserAccountId).ToList();

        var skillsByEmployee = await GetSkillNamesForUsersAsync(userIds);

        var applicationCounts = await _db.JobApplications
            .AsNoTracking()
            .Where(a => userIds.Contains(a.EmployeeUserAccountId))
            .GroupBy(a => a.EmployeeUserAccountId)
            .Select(g => new { EmployeeUserAccountId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.EmployeeUserAccountId, x => x.Count);

        return rows.Select(x => new EmployeeListItemViewModel
        {
            UserAccountId = x.User.UserAccountId,
            Name = $"{x.User.FirstName} {x.User.LastName}".Trim(),
            ContactNbr = x.User.ContactNbr,
            EmailId = x.User.EmailId,
            SkillNames = skillsByEmployee.TryGetValue(x.User.UserAccountId, out var sk) ? sk : new List<string>(),
            CityName = x.Profile.CityName,
            DateRegistered = x.User.CreatedTs,
            IsActiveFl = x.User.IsActiveFl,
            IsApprovedFl = x.User.IsApprovedFl,
            ApplicationsCount = applicationCounts.TryGetValue(x.User.UserAccountId, out var c) ? c : 0,
        }).ToList();
    }

    public Task<List<string>> GetDistinctCitiesAsync() =>
        _db.EmployeeProfiles
           .AsNoTracking()
           .Where(p => p.CityName != null && p.CityName != "")
           .Select(p => p.CityName!)
           .Distinct()
           .OrderBy(c => c)
           .ToListAsync();

    public Task<List<string>> GetDistinctSkillsAsync() =>
        _db.Skills
           .AsNoTracking()
           .Where(s => s.IsActiveFl)
           .Select(s => s.SkillName)
           .OrderBy(s => s)
           .ToListAsync();

    public async Task<EmployeeDetailViewModel?> GetEmployeeDetailAsync(int userAccountId)
    {
        var row = await _db.UserAccounts
            .AsNoTracking()
            .Where(u => u.UserAccountId == userAccountId && u.RoleCd == "EMPLOYEE")
            .Join(_db.EmployeeProfiles.AsNoTracking(), u => u.UserAccountId, p => p.UserAccountId,
                  (u, p) => new { User = u, Profile = p })
            .FirstOrDefaultAsync();

        if (row == null) return null;

        var skills = (await GetSkillNamesForUsersAsync(new List<int> { userAccountId }))
            .TryGetValue(userAccountId, out var sk) ? sk : new List<string>();

        var applicationsCount = await _db.JobApplications.AsNoTracking()
            .CountAsync(a => a.EmployeeUserAccountId == userAccountId);
        var completedCount = await _db.JobApplications.AsNoTracking()
            .CountAsync(a => a.EmployeeUserAccountId == userAccountId && a.StatusCd == "COMPLETED");

        var documents = await _db.EmployeeDocuments.AsNoTracking()
            .Where(d => d.UserAccountId == userAccountId)
            .OrderByDescending(d => d.UploadedTs)
            .Select(d => new EmployeeDocumentViewModel
            {
                EmployeeDocumentId = d.EmployeeDocumentId,
                DocumentTypeCd = d.DocumentTypeCd,
                DocumentSubtypeCd = d.DocumentSubtypeCd,
                IdLastFourDigitTxt = d.IdLastFourDigitTxt,
                OriginalFileNameTxt = d.OriginalFileNameTxt,
                FileUrl = d.ServerFilePathTxt,
                MimeTypeTxt = d.MimeTypeTxt,
                UploadedTs = d.UploadedTs,
                ReviewStatusCd = d.ReviewStatusCd,
                RejectionReasonTxt = d.RejectionReasonTxt,
            })
            .ToListAsync();

        return new EmployeeDetailViewModel
        {
            UserAccountId = row.User.UserAccountId,
            Name = $"{row.User.FirstName} {row.User.LastName}".Trim(),
            ContactNbr = row.User.ContactNbr,
            EmailId = row.User.EmailId,
            DateRegistered = row.User.CreatedTs,
            LastLoginTs = row.User.LastLoginTs,
            IsActiveFl = row.User.IsActiveFl,
            IsApprovedFl = row.User.IsApprovedFl,
            SkillNames = skills,
            CityName = row.Profile.CityName,
            AreaAddressTxt = row.Profile.AreaAddressTxt,
            AddressTxt = row.Profile.AddressTxt,
            PreferredRadiusNbr = row.Profile.PreferredRadiusNbr,
            ApplicationsCount = applicationsCount,
            JobsCompletedCount = completedCount,
            Documents = documents,
        };
    }

    public async Task SetActiveStatusAsync(int userAccountId, bool isActive)
    {
        var user = await _db.UserAccounts.FirstOrDefaultAsync(u => u.UserAccountId == userAccountId && u.RoleCd == "EMPLOYEE");
        if (user == null) return;

        user.IsActiveFl = isActive;
        user.UpdatedBy = "ADMIN_MANAGE_EMPLOYEES";
        user.UpdatedTs = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task SetApprovedStatusAsync(int userAccountId, bool isApproved)
    {
        var user = await _db.UserAccounts.FirstOrDefaultAsync(u => u.UserAccountId == userAccountId && u.RoleCd == "EMPLOYEE");
        if (user == null) return;

        user.IsApprovedFl = isApproved;
        user.UpdatedBy = "ADMIN_MANAGE_EMPLOYEES";
        user.UpdatedTs = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public Task<int> GetApplicationsCountAsync(int userAccountId) =>
        _db.JobApplications.AsNoTracking().CountAsync(a => a.EmployeeUserAccountId == userAccountId);

    public async Task DeleteEmployeeAsync(int userAccountId)
    {
        var user = await _db.UserAccounts.FirstOrDefaultAsync(u => u.UserAccountId == userAccountId && u.RoleCd == "EMPLOYEE");
        if (user == null) return;

        // EMPLOYEE_PROFILE and EMPLOYEE_SKILL both have ON DELETE CASCADE from
        // USER_ACCOUNT, so removing the UserAccount is enough.
        _db.UserAccounts.Remove(user);
        await _db.SaveChangesAsync();
    }

    public async Task<bool> SetDocumentReviewStatusAsync(
        int employeeDocumentId, string reviewStatusCd, int reviewedByUserAccountId, string? rejectionReason)
    {
        var doc = await _db.EmployeeDocuments.FirstOrDefaultAsync(d => d.EmployeeDocumentId == employeeDocumentId);
        if (doc == null) return false;

        doc.ReviewStatusCd = reviewStatusCd;
        doc.ReviewedByUserAccountId = reviewedByUserAccountId;
        doc.ReviewedTs = DateTime.UtcNow;
        doc.RejectionReasonTxt = reviewStatusCd == "REJECTED" ? rejectionReason : null;
        doc.UpdatedBy = "ADMIN_MANAGE_EMPLOYEES";
        doc.UpdatedTs = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<Dictionary<int, List<string>>> GetSkillNamesForUsersAsync(List<int> userAccountIds)
    {
        if (userAccountIds.Count == 0) return new Dictionary<int, List<string>>();

        var rows = await _db.EmployeeSkills
            .AsNoTracking()
            .Where(es => userAccountIds.Contains(es.UserAccountId))
            .Join(_db.Skills.AsNoTracking(), es => es.SkillId, s => s.SkillId, (es, s) => new { es.UserAccountId, s.SkillName })
            .ToListAsync();

        return rows
            .GroupBy(r => r.UserAccountId)
            .ToDictionary(g => g.Key, g => g.Select(r => r.SkillName).ToList());
    }
}
