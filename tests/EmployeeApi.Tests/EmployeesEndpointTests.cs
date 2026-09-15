using System.Net;
using System.Net.Http.Json;
using EmployeeApi.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace EmployeeApi.Tests;

public class EmployeesEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public EmployeesEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_Employees_ReturnsOkWithSeedData()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/employees");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var employees = await response.Content.ReadFromJsonAsync<List<Employee>>();
        Assert.NotNull(employees);
        Assert.NotEmpty(employees);
    }
}
