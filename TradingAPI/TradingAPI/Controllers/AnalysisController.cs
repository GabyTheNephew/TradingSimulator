using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TradingAPI.Services;

namespace TradingAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AnalysisController: ControllerBase
    {
        private readonly AnalysisService _aiAnalysisService;

        public AnalysisController(AnalysisService aiAnalysisService)
        {
            _aiAnalysisService = aiAnalysisService;
        }

        // Endpoint: GET /api/analysis/{ticker}
        [HttpGet("{ticker}")]
        public async Task<IActionResult> GetStockAnalysis(string ticker)
        {
            // Extragem ID-ul utilizatorului din token-ul JWT (la fel ca în TradingController)
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized(new { Message = "User authentication failed." });
            }

            try
            {
                // Așteptăm ca C# să preia datele din SQL și să comunice cu Python
                var result = await _aiAnalysisService.GenerateAnalysisAsync(userId, ticker);

                if (result == null)
                {
                    return NotFound(new { Message = "User or portfolio data not found." });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                // Prindem erorile (ex: dacă serverul Python e oprit)
                return StatusCode(500, new { Message = "Error generating AI analysis: " + ex.Message });
            }
        }
    }
}
