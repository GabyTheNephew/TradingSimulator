using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TradingAPI.Models.Enums;

namespace TradingAPI.Models.Entities
{
    public class Order
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; } = null!;

        [Required]
        public string Symbol { get; set; } = string.Empty;

        public OrderSide Side { get; set; } // Buy sau Sell

        [Column(TypeName = "decimal(18,4)")]
        public decimal Quantity { get; set; } // Cât ai cerut tu

        [Column(TypeName = "decimal(18,4)")]
        public decimal FilledQuantity { get; set; } // Cât ți-a dat piața efectiv

        [Column(TypeName = "decimal(18,4)")]
        public decimal AverageFillPrice { get; set; } // Prețul mediu la care ți le-a dat

        public OrderStatus Status { get; set; }

        public DateTime CreatedAt { get; set; } // Când ai apăsat butonul
        public DateTime UpdatedAt { get; set; } // Când s-a executat/modificat ultima oară
    }
}