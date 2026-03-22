namespace TradingAPI.Models.DTOs
{
    public class StockQuoteDto
    {
        public string Symbol { get; set; } = string.Empty;
        public decimal Price { get; set; } // Prețul curent (Latest Trade)
        public decimal Bid { get; set; }   // Prețul de vânzare
        public decimal Ask { get; set; }   // Prețul de cumpărare
        public decimal ChangePercent { get; set; } // Procentul verde/roșu

        // Date extra pentru statistici:
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal PreviousClose { get; set; }
        public decimal Volume { get; set; }
    }
}