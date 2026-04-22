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
            // 1. Luăm datele INIȚIALE (fără să blocăm modificările viitoare)
            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return null;

            var portfolio = await _tradingService.GetPortfolioAsync(userId);
            string portfolioContext = BuildPortfolioContextString(portfolio);
            var lastUpdate = user.LastAnalysisDate ?? DateTime.UtcNow.AddDays(-7);

            var request = new AnalysisRequestDto
            {
                Ticker = ticker,
                PortfolioContext = portfolioContext,
                LastUpdate = lastUpdate.ToString("O")
            };

            // 2. Așteptăm zeci de secunde după AI (AICI se putea întâmpla conflictul înainte)
            var response = await _httpClient.PostAsJsonAsync("http://localhost:8000/analyze", request);
            if (!response.IsSuccessStatusCode)
                throw new Exception("AI Microservice is unreachable or returned an error.");

            var result = await response.Content.ReadFromJsonAsync<AnalysisResponseDto>();

            // 3. ACTUALIZĂM DOAR DATA, FĂRĂ SĂ ATINGEM RESTUL CÂMPURILOR
            // Re-extragem user-ul fresh din DB, fix inainte sa salvam, ca sa evitam conflictele cu Worker-ul
            var userToUpdate = await _context.Users.FindAsync(userId);
            if (userToUpdate != null)
            {
                userToUpdate.LastAnalysisDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

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
