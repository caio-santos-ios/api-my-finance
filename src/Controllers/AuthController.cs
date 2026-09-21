using api_finances.src.Interfaces;
using api_finances.src.Models.Base;
using Microsoft.AspNetCore.Mvc;
using api_finances.src.Requests;

namespace api_finances.src.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController(IAuthService service) : ControllerBase
    {   
        
        [HttpPost("register")]
        public async Task<IActionResult> Create([FromBody] CreateUserDTO user)
        {
            if (user == null) return BadRequest("Dados inválidos.");

            ResponseApi<dynamic?> response = await service.CreateAsync(user);
            return StatusCode(response.StatusCode, response.Result);
        }
             
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest body)
        {
            if (body == null) return BadRequest("Dados inválidos.");

            ResponseApi<dynamic?> response = await service.LoginAsync(body);
            return StatusCode(response.StatusCode, response.Result);
        }
       
        [HttpPost("new-code")]
        public async Task<IActionResult> NewCode([FromBody] NewCodeRequest request)
        {
            if (request == null) return BadRequest("Dados inválidos.");

            ResponseApi<dynamic?> response = await service.NewCodeAsync(request);
            return StatusCode(response.StatusCode, response.Result);
        }
        
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            if (request == null) return BadRequest("Dados inválidos.");

            ResponseApi<dynamic?> response = await service.ForgotPasswordAsync(request);
            return StatusCode(response.StatusCode, response.Result);
        }
        
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            if (request == null) return BadRequest("Dados inválidos.");

            ResponseApi<dynamic?> response = await service.ResetPasswordAsync(request);
            return StatusCode(response.StatusCode, response.Result);
        }
    }
}