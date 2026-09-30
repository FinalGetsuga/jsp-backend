using System.Security.Claims;
using Domain.DTO.Params;
using Domain.DTO.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interface;

namespace Web.Controllers;

[ApiController]
[Route("api/admin/applications")]
[Authorize(Roles = "Admin")]
public class AdminApplicationsController : ControllerBase
{
    private readonly IApplicationService _applicationService;

    public AdminApplicationsController(IApplicationService applicationService)
    {
        _applicationService = applicationService;
    }

    private string CurrentAdminId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] ApplicationFilterParams filter,
        [FromQuery] PaginationParams pagination, CancellationToken ct)
    {
        var result = await _applicationService.GetAllApplicationsAsync(filter, pagination, ct);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await _applicationService.GetByIdForAdminAsync(id, ct);
        return Ok(result);
    }

    [HttpPost("{id}/approve")]
    public async Task<IActionResult> Approve([FromRoute] Guid id, CancellationToken ct)
    {
        await _applicationService.ApproveApplicationAsync(id, CurrentAdminId, ct);
        return Ok();
    }
    
    [HttpPost("{id}/reject")]
    public async Task<IActionResult> Reject([FromRoute] Guid id, [FromBody] RejectRequest request, CancellationToken ct)
    {
        await _applicationService.RejectApplicationAsync(id, CurrentAdminId, request.Reason, ct);
        return Ok();
    }
}