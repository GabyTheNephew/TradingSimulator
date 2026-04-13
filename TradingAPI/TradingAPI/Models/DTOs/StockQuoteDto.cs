namespace TradingAPI.Models.DTOs
{
    public class StockQuoteDto
    {
        public string Symbol { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal Bid { get; set; }
        public decimal Ask { get; set; }
        public decimal ChangePercent { get; set; }

        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal PreviousClose { get; set; }
        public decimal Volume { get; set; }
        public decimal BidSize { get; set; }
        public decimal AskSize { get; set; }
    }
}