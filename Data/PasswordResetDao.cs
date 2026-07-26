using Kaarigar.Models;
using Microsoft.EntityFrameworkCore;

namespace Kaarigar.Data;

/// <summary>
/// EF Core implementation of IPasswordResetDao.
/// All reads are AsNoTracking; writes go through SaveChangesAsync explicitly
/// so the Service layer controls transaction boundaries.
/// </summary>
public class PasswordResetDao : IPasswordResetDao
{
    private readonly AppDbContext _db;

    public PasswordResetDao(AppDbContext db)
    {
        _db = db;
    }

    public Task<UserAccount?> GetUserByEmailAsync(string emailId) =>
        _db.UserAccounts
           .FirstOrDefaultAsync(u => u.EmailId != null &&
                                     u.EmailId.ToLower() == emailId.ToLower() &&
                                     u.IsActiveFl);

    public async Task InvalidatePreviousOtpsAsync(string contactNbr)
    {
        var previous = await _db.MobileVerificationOtps
            .Where(o => o.ContactNbr == contactNbr &&
                        o.PurposeCd == "FORGOT_PASSWORD" &&
                        !o.IsUsedFl)
            .ToListAsync();

        foreach (var otp in previous)
        {
            otp.IsUsedFl = true;
            otp.UpdatedBy = "FORGOT_PASSWORD_REQUEST";
            otp.UpdatedTs = DateTime.UtcNow;
        }

        if (previous.Count > 0)
            await _db.SaveChangesAsync();
    }

    public async Task<MobileVerificationOtp> CreateOtpAsync(MobileVerificationOtp otp)
    {
        _db.MobileVerificationOtps.Add(otp);
        await _db.SaveChangesAsync();
        return otp;
    }

    public Task<MobileVerificationOtp?> GetLatestValidOtpAsync(string contactNbr, string otpCd) =>
        _db.MobileVerificationOtps
           .Where(o => o.ContactNbr == contactNbr &&
                       o.PurposeCd == "FORGOT_PASSWORD" &&
                       o.OtpCd == otpCd &&
                       !o.IsUsedFl &&
                       o.ExpiresTs > DateTime.UtcNow)
           .OrderByDescending(o => o.GeneratedTs)
           .FirstOrDefaultAsync();

    public async Task MarkOtpUsedAsync(int mobileVerificationOtpId)
    {
        var otp = await _db.MobileVerificationOtps.FindAsync(mobileVerificationOtpId);
        if (otp == null) return;

        otp.IsUsedFl = true;
        otp.UpdatedBy = "PASSWORD_RESET";
        otp.UpdatedTs = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task UpdatePasswordHashAsync(int userAccountId, string newPasswordHash)
    {
        var user = await _db.UserAccounts.FindAsync(userAccountId);
        if (user == null) return;

        user.PasswordHashTxt = newPasswordHash;
        user.UpdatedBy = "PASSWORD_RESET";
        user.UpdatedTs = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public Task<int> CountRecentOtpRequestsAsync(string contactNbr, TimeSpan window)
    {
        var since = DateTime.UtcNow.Subtract(window);
        return _db.MobileVerificationOtps
                  .CountAsync(o => o.ContactNbr == contactNbr &&
                                   o.PurposeCd == "FORGOT_PASSWORD" &&
                                   o.GeneratedTs >= since);
    }
}
