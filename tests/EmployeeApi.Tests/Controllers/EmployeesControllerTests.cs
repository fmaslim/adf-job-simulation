using EmployeeApi.Controllers;
using EmployeeApi.Models;
using EmployeeApi.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace EmployeeApi.Tests.Controllers;

public class EmployeesControllerTests
{
    [Fact]
    public async Task GetEmployees_ReturnsOkWithEmployees_WhenServiceSucceeds()
    {
        var expected = new List<Employee>
        {
            new() { Id = 1, Name = "Ava Thompson", Department = "Engineering", Salary = 98000 }
        };

        var serviceMock = new Mock<IEmployeeService>();
        serviceMock.Setup(s => s.GetEmployeesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var controller = new EmployeesController(serviceMock.Object, NullLogger<EmployeesController>.Instance);

        var result = await controller.GetEmployees(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var employees = Assert.IsAssignableFrom<IEnumerable<Employee>>(okResult.Value);
        Assert.Single(employees);
    }

    [Fact]
    public async Task GetEmployees_Returns500_WhenServiceThrows()
    {
        var serviceMock = new Mock<IEmployeeService>();
        serviceMock.Setup(s => s.GetEmployeesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var controller = new EmployeesController(serviceMock.Object, NullLogger<EmployeesController>.Instance);

        var result = await controller.GetEmployees(CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
    }
}
