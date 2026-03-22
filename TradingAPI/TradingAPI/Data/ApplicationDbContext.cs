using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TradingAPI.Models.Entities;

namespace TradingAPI.Data
{
    public class ApplicationDbContext: IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }
        public DbSet<PortfolioItem> PortfolioItems { get; set; }
        public DbSet<Order> Orders { get; set; }
    }
}
