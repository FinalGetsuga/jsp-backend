using System.Security.Claims;
using Domain.DTO.Params;
using Domain.DTO.Requests;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Service.Interface;
using Web.Extensions;
using Web.Models;

namespace Web.Controllers;

[ApiController]
[Route("api/applications")]
[Authorize]
public class StudentApplicationsController : ControllerBase
{
    private readonly IApplicationService _applicationService;

    public StudentApplicationsController(IApplicationService applicationService)
    {
        _applicationService = applicationService;
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    
    [HttpPost]
    [EnableRateLimiting("file-upload")]
    public async Task<IActionResult> CreateApplication([FromForm] CreateApplicationFormRequest form,
        CancellationToken ct)
    {
        
        var request = new CreateApplicationRequest
        {
            RenewalYear = form.RenewalYear,
            StudentCertificate = form.StudentCertificate.ToFileUploadRequest(),
            IdCard = form.IdCard.ToFileUploadRequest(),
            StudentIndex = form.StudentIndex.ToFileUploadRequest()
        };

        var id = await _applicationService.CreateApplicationAsync(CurrentUserId, request, ct);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _applicationService.GetByIdAsync(id, CurrentUserId, ct);
        return Ok(result);
    }

    [HttpGet("mine")]
    public async Task<IActionResult> GetMine([FromQuery] PaginationParams pagination, CancellationToken ct)
    {
        var result = await _applicationService.GetMyApplicationsAsync(CurrentUserId, pagination, ct);
        return Ok(result);
    }

    [HttpPut("{id}/documents/{type}")]
    [EnableRateLimiting("file-upload")]
    public async Task<IActionResult> ReplaceDocument(Guid id, DocumentType type, IFormFile file, CancellationToken ct)
    {
        await _applicationService.ReplaceDocumentAsync(id, CurrentUserId, type, file.ToFileUploadRequest(), ct);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> WithdrawApplication(Guid id, CancellationToken ct)
    {
        await _applicationService.WithdrawApplicationAsync(id, CurrentUserId, ct);
        return NoContent();
    }
}