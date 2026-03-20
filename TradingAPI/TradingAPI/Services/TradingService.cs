using System.Diagnostics;
using TradingAPI.Data;
using TradingAPI.Models.DTOs;
using TradingAPI.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace TradingAPI.Services
{
    public class TradingService
    {
        private readonly ApplicationDbContext _context;
        private readonly AlpacaService _alpacaService;

        public TradingService(ApplicationDbContext context, AlpacaService service)
        {
            _context = context;
            _alpacaService = service;
        }

        public async Task<TradeResponseDto> BuyAsync(string userId, TradeRequestDto request)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return new TradeResponseDto { Success = false,
                    Message = "Utilizatorul nu a fost găsit." };

            var quote = await _alpacaService.GetLatestQuoteAsync(request.Symbol.ToUpper());
            if (quote == null)
                return new TradeResponseDto { Success = false,
                    Message = $"Nu am putut obține prețul pentru {request.Symbol}" };

            decimal currentPrice = quote.AskPrice;
            decimal totalCost = currentPrice * request.Quantity;

            if(user.Balance < totalCost)
            {
                return new TradeResponseDto { Success = false,
                    Message = $"Insuficient funds. Total cost: {totalCost}, Balance: {user.Balance}" };
            }

            user.Balance -= totalCost;

            var portfolioItem = await _context.PortfolioItems
                .FirstOrDefaultAsync(portfolioItem => portfolioItem.UserId == userId
                && portfolioItem.Symbol == request.Symbol.ToUpper());

            if(portfolioItem == null)
            {
                portfolioItem = new PortfolioItem
                {
                    UserId = userId,
                    Symbol = request.Symbol.ToUpper(),
                    Quantity = request.Quantity,
                    AveragePrice = currentPrice
                };
                _context.PortfolioItems.Add(portfolioItem);
            }
            else
            {
                decimal totalValue = (portfolioItem.Quantity * portfolioItem.AveragePrice) + totalCost;
                portfolioItem.Quantity += request.Quantity;
                portfolioItem.AveragePrice = totalValue / portfolioItem.Quantity;
            }

            await _context.SaveChangesAsync();

            return new TradeResponseDto
            {
                Success = true,
                Message = "Transaction succesful!",
                NewBalance = user.Balance,
                PortfolioItem = portfolioItem
            };
        }
        public async Task<TradeResponseDto> SellAsync(string userId, TradeRequestDto request)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return new TradeResponseDto { Success = false, Message = "User not found." };

            var portfolioItem = await _context.PortfolioItems
                .FirstOrDefaultAsync(p => p.UserId == userId && p.Symbol == request.Symbol.ToUpper());

            if (portfolioItem == null || portfolioItem.Quantity < request.Quantity)
                return new TradeResponseDto { Success = false, Message = "Not enough quantity to sell this action." };

            var quote = await _alpacaService.GetLatestQuoteAsync(request.Symbol.ToUpper());
            if (quote == null)
                return new TradeResponseDto { Success = false, Message = $"Failed to fetch price for {request.Symbol}" };

            decimal currentPrice = quote.BidPrice;
            decimal totalRevenue = currentPrice * request.Quantity;

            user.Balance += totalRevenue;
            portfolioItem.Quantity -= request.Quantity;

            if (portfolioItem.Quantity == 0)
            {
                _context.PortfolioItems.Remove(portfolioItem);
            }

            await _context.SaveChangesAsync();

            return new TradeResponseDto
            {
                Success = true,
                Message = "Transaction succesful!",
                NewBalance = user.Balance,
                RemainingQuantity = portfolioItem.Quantity
            };
        }
        public async Task<PortfolioResponseDto?> GetPortfolioAsync(string userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return null;

            // Extragem din baza de date și mapăm direct în DTO
            var portfolioItems = await _context.PortfolioItems
                .Where(p => p.UserId == userId)
                .Select(p => new PortfolioItemDto
                {
                    Symbol = p.Symbol,
                    Quantity = p.Quantity,
                    AveragePrice = p.AveragePrice
                })
                .ToListAsync();

            return new PortfolioResponseDto
            {
                Balance = user.Balance,
                Items = portfolioItems
            };
        }
    }
}
