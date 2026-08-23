using Kaarigar.Models;
using Microsoft.EntityFrameworkCore;

namespace Kaarigar.Data;

public class LoginOtpDao : ILoginOtpDao
{
    private readonly AppDbContext _db;

    public LoginOtpDao(AppDbContext db)
    {
        _db = db;
    }

    public Task<UserAccount?> GetUserByContactNbrAsync(string contactNbr) =>
        _db.UserAccounts.FirstOrDefaultAsync(u => u.ContactNbr == contactNbr && u.IsActiveFl);

    public async Task InvalidatePreviousOtpsAsync(string contactNbr)
    {
        var previous = await _db.MobileVerificationOtps
            .Where(o => o.ContactNbr == contactNbr &&
                        o.PurposeCd == "LOGIN" &&
                        !o.IsUsedFl)
            .ToListAsync();

        foreach (var otp in previous)
        {
            otp.IsUsedFl = true;
            otp.UpdatedBy = "LOGIN_OTP_REQUEST";
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
                       o.PurposeCd == "LOGIN" &&
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
        otp.UpdatedBy = "LOGIN_OTP";
        otp.UpdatedTs = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public Task<int> CountRecentOtpRequestsAsync(string contactNbr, TimeSpan window)
    {
        var since = DateTime.UtcNow.Subtract(window);
        return _db.MobileVerificationOtps
                  .CountAsync(o => o.ContactNbr == contactNbr &&
                                   o.PurposeCd == "LOGIN" &&
                                   o.GeneratedTs >= since);
    }
}
