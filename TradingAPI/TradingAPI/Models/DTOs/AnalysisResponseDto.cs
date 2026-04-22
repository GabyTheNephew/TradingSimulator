using System.Text.Json.Serialization;

namespace TradingAPI.Models.DTOs
{
    public class AnalysisResponseDto
    {
        [JsonPropertyName("ticker")]
        public string Ticker { get; set; } = string.Empty;

        [JsonPropertyName("final_report")]
        public string FinalReport { get; set; } = string.Empty;

        [JsonPropertyName("sources_used")]
        public List<string> SourcesUsed { get; set; } = new();
    }
}
