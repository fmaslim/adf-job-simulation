using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using CsvHelper;
using EmployeeApi.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace EmployeeApi.Tests.Services;

public class BlobStorageEmployeeServiceTests
{
    private static IOptions<EmployeeDataOptions> CreateOptions(string blobName = "employees.csv") =>
        Options.Create(new EmployeeDataOptions
        {
            Blob = new BlobEmployeeDataOptions { BlobName = blobName, ContainerName = "output" }
        });

    private static BlobStorageEmployeeService CreateService(string blobContent, string blobName = "employees.csv")
    {
        var blobClientMock = new Mock<BlobClient>();
        blobClientMock
            .Setup(b => b.ExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));
        blobClientMock
            .Setup(b => b.DownloadContentAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(
                BlobsModelFactory.BlobDownloadResult(content: BinaryData.FromString(blobContent)),
                Mock.Of<Response>()));

        var containerClientMock = new Mock<BlobContainerClient>();
        containerClientMock.Setup(c => c.Name).Returns("output");
        containerClientMock
            .Setup(c => c.GetBlobClient(It.IsAny<string>()))
            .Returns(blobClientMock.Object);

        return new BlobStorageEmployeeService(
            containerClientMock.Object, CreateOptions(blobName), NullLogger<BlobStorageEmployeeService>.Instance);
    }

    [Fact]
    public async Task GetEmployeesAsync_ParsesCsv_WhenBlobExistsAndIsValid()
    {
        const string csv = "id,name,department,salary\n1,Jane Doe,Engineering,90000";

        var service = CreateService(csv);

        var employees = await service.GetEmployeesAsync();

        Assert.Single(employees);
        Assert.Equal(1, employees[0].Id);
        Assert.Equal("Jane Doe", employees[0].Name);
        Assert.Equal("Engineering", employees[0].Department);
        Assert.Equal(90000m, employees[0].Salary);
    }

    [Fact]
    public async Task GetEmployeesAsync_ParsesMultipleRows_InFileOrder()
    {
        const string csv = """
        id,name,department,salary
        1,Ava Thompson,Engineering,98000
        2,Liam Garcia,Engineering,87500
        3,Sophia Patel,Finance,91200
        """;

        var service = CreateService(csv);

        var employees = await service.GetEmployeesAsync();

        Assert.Equal(3, employees.Count);
        Assert.Equal(["Ava Thompson", "Liam Garcia", "Sophia Patel"], employees.Select(e => e.Name));
    }

    [Fact]
    public async Task GetEmployeesAsync_MatchesHeaders_CaseInsensitively()
    {
        // Mirrors the real ADF output header casing: id,name,department,salary
        const string csv = "Id,Name,Department,Salary\n1,Jane Doe,Engineering,90000";

        var service = CreateService(csv);

        var employees = await service.GetEmployeesAsync();

        Assert.Single(employees);
        Assert.Equal("Jane Doe", employees[0].Name);
    }

    [Fact]
    public async Task GetEmployeesAsync_HandlesQuotedFieldsContainingCommas()
    {
        const string csv = "id,name,department,salary\n1,Jane Doe,\"Sales, EMEA\",90000";

        var service = CreateService(csv);

        var employees = await service.GetEmployeesAsync();

        Assert.Single(employees);
        Assert.Equal("Sales, EMEA", employees[0].Department);
    }

    [Fact]
    public async Task GetEmployeesAsync_Throws_WhenBlobDoesNotExist()
    {
        var blobClientMock = new Mock<BlobClient>();
        blobClientMock
            .Setup(b => b.ExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(false, Mock.Of<Response>()));

        var containerClientMock = new Mock<BlobContainerClient>();
        containerClientMock.Setup(c => c.Name).Returns("output");
        containerClientMock
            .Setup(c => c.GetBlobClient(It.IsAny<string>()))
            .Returns(blobClientMock.Object);

        var service = new BlobStorageEmployeeService(
            containerClientMock.Object, CreateOptions("missing.csv"), NullLogger<BlobStorageEmployeeService>.Instance);

        await Assert.ThrowsAsync<FileNotFoundException>(() => service.GetEmployeesAsync());
    }

    [Fact]
    public async Task GetEmployeesAsync_Throws_WhenSalaryColumnIsNotNumeric()
    {
        const string csv = "id,name,department,salary\n1,Jane Doe,Engineering,not-a-number";

        var service = CreateService(csv);

        await Assert.ThrowsAnyAsync<CsvHelperException>(() => service.GetEmployeesAsync());
    }

    [Fact]
    public async Task GetEmployeesAsync_Throws_WhenRequiredHeaderIsMissing()
    {
        const string csv = "id,name,department\n1,Jane Doe,Engineering";

        var service = CreateService(csv);

        await Assert.ThrowsAnyAsync<CsvHelperException>(() => service.GetEmployeesAsync());
    }
}
