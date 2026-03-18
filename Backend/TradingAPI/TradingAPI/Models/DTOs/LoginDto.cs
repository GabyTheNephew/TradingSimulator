using System.ComponentModel.DataAnnotations;

namespace TradingAPI.Models.DTOs
{
    public class LoginDto
    {
        [Required]
        [EmailAddress]
        public string Email {  get; set; }

        [Required]
        public string Password {  get; set; }
    }
}
