using Alpaca.Markets;
using Microsoft.Extensions.Configuration;
using System.Security;
using TradingAPI.Models.DTOs;
using TradingAPI.Models.Enums;

namespace TradingAPI.Services
{
    public class AlpacaService
    {
        private readonly IAlpacaDataClient _dataClient;

        public AlpacaService(IConfiguration config)
        {
            var apiKey = config["Alpaca:ApiKey"];
            var apiSecret = config["Alpaca:ApiSecret"];

            var secretKey = new SecretKey(apiKey!, apiSecret!);

            _dataClient = Alpaca.Markets.Environments.Paper.GetAlpacaDataClient(secretKey);
        }

        public async Task<IQuote?> GetLatestQuoteAsync(string symbol)
        {
            try
            {
                var request = new LatestMarketDataRequest(symbol);

                return await _dataClient.GetLatestQuoteAsync(request);
            }
            catch (RestClientErrorException)
            {
                return null;
            }
        }

        public async Task<IEnumerable<HistoricalBarDto>?> GetStockHistoryAsync(string symbol,
            ChartTimeframe timeframe = ChartTimeframe.OneDay)
        {
            try
            {
                symbol = symbol.ToUpper();

                var into = DateTime.Now.AddMinutes(-20);
                DateTime from = into;

                switch (timeframe)
                {
                    case ChartTimeframe.OneMonth:
                        from = into.AddYears(-10);
                        break;
                    case ChartTimeframe.OneDay:
                        from = into.AddYears(-3);
                        break;
                    case ChartTimeframe.OneHour:
                        from = into.AddMonths(-6);
                        break;
                    case ChartTimeframe.ThirtyMinutes:
                        from = into.AddMonths(-3);
                        break;
                    case ChartTimeframe.FifteenMinutes:
                        from = into.AddMonths(-1);
                        break;
                    default:
                        from = into.AddYears(-1);
                        break;
                }

                BarTimeFrame alpacaTimeFrame = BarTimeFrame.Day;
                switch(timeframe)
                {
                    case ChartTimeframe.OneMonth: alpacaTimeFrame = BarTimeFrame.Month; break;
                    case ChartTimeframe.OneDay: alpacaTimeFrame= BarTimeFrame.Day; break;
                    case ChartTimeframe.OneHour: alpacaTimeFrame = BarTimeFrame.Hour; break;
                    case ChartTimeframe.ThirtyMinutes: alpacaTimeFrame = new BarTimeFrame(30, BarTimeFrameUnit.Minute); break;
                    case ChartTimeframe.FifteenMinutes: alpacaTimeFrame = new BarTimeFrame(15, BarTimeFrameUnit.Minute); break;
                    default: alpacaTimeFrame = new BarTimeFrame(1, BarTimeFrameUnit.Hour); break;
                }

                var request = new HistoricalBarsRequest(symbol, from, into, alpacaTimeFrame).WithPageSize(10000);
                var bars = await _dataClient.GetHistoricalBarsAsync(request);

                if(bars.Items.ContainsKey(symbol))
                {
                    return bars.Items[symbol].Select(b => new HistoricalBarDto
                    {
                        Time = b.TimeUtc.ToString("o"),
                        Open = b.Open,
                        High = b.High,
                        Low = b.Low,
                        Close = b.Close,
                        Volume = b.Volume
                    });

                }
                return new List<HistoricalBarDto>();
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
                return null;
            }
        }
        public async Task<StockQuoteDto?> GetStockSnapshotAsync(string symbol)
        {
            try
            {
                var snapshot = await _dataClient.GetSnapshotAsync(new LatestMarketDataRequest(symbol));

                if (snapshot == null) return null;

                var tradePrice = snapshot.Trade?.Price ?? 0m;
                var askPrice = snapshot.Quote?.AskPrice ?? 0m;
                var bidPrice = snapshot.Quote?.BidPrice ?? 0m;
                var prevClose = snapshot.PreviousDailyBar?.Close ?? 1m;

                var currentPrice = snapshot.Quote?.AskPrice ?? snapshot.Trade?.Price ?? 0m;

                var changePercent = ((tradePrice - prevClose) / prevClose) * 100m;

                return new StockQuoteDto
                {
                    Symbol = snapshot.Symbol.ToUpper(),
                    Price = tradePrice,
                    Bid = bidPrice,
                    Ask = askPrice,
                    ChangePercent = Math.Round(changePercent, 2),

                    Open = snapshot.CurrentDailyBar?.Open ?? 0m,
                    High = snapshot.CurrentDailyBar?.High ?? 0m,
                    Low = snapshot.CurrentDailyBar?.Low ?? 0m,
                    Volume = snapshot.CurrentDailyBar?.Volume ?? 0m,
                    PreviousClose = prevClose,
                    BidSize = snapshot.Quote?.BidSize ?? 0m,
                    AskSize = snapshot.Quote?.AskSize ?? 0m
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Eroare Alpaca la preluarea datelor: {ex.Message}");
                return null;
            }
        }
    }
}
