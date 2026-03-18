using System.ComponentModel.DataAnnotations;

namespace TradingAPI.Models.DTOs
{
    public class RegisterDto
    {
        [Required]
        public string Username { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(6, ErrorMessage = "Password must have at least 6 characters")]
        public string Password { get; set; } = string.Empty;
    }
}
