using System.Security.Claims;
using api_finances.src.Interfaces;
using api_finances.src.Models;
using api_finances.src.Models.Base;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using api_finances.src.Requests;
using api_finances.src.Shared.DTOs;

namespace api_finances.src.Controllers
{
    [Route("api/users")]
    [ApiController]
    public class UserController(IUserService service) : ControllerBase
    {
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            ResponseApi<PaginationApi<List<dynamic>>> response = await service.GetAllAsync(new(Request.Query));
            return StatusCode(response.StatusCode, response.Result);
        }
        
        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetByIdAsync(string id)
        {
            ResponseApi<dynamic?> response = await service.GetByIdAggregateAsync(id);
            return StatusCode(response.StatusCode, response.Result);
        }
        
        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetLoggedAsync()
        {
            string id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
            ResponseApi<dynamic?> response = await service.GetByIdAggregateAsync(id);
            return StatusCode(response.StatusCode, response.Result);
        }
        
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateUserRequest user)
        {
            if (user == null) return BadRequest("Dados inválidos.");

            ResponseApi<User?> response = await service.CreateAsync(user);
            return StatusCode(response.StatusCode, response.Result);
        }
        
        [Authorize]
        [HttpPut]
        public async Task<IActionResult> Update([FromBody] UpdateUserDTO user)
        {
            if (user == null) return BadRequest("Dados inválidos.");

            ResponseApi<User?> response = await service.UpdateAsync(user);
            return StatusCode(response.StatusCode, response.Result);
        }
        
        [HttpPut("confirm-account")]
        [AllowAnonymous]
        public async Task<IActionResult> UpdateConfirmAccount([FromBody] UpdateConfirmAccountDTO user)
        {
            if (user == null) return BadRequest("Dados inválidos.");

            ResponseApi<User?> response = await service.UpdateConfirmAccountAsync(user);
            return StatusCode(response.StatusCode, response.Result);
        }
        
        [Authorize]
        [HttpPut("profile-photo")]
        public async Task<IActionResult> ProfilePhoto([FromForm] ProfilePhotoDTO request)
        {
            if (request == null) return BadRequest("Dados inválidos.");
            request.Id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
            ResponseApi<string> response = await service.ProfilePhotoAsync(request);
            return StatusCode(response.StatusCode, response.Result);
        }
        
        [Authorize]
        [HttpPut("remove-profile-photo")]
        public async Task<IActionResult> RemoveProfilePhoto([FromForm] ProfilePhotoDTO request)
        {
            if (request == null) return BadRequest("Dados inválidos.");
            request.Id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
            ResponseApi<string> response = await service.RemoveProfilePhotoAsync(request);
            return StatusCode(response.StatusCode, response.Result);
        }
        
        [Authorize]
        [HttpPut("token-fcm")]
        public async Task<IActionResult> UpdateFCM([FromBody] UpdateFCMUserDTO request)
        {
            if (request == null) return BadRequest("Dados inválidos.");
            request.Id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
            ResponseApi<dynamic?> response = await service.UpdateFCMAsync(request);
            return StatusCode(response.StatusCode, response.Result);
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            ResponseApi<User> response = await service.DeleteAsync(new () { Id = id, DeletedBy = userId! });
            return StatusCode(response.StatusCode, response.Result);
        }
    }
}