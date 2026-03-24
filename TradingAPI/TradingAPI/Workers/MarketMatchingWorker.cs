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
            // This loop runs continuously while the app is alive
            while (!stoppingToken.IsCancellationRequested)
            {
                Console.WriteLine($"[WORKER] Checking for pending orders at {DateTime.Now:HH:mm:ss}...");
                await ProcessPendingOrdersAsync();

                // Wait 5 seconds before checking the market again (Polling)
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        private async Task ProcessPendingOrdersAsync()
        {
            // Background services are Singletons, so we must create a Scope to use the Database
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var alpacaService = scope.ServiceProvider.GetRequiredService<AlpacaService>();

            // 1. Fetch all active orders
            var activeOrders = await context.Orders
                .Include(o => o.User)
                .Where(o => o.Status == OrderStatus.Pending || o.Status == OrderStatus.PartiallyFilled)
                .ToListAsync();

            if (!activeOrders.Any()) return;

            // 2. Group by symbol so we only call Alpaca once per symbol
            var symbols = activeOrders.Select(o => o.Symbol).Distinct();

            foreach (var symbol in symbols)
            {
                var snapshot = await alpacaService.GetStockSnapshotAsync(symbol);
                if (snapshot == null) continue;

                // If Ask is 0, the market is closed or lacks liquidity. Skip matching.
                if (snapshot.Ask <= 0 || snapshot.Bid <= 0) continue;

                var symbolOrders = activeOrders.Where(o => o.Symbol == symbol).OrderBy(o => o.CreatedAt);

                // 3. Match orders against the Order Book
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

            if (availableVolume <= 0) return; // No shares left to buy

            // Calculate how much we can actually fill
            decimal fillQuantity = Math.Min(neededQuantity, availableVolume);
            decimal fillPrice = snapshot.Ask;

            UpdateOrderStats(order, fillQuantity, fillPrice);

            // Deduct the volume so the next order in the queue doesn't buy the same shares
            snapshot.AskSize -= fillQuantity;

            // Add shares to User's Portfolio
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

            // Add money to User's Balance (Shares were already locked/deducted at order creation)
            order.User.Balance += (fillQuantity * fillPrice);
        }

        private void UpdateOrderStats(Order order, decimal fillQuantity, decimal fillPrice)
        {
            // Recalculate average fill price
            decimal currentTotalValue = order.FilledQuantity * order.AverageFillPrice;
            decimal newFillValue = fillQuantity * fillPrice;

            order.FilledQuantity += fillQuantity;
            order.AverageFillPrice = (currentTotalValue + newFillValue) / order.FilledQuantity;

            order.Status = order.FilledQuantity >= order.Quantity ? OrderStatus.Filled : OrderStatus.PartiallyFilled;
            order.UpdatedAt = DateTime.UtcNow;
        }
    }
}