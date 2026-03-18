using Microsoft.AspNetCore.Mvc;
using TradingAPI.Models.DTOs;
using TradingAPI.Services;

namespace TradingAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _authService.RegisterUserAsync(model);

            if (result.Succeeded)
            {
                return Ok(new { Message = "Account created succesfully", InitialBalance = 100000m });
            }
            return BadRequest(result.Errors);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            if(!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var token = await _authService.LoginUserAsync(model);

            if(token==null)
            {
                return Unauthorized(new { message = "Email or password incorrect!" });
            }

            return Ok(new
            {
                token = token,
                message = "Logged in succesfully!"
            });
        }
    }
}
