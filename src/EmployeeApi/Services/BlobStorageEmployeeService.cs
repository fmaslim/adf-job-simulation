using System.Globalization;
using Azure.Storage.Blobs;
using CsvHelper;
using CsvHelper.Configuration;
using EmployeeApi.Models;
using Microsoft.Extensions.Options;

namespace EmployeeApi.Services;

/// <summary>
/// Reads employee data from the CSV file produced by the Azure Data Factory
/// pipeline's Copy activity and written to Azure Blob Storage. The CSV schema
/// (id, name, department, salary) is the source of truth and maps directly
/// onto <see cref="Employee"/>.
/// </summary>
public class BlobStorageEmployeeService : IEmployeeService
{
    private static readonly CsvConfiguration CsvConfiguration = new(CultureInfo.InvariantCulture)
    {
        HasHeaderRecord = true,
        // The ADF dataset headers match the Employee property names but case may vary
        // depending on how the source file was authored, so match case-insensitively.
        PrepareHeaderForMatch = args => args.Header.Trim().ToLowerInvariant()
    };

    private readonly BlobContainerClient _containerClient;
    private readonly string _blobName;
    private readonly ILogger<BlobStorageEmployeeService> _logger;

    public BlobStorageEmployeeService(
        BlobContainerClient containerClient,
        IOptions<EmployeeDataOptions> options,
        ILogger<BlobStorageEmployeeService> logger)
    {
        _containerClient = containerClient;
        _blobName = options.Value.Blob.BlobName;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Employee>> GetEmployeesAsync(CancellationToken cancellationToken = default)
    {
        var blobClient = _containerClient.GetBlobClient(_blobName);

        try
        {
            if (!await blobClient.ExistsAsync(cancellationToken))
            {
                _logger.LogError(
                    "Employee data blob '{BlobName}' not found in container '{ContainerName}'.",
                    _blobName, _containerClient.Name);
                throw new FileNotFoundException($"Blob '{_blobName}' not found in container '{_containerClient.Name}'.");
            }

            var download = await blobClient.DownloadContentAsync(cancellationToken);
            var employees = ParseCsv(download.Value.Content.ToString());

            _logger.LogInformation(
                "Loaded {Count} employees from blob '{BlobName}' in container '{ContainerName}'.",
                employees.Count, _blobName, _containerClient.Name);

            return employees;
        }
        catch (CsvHelperException ex)
        {
            _logger.LogError(ex, "Failed to parse employee data CSV blob '{BlobName}'.", _blobName);
            throw;
        }
        catch (Exception ex) when (ex is not FileNotFoundException)
        {
            _logger.LogError(ex, "Failed to read employee data blob '{BlobName}' from container '{ContainerName}'.",
                _blobName, _containerClient.Name);
            throw;
        }
    }

    private static List<Employee> ParseCsv(string csvContent)
    {
        using var stringReader = new StringReader(csvContent);
        using var csv = new CsvReader(stringReader, CsvConfiguration);

        return csv.GetRecords<Employee>().ToList();
    }
}
