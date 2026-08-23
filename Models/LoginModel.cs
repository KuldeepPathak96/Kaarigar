using System.ComponentModel.DataAnnotations;

namespace Kaarigar.Models;

public class LoginModel
{
    [Required(ErrorMessage = "Mobile number is required.")]
    [RegularExpression(@"^\d{10}$", ErrorMessage = "Enter a valid 10-digit mobile number.")]
    public string ContactNbr { get; set; } = string.Empty;

    /// <summary>'OTP' (default) | 'PASSWORD' — which credential the user is logging in with.</summary>
    public string LoginMethodCd { get; set; } = "OTP";

    /// <summary>Required only when LoginMethodCd == "PASSWORD".</summary>
    public string? Password { get; set; }

    /// <summary>Required only when LoginMethodCd == "OTP". 4 digits.</summary>
    [RegularExpression(@"^\d{4}$", ErrorMessage = "Enter the 4-digit OTP.")]
    public string? OtpCd { get; set; }
}
