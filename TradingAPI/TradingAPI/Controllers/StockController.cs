using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using TradingAPI.Models.DTOs;
using TradingAPI.Models.Enums;
using TradingAPI.Services;

namespace TradingAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class StockController : ControllerBase
    {
        private readonly AlpacaService _alpacaService;
        
        public StockController(AlpacaService alpacaService) 
        {
            _alpacaService = alpacaService;
        }

        [HttpGet("{symbol}")]
        public async Task<IActionResult> GetLatestPrice(string symbol)
        {
            try
            {
                var quote = await _alpacaService.GetLatestQuoteAsync(symbol.ToUpper());

                if (quote == null)
                {
                    return NotFound(new { Message = $"Data not found for action {symbol}" });
                }

                return Ok(new
                {
                    Symbol = symbol.ToUpper(),
                    Price = quote.AskPrice
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new {Message = "Connection error", Details = ex.Message});
            }
        }

        [HttpGet("{symbol}/history")]
        public async Task<IActionResult> GetStockHistory(string symbol, 
            [FromQuery] ChartTimeframe timeframe = ChartTimeframe.OneDay) { 
            var history = await _alpacaService.GetStockHistoryAsync(symbol, timeframe);

            if(history == null)
            {
                return NotFound("Symbol not found or it doesn't have history");
            }

            return Ok(history);
        }
        [HttpGet("search/{symbol}")]
        public async Task<ActionResult<StockQuoteDto>> SearchStock(string symbol)
        {
            var stockData = await _alpacaService.GetStockSnapshotAsync(symbol);

            if (stockData == null) return NotFound();
            return Ok(stockData);
        }
    }
}
