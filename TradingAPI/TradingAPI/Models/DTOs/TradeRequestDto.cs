using System.ComponentModel.DataAnnotations;

namespace TradingAPI.Models.DTOs
{
    public class TradeRequestDto
    {
        [Required]
        public string Symbol { get; set; } = string.Empty;

        [Required]
        [Range(0.0001, double.MaxValue, ErrorMessage = "Cantitatea trebuie să fie mai mare ca 0.")]
        public decimal Quantity { get; set; }
    }
}