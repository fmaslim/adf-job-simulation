using EmployeeApi.Models;
using EmployeeApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _employeeService;
    private readonly ILogger<EmployeesController> _logger;

    public EmployeesController(IEmployeeService employeeService, ILogger<EmployeesController> logger)
    {
        _employeeService = employeeService;
        _logger = logger;
    }

    /// <summary>
    /// Returns the processed employee data.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Employee>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<Employee>>> GetEmployees(CancellationToken cancellationToken)
    {
        try
        {
            var employees = await _employeeService.GetEmployeesAsync(cancellationToken);
            return Ok(employees);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve employee data.");
            return StatusCode(StatusCodes.Status500InternalServerError, "Unable to retrieve employee data at this time.");
        }
    }
}
