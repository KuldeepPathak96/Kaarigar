using Kaarigar.Models;

namespace Kaarigar.Services;

/// <summary>Result of "Contact Employee" — carries the revealed phone number on success.</summary>
public record ContactRevealResult(bool Success, string Message, string? PhoneNbr = null);

public interface IJobApplicantService
{
    /// <summary>Screen E-05: full job details + the list of applicants (with distance from job site).</summary>
    Task<JobDetailViewModel?> GetJobDetailAsync(int jobPostId, int employerUserAccountId);

    /// <summary>Screen E-06: read-only employee profile view for one applicant. Marks the application EMPLOYER_VIEWED.</summary>
    Task<ApplicantProfileViewModel?> GetApplicantProfileAsync(int jobApplicationId, int employerUserAccountId);

    /// <summary>
    /// "Contact Employee" — reveals the employee's phone number once they've
    /// expressed interest (i.e. an application already exists), and marks
    /// the application EMPLOYER_CONTACTED.
    /// </summary>
    Task<ContactRevealResult> ContactEmployeeAsync(int jobApplicationId, int employerUserAccountId);

    /// <summary>
    /// Employer generates a fresh OTP (Job Start or Satisfaction) for one Kaarigar,
    /// valid for 15 minutes. The Kaarigar enters this code in their own app.
    /// </summary>
    Task<ServiceResult> GenerateOtpAsync(int jobApplicationId, int employerUserAccountId, string otpTypeCd, string? ipAddress);

    /// <summary>Employer rates the Kaarigar. Available once StatusCd = COMPLETED (Satisfaction OTP done). One rating per application.</summary>
    Task<ServiceResult> SubmitRatingAsync(int jobApplicationId, int employerUserAccountId, byte ratingNbr, string? reviewTxt);
    Task<KaarigarRating?> GetRatingForApplicationAsync(int jobApplicationId, int employerUserAccountId);
}
