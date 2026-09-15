using System.Text.Json;
using EmployeeApi.Models;
using Microsoft.Extensions.Options;

namespace EmployeeApi.Services;

/// <summary>
/// Reads employee data from a local JSON file that stands in for the
/// processed output of an Azure Data Factory pipeline. This is a temporary
/// data source until a Blob Storage-backed implementation replaces it.
/// </summary>
public class JsonFileEmployeeService : IEmployeeService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly string _filePath;
    private readonly ILogger<JsonFileEmployeeService> _logger;

    public JsonFileEmployeeService(
        IWebHostEnvironment environment,
        IOptions<EmployeeDataOptions> options,
        ILogger<JsonFileEmployeeService> logger)
    {
        _logger = logger;
        _filePath = Path.Combine(environment.ContentRootPath, options.Value.FilePath);
    }

    public async Task<IReadOnlyList<Employee>> GetEmployeesAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_filePath))
        {
            _logger.LogError("Employee data file not found at {FilePath}", _filePath);
            throw new FileNotFoundException("Employee data file not found.", _filePath);
        }

        try
        {
            await using var stream = File.OpenRead(_filePath);
            var employees = await JsonSerializer.DeserializeAsync<List<Employee>>(stream, SerializerOptions, cancellationToken);

            _logger.LogInformation("Loaded {Count} employees from {FilePath}", employees?.Count ?? 0, _filePath);

            return employees ?? [];
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse employee data file at {FilePath}", _filePath);
            throw;
        }
    }
}
