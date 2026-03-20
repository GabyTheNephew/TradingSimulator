using TradingAPI.Models.Entities;

namespace TradingAPI.Models.DTOs
{
    public class TradeResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public decimal? NewBalance { get; set; }
        public decimal? RemainingQuantity { get; set; }
        public PortfolioItem? PortfolioItem { get; set; }
    }
}