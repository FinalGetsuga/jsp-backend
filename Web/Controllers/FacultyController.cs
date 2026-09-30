using Microsoft.AspNetCore.Mvc;
using Service.Interface;

namespace Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FacultyController : ControllerBase
{
    private readonly IFacultyService _service;

    public FacultyController(IFacultyService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllFacultiesAsync()
    {
        var result = await _service.GetAllAsync();
        return Ok(result);
    }
}