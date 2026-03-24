namespace TradingAPI.Models.DTOs
{
    public class PortfolioItemDto
    {
        public string Symbol { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal AveragePrice { get; set; }
        public decimal CurrentPrice { get; set; }
    }
}
