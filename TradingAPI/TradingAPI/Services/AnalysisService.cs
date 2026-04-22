using Microsoft.EntityFrameworkCore;
using TradingAPI.Data;
using TradingAPI.Models.DTOs;

namespace TradingAPI.Services
{
    public class AnalysisService
    {
        private readonly HttpClient _httpClient;
        private readonly TradingService _tradingService;
        private readonly ApplicationDbContext _context;

        public AnalysisService(HttpClient httpClient, TradingService tradingService, ApplicationDbContext context)
        {
            _httpClient = httpClient;
            _tradingService = tradingService;
            _context = context;
        }

        public async Task<AnalysisResponseDto?> GenerateAnalysisAsync(string userId, string ticker)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return null;

            // 1. Obținem contextul portofoliului folosind TradingService existent
            var portfolio = await _tradingService.GetPortfolioAsync(userId);
            string portfolioContext = BuildPortfolioContextString(portfolio);

            // 2. Determinăm data de start pentru știri (default 7 zile dacă e prima rulare)
            var lastUpdate = user.LastAnalysisDate ?? DateTime.UtcNow.AddDays(-7);

            var request = new AnalysisRequestDto
            {
                Ticker = ticker,
                PortfolioContext = portfolioContext,
                LastUpdate = lastUpdate.ToString("O") // Format ISO 8601
            };

            // 3. Apelăm Microserviciul Python (presupunem că rulează pe portul 8000)
            var response = await _httpClient.PostAsJsonAsync("http://localhost:8000/analyze", request);

            if (!response.IsSuccessStatusCode)
                throw new Exception("AI Microservice is unreachable or returned an error.");

            var result = await response.Content.ReadFromJsonAsync<AnalysisResponseDto>();

            // 4. Actualizăm data ultimei analize în baza de date
            user.LastAnalysisDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return result;
        }

        private string BuildPortfolioContextString(PortfolioResponseDto? portfolio)
        {
            if (portfolio == null) return "Account Balance: $0 | No positions.";

            var positions = portfolio.Items.Select(i => $"{i.Quantity} shares of {i.Symbol}").ToList();
            string posStr = positions.Any() ? string.Join(", ", positions) : "No open positions.";

            return $"Account Balance: ${portfolio.Balance:N2} | Current Positions: {posStr}";
        }
    }
}
