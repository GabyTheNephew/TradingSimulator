using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace TradingAPI.Models.Entities
{
    public class ApplicationUser: IdentityUser
    {
        [Precision(18, 4)]
        public decimal Balance {  get; set; }

        public DateTime? LastAnalysisDate {  get; set; }
    }
}
