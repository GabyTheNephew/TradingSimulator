using Microsoft.EntityFrameworkCore;
using TradingAPI.Data;
using TradingAPI.Models.Entities;
using TradingAPI.Models.Enums;
using TradingAPI.Services;

namespace TradingAPI.Workers
{
    public class MarketMatchingWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        public MarketMatchingWorker(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("[WORKER] Market Matching Engine has started!");
            while (!stoppingToken.IsCancellationRequested)
            {
                Console.WriteLine($"[WORKER] Checking for pending orders at {DateTime.Now:HH:mm:ss}...");
                await ProcessPendingOrdersAsync();

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        private async Task ProcessPendingOrdersAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var alpacaService = scope.ServiceProvider.GetRequiredService<AlpacaService>();

            var activeOrders = await context.Orders
                .Include(o => o.User)
                .Where(o => o.Status == OrderStatus.Pending || o.Status == OrderStatus.PartiallyFilled)
                .ToListAsync();

            if (!activeOrders.Any()) return;

            var symbols = activeOrders.Select(o => o.Symbol).Distinct();

            foreach (var symbol in symbols)
            {
                var snapshot = await alpacaService.GetStockSnapshotAsync(symbol);
                if (snapshot == null) continue;

                if (snapshot.Ask <= 0 || snapshot.Bid <= 0) continue;

                var symbolOrders = activeOrders.Where(o => o.Symbol == symbol).OrderBy(o => o.CreatedAt);

                foreach (var order in symbolOrders)
                {
                    if (order.Side == OrderSide.Buy)
                    {
                        await ProcessBuyOrder(context, order, snapshot);
                    }
                    else if (order.Side == OrderSide.Sell)
                    {
                        ProcessSellOrder(order, snapshot);
                    }
                }
            }

            await context.SaveChangesAsync();
        }

        private async Task ProcessBuyOrder(ApplicationDbContext context, Order order, Models.DTOs.StockQuoteDto snapshot)
        {
            decimal availableVolume = snapshot.AskSize;
            decimal neededQuantity = order.Quantity - order.FilledQuantity;

            if (availableVolume <= 0) return;

            decimal fillQuantity = Math.Min(neededQuantity, availableVolume);
            decimal fillPrice = snapshot.Ask;

            UpdateOrderStats(order, fillQuantity, fillPrice);

            snapshot.AskSize -= fillQuantity;

            var portfolioItem = await context.PortfolioItems
                .FirstOrDefaultAsync(p => p.UserId == order.UserId && p.Symbol == order.Symbol);

            if (portfolioItem == null)
            {
                context.PortfolioItems.Add(new PortfolioItem
                {
                    UserId = order.UserId,
                    Symbol = order.Symbol,
                    Quantity = fillQuantity,
                    AveragePrice = fillPrice
                });
            }
            else
            {
                decimal totalValue = (portfolioItem.Quantity * portfolioItem.AveragePrice) + (fillQuantity * fillPrice);
                portfolioItem.Quantity += fillQuantity;
                portfolioItem.AveragePrice = totalValue / portfolioItem.Quantity;
            }
        }

        private void ProcessSellOrder(Order order, Models.DTOs.StockQuoteDto snapshot)
        {
            decimal availableVolume = snapshot.BidSize;
            decimal neededQuantity = order.Quantity - order.FilledQuantity;

            if (availableVolume <= 0) return;

            decimal fillQuantity = Math.Min(neededQuantity, availableVolume);
            decimal fillPrice = snapshot.Bid;

            UpdateOrderStats(order, fillQuantity, fillPrice);
            snapshot.BidSize -= fillQuantity;

            order.User.Balance += (fillQuantity * fillPrice);
        }

        private void UpdateOrderStats(Order order, decimal fillQuantity, decimal fillPrice)
        {
            decimal currentTotalValue = order.FilledQuantity * order.AverageFillPrice;
            decimal newFillValue = fillQuantity * fillPrice;

            order.FilledQuantity += fillQuantity;
            order.AverageFillPrice = (currentTotalValue + newFillValue) / order.FilledQuantity;

            order.Status = order.FilledQuantity >= order.Quantity ? OrderStatus.Filled : OrderStatus.PartiallyFilled;
            order.UpdatedAt = DateTime.UtcNow;
        }
    }
}