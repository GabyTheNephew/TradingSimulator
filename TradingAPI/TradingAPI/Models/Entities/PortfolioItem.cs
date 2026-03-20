using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TradingAPI.Models.Entities
{
    public class PortfolioItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [JsonIgnore]
        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; } = null!;

        [Required]
        public string Symbol { get; set; } = string.Empty;

        [Precision(18, 4)]
        public decimal Quantity { get; set; }

        [Precision(18, 4)]
        public decimal AveragePrice { get; set; }
    }
}