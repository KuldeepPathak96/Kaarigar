using System.ComponentModel.DataAnnotations;

namespace Kaarigar.Models
{
    public class AuthResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
