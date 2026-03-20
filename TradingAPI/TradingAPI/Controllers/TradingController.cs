using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TradingAPI.Models.DTOs;
using TradingAPI.Services;

namespace TradingAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TradingController : ControllerBase
    {
        private readonly TradingService _tradingService;

        public TradingController(TradingService tradingService)
        {
            _tradingService = tradingService;
        }

        [HttpPost("buy")]
        public async Task<IActionResult> Buy([FromBody] TradeRequestDto request)
        {
            // Extragem ID-ul userului care a făcut cererea
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            // Delegăm logica către serviciu
            var result = await _tradingService.BuyAsync(userId, request);

            if (!result.Success)
                return BadRequest(new { Message = result.Message });

            return Ok(result);
        }

        [HttpPost("sell")]
        public async Task<IActionResult> Sell([FromBody] TradeRequestDto request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var result = await _tradingService.SellAsync(userId, request);

            if (!result.Success)
                return BadRequest(new { Message = result.Message });

            return Ok(result);
        }

        [HttpGet("portfolio")]
        public async Task<ActionResult<PortfolioResponseDto>> GetPortfolio()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var portfolioData = await _tradingService.GetPortfolioAsync(userId);

            if (portfolioData == null) return NotFound("User not found.");

            return Ok(portfolioData);
        }
    }
}