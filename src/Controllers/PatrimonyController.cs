using System.Security.Claims;
using api_finances.src.Interfaces;
using api_finances.src.Models.Base;
using api_finances.src.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api_finances.src.Controllers;

[Route("api/patrimony")]
[ApiController]
public class PatrimonyController(IPatrimonyService service) : ControllerBase
{
    private string UserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";

    [Authorize]
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        ResponseApi<dynamic> response = await service.GetSummaryAsync(UserId);
        return StatusCode(response.StatusCode, response.Result);
    }
}
