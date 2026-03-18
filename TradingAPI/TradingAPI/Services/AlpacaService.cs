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

                //switch(range)
                //{
                //    case ChartRange.OneYear: from = into.AddYears(-1); break;
                //    case ChartRange.OneMonth: from = into.AddMonths(-1); break;
                //    case ChartRange.OneDay: from = into.AddDays(-1); break;
                //    case ChartRange.OneHour: from = into.AddHours(-1); break;
                //    case ChartRange.ThirtyMinutes: from = into.AddMinutes(-30); break;
                //    case ChartRange.FifteenMinutes: from = into.AddMinutes(-15); break;
                //    default: from = into.AddMonths(-1); break;
                //}

                switch (timeframe)
                {
                    case ChartTimeframe.OneMonth:
                        from = into.AddYears(-10); // 10 ani pentru lumânări lunare
                        break;
                    case ChartTimeframe.OneDay:
                        from = into.AddYears(-3);  // 3 ani pentru lumânări zilnice
                        break;
                    case ChartTimeframe.OneHour:
                        from = into.AddMonths(-6); // 6 luni de date orare
                        break;
                    case ChartTimeframe.ThirtyMinutes:
                        from = into.AddMonths(-3); // 3 luni
                        break;
                    case ChartTimeframe.FifteenMinutes:
                        from = into.AddMonths(-1); // 1 lună (mai mult decât suficient pt 15 min)
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
    }
}
