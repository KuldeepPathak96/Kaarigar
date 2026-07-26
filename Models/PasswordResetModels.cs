using System.ComponentModel.DataAnnotations;

namespace Kaarigar.Models;

/// <summary>Step 1: user enters registered email.</summary>
public class ForgotPasswordModel
{
    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string EmailId { get; set; } = string.Empty;
}

/// <summary>Step 2: user enters the OTP received by email.</summary>
public class VerifyOtpModel
{
    [Required]
    [EmailAddress]
    public string EmailId { get; set; } = string.Empty;

    [Required(ErrorMessage = "OTP is required.")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "Enter the 6-digit OTP.")]
    public string OtpCd { get; set; } = string.Empty;
}

/// <summary>Step 3: user sets a new password (after OTP has been verified).</summary>
public class ResetPasswordModel
{
    [Required]
    [EmailAddress]
    public string EmailId { get; set; } = string.Empty;

    /// <summary>
    /// Carries the verified OTP forward from Step 2 so ResetPasswordAsync can
    /// re-validate it belongs to this email and is still the used/verified one,
    /// without trusting the client that "verification" happened.
    /// </summary>
    [Required]
    [RegularExpression(@"^\d{6}$")]
    public string OtpCd { get; set; } = string.Empty;

    [Required(ErrorMessage = "New password is required.")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm your new password.")]
    [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
