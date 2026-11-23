using System.Security.Claims;
using api_finances.src.Interfaces;
using api_finances.src.Models.Base;
using api_finances.src.Requests.Ai;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api_finances.src.Controllers
{
    [Route("api/ai")]
    [ApiController]
    public class AiController(IAiService service) : ControllerBase
    {
        private string UserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";

        [Authorize]
        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] AiChatRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest("A mensagem é obrigatória.");
            }

            ResponseApi<string> response = await service.ChatAsync(UserId, request);
            return StatusCode(response.StatusCode, response.Result);
        }
    }
}
