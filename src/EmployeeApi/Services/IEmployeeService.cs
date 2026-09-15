using EmployeeApi.Models;

namespace EmployeeApi.Services;

/// <summary>
/// Abstraction over the source of processed employee data.
/// Implementations can read from a local file, Azure Blob Storage, a database, etc.
/// </summary>
public interface IEmployeeService
{
    Task<IReadOnlyList<Employee>> GetEmployeesAsync(CancellationToken cancellationToken = default);
}
