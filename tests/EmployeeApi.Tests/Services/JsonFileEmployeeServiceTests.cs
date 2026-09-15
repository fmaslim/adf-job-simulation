using EmployeeApi.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace EmployeeApi.Tests.Services;

public class JsonFileEmployeeServiceTests : IDisposable
{
    private readonly string _tempDirectory;

    public JsonFileEmployeeServiceTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    private JsonFileEmployeeService CreateService(string relativeFilePath)
    {
        var environmentMock = new Mock<IWebHostEnvironment>();
        environmentMock.Setup(e => e.ContentRootPath).Returns(_tempDirectory);

        var options = Options.Create(new EmployeeDataOptions { FilePath = relativeFilePath });

        return new JsonFileEmployeeService(environmentMock.Object, options, NullLogger<JsonFileEmployeeService>.Instance);
    }

    [Fact]
    public async Task GetEmployeesAsync_ReturnsEmployees_WhenFileIsValid()
    {
        const string fileName = "employees.json";
        var filePath = Path.Combine(_tempDirectory, fileName);
        await File.WriteAllTextAsync(filePath, """
        [
            { "id": 1, "name": "Jane Doe", "department": "Engineering", "salary": 90000 }
        ]
        """);

        var service = CreateService(fileName);

        var employees = await service.GetEmployeesAsync();

        Assert.Single(employees);
        Assert.Equal("Jane Doe", employees[0].Name);
        Assert.Equal("Engineering", employees[0].Department);
    }

    [Fact]
    public async Task GetEmployeesAsync_Throws_WhenFileDoesNotExist()
    {
        var service = CreateService("does-not-exist.json");

        await Assert.ThrowsAsync<FileNotFoundException>(() => service.GetEmployeesAsync());
    }

    [Fact]
    public async Task GetEmployeesAsync_Throws_WhenFileIsMalformed()
    {
        const string fileName = "malformed.json";
        var filePath = Path.Combine(_tempDirectory, fileName);
        await File.WriteAllTextAsync(filePath, "{ not valid json ");

        var service = CreateService(fileName);

        await Assert.ThrowsAsync<System.Text.Json.JsonException>(() => service.GetEmployeesAsync());
    }
}
