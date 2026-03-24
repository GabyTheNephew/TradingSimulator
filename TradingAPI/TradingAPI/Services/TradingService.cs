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
                return new TradeResponseDto { Success = false, Message = "User not found." };

            // 1. Get the best available price estimation
            var snapshot = await _alpacaService.GetStockSnapshotAsync(request.Symbol);
            if (snapshot == null)
                return new TradeResponseDto { Success = false, Message = $"Failed to fetch price for {request.Symbol}" };

            // Use Ask price if market is open, otherwise use last trade price
            decimal estimatedPrice = snapshot.Ask > 0 ? snapshot.Ask : snapshot.Price;
            decimal estimatedTotalCost = estimatedPrice * request.Quantity;

            // 2. Check funds
            if (user.Balance < estimatedTotalCost)
            {
                return new TradeResponseDto { Success = false, Message = "Insufficient funds for this order." };
            }

            // 3. LOCK FUNDS: Deduct money immediately so it can't be double-spent
            user.Balance -= estimatedTotalCost;

            // 4. Create PENDING Order (Do not add to Portfolio yet!)
            var order = new Order
            {
                UserId = userId,
                Symbol = request.Symbol.ToUpper(),
                Side = OrderSide.Buy,
                Quantity = request.Quantity,
                FilledQuantity = 0, // Nothing filled yet
                AverageFillPrice = 0, // No price yet
                Status = OrderStatus.Pending, // Placed in the queue
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            return new TradeResponseDto
            {
                Success = true,
                Message = "Buy order placed successfully and is pending execution.",
                NewBalance = user.Balance
            };
        }

        public async Task<TradeResponseDto> SellAsync(string userId, TradeRequestDto request)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return new TradeResponseDto { Success = false, Message = "User not found." };

            var portfolioItem = await _context.PortfolioItems
                .FirstOrDefaultAsync(p => p.UserId == userId && p.Symbol == request.Symbol.ToUpper());

            // 1. Check if user has enough shares
            if (portfolioItem == null || portfolioItem.Quantity < request.Quantity)
                return new TradeResponseDto { Success = false, Message = "Not enough shares to sell." };

            // 2. LOCK ASSETS: Deduct shares immediately from portfolio
            portfolioItem.Quantity -= request.Quantity;
            if (portfolioItem.Quantity == 0)
            {
                _context.PortfolioItems.Remove(portfolioItem);
            }

            // 3. Create PENDING Order (Do not add money to balance yet!)
            var order = new Order
            {
                UserId = userId,
                Symbol = request.Symbol.ToUpper(),
                Side = OrderSide.Sell,
                Quantity = request.Quantity,
                FilledQuantity = 0,
                AverageFillPrice = 0,
                Status = OrderStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            return new TradeResponseDto
            {
                Success = true,
                Message = "Sell order placed successfully and is pending execution.",
                NewBalance = user.Balance // Balance is unchanged here, but we return it
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
        public async Task<TradeResponseDto> CancelOrderAsync(string userId, int orderId)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

            if (order == null)
                return new TradeResponseDto { Success = false, Message = "Order not found." };

            if (order.Status == OrderStatus.Filled || order.Status == OrderStatus.Cancelled)
                return new TradeResponseDto { Success = false, Message = "Order cannot be cancelled in its current state." };

            decimal remainingQuantity = order.Quantity - order.FilledQuantity;

            // Refund logic
            if (order.Side == OrderSide.Sell)
            {
                // Refund shares back to portfolio
                var portfolioItem = await _context.PortfolioItems
                    .FirstOrDefaultAsync(p => p.UserId == userId && p.Symbol == order.Symbol);

                if (portfolioItem == null)
                {
                    _context.PortfolioItems.Add(new PortfolioItem
                    {
                        UserId = userId,
                        Symbol = order.Symbol,
                        Quantity = remainingQuantity,
                        AveragePrice = 0 // Restored shares
                    });
                }
                else
                {
                    portfolioItem.Quantity += remainingQuantity;
                }
            }
            else if (order.Side == OrderSide.Buy)
            {
                // Refund money. Note: For perfect accuracy, we should save LockedPrice in the Order table.
                // For now, we refund based on current market price to unblock the user.
                var snapshot = await _alpacaService.GetStockSnapshotAsync(order.Symbol);
                decimal currentPrice = snapshot?.Ask > 0 ? snapshot.Ask : (snapshot?.Price ?? 0m);

                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    user.Balance += (remainingQuantity * currentPrice);
                }
            }

            order.Status = OrderStatus.Cancelled;
            order.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new TradeResponseDto { Success = true, Message = "Order cancelled successfully." };
        }
    }
}
