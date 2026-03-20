namespace TradingAPI.Models.DTOs
{
    public class PortfolioResponseDto
    {
        public decimal Balance { get; set; }
        public List<PortfolioItemDto> Items { get; set; } = new List<PortfolioItemDto>();
    }
}
