namespace TradingAPI.Models.DTOs
{
    public class OrderResponseDto
    {
        public int Id { get; set; }
        public string Symbol { get; set; } = string.Empty;
        public string Side { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal FilledQuantity { get; set; }
        public decimal AverageFillPrice { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}