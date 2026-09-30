using Domain.DTO.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Service.Interface;

namespace Web.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }
    
    [HttpPost("register/student")]
    [EnableRateLimiting("register")]
    public async Task<IActionResult> RegisterStudent([FromBody] RegisterStudentRequest request, CancellationToken ct)
    {
        var result = await _authService.RegisterStudentAsync(request, ct);

        if (!result.IsSuccess)
        {
            return BadRequest(new { errors = result.Errors });
        }
        
        return Ok(result);
    }

    [HttpPost("register/admin")]
    public async Task<IActionResult> RegisterAdmin([FromBody] RegisterAdminRequest request, CancellationToken ct)
    {
        var result = await _authService.RegisterAdminAsync(request, ct);

        if (!result.IsSuccess)
        {
            return BadRequest(new { errors = result.Errors });
        }
        
        return Ok(result);
    }

    [HttpPost("login")]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await _authService.LoginAsync(request, ct);

        if (!result.IsSuccess)
        {
            return Unauthorized(new { errors = result.Errors });
        }
        
        return Ok(new { token = result.Token, expiresAt = result.ExpiresAt });
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("add-to-role")]
    public async Task<IActionResult> AddUserToAdmin([FromBody]RoleRequest request, CancellationToken ct)
    {
        await _authService.AddUserToAdminAsync(request.Email, ct);
        return NoContent();
    }

    [HttpPost("confirm-email")]
    public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequest request, CancellationToken ct)
    {
        var success = await _authService.ConfirmEmailAsync(request.UserId, request.Token, ct);
        if (!success)
            return BadRequest(new { message = "Invalid or expired confirmation link." });
        
        return Ok(new { message = "Email confirmed. You can now log in" });
    }
}