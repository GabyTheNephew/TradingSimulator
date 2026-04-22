namespace TradingAPI.Models.DTOs
{
    public class AnalysisRequestDto
    {
        public string Ticker { get; set; } = string.Empty;
        public string PortfolioContext { get; set; } = string.Empty;
        public string LastUpdate { get; set; } = string.Empty;
    }
}
