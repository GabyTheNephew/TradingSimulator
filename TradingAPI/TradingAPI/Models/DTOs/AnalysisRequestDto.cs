using System.Text.Json.Serialization;

namespace TradingAPI.Models.DTOs
{
    public class AnalysisRequestDto
    {
        [JsonPropertyName("ticker")]
        public string Ticker { get; set; } = string.Empty;

        [JsonPropertyName("portfolio_context")]
        public string PortfolioContext { get; set; } = string.Empty;

        [JsonPropertyName("last_update")]
        public string LastUpdate { get; set; } = string.Empty;
    }
}
