namespace TradingAPI.Models.DTOs
{
    public class AnalysisResponseDto
    {
        public string Ticker { get; set; } = string.Empty;
        public string FinalReport { get; set; } = string.Empty;
        public List<string> SourcesUsed { get; set; } = new();
    }
}
