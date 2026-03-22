using System.Diagnostics;
using TradingAPI.Data;
using TradingAPI.Models.DTOs;
using TradingAPI.Models.Entities;
using Microsoft.EntityFrameworkCore;
using TradingAPI.Models.Enums;
using TradingAPI.Models.Entities;

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

            var order = new Order
            {
                UserId = userId,
                Symbol = request.Symbol,
                Side = OrderSide.Buy,
                Quantity = request.Quantity,
                FilledQuantity = request.Quantity, // Deocamdată primești tot ce ai cerut instant
                AverageFillPrice = currentPrice,
                Status = OrderStatus.Filled,       // Statusul e direct Filled
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Orders.Add(order);

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

            var order = new Order
            {
                UserId = userId,
                Symbol = request.Symbol,
                Side = OrderSide.Sell,
                Quantity = request.Quantity,
                FilledQuantity = request.Quantity,
                AverageFillPrice = currentPrice,
                Status = OrderStatus.Filled,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Orders.Add(order);

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
        public async Task<List<OrderResponseDto>> GetAllOrdersAsync(string userId)
        {
            // Extragem din baza de date
            var orders = await _context.Orders
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            // Mapăm în DTO (transformăm Enum-urile în String)
            return orders.Select(o => new OrderResponseDto
            {
                Id = o.Id,
                Symbol = o.Symbol,
                Side = o.Side.ToString(),
                Quantity = o.Quantity,
                FilledQuantity = o.FilledQuantity,
                AverageFillPrice = o.AverageFillPrice,
                Status = o.Status.ToString(),
                CreatedAt = o.CreatedAt
            }).ToList();
        }

        public async Task<List<OrderResponseDto>> GetOrdersBySymbolAsync(string userId, string symbol)
        {
            var orders = await _context.Orders
                .Where(o => o.UserId == userId && o.Symbol.ToUpper() == symbol.ToUpper())
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return orders.Select(o => new OrderResponseDto
            {
                Id = o.Id,
                Symbol = o.Symbol,
                Side = o.Side.ToString(),
                Quantity = o.Quantity,
                FilledQuantity = o.FilledQuantity,
                AverageFillPrice = o.AverageFillPrice,
                Status = o.Status.ToString(),
                CreatedAt = o.CreatedAt
            }).ToList();
        }
    }
}
