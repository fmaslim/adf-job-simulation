namespace EmployeeApi.Services;

/// <summary>
/// Configuration for locating the processed employee data.
/// Bound from the "EmployeeData" section of appsettings.json.
/// </summary>
public class EmployeeDataOptions
{
    public const string SectionName = "EmployeeData";

    /// <summary>
    /// Which implementation of <see cref="IEmployeeService"/> to use: "Json" or "Blob".
    /// </summary>
    public string Source { get; set; } = "Json";

    /// <summary>
    /// Path to the local JSON file, relative to the application's content root.
    /// Only used when <see cref="Source"/> is "Json".
    /// </summary>
    public string FilePath { get; set; } = "Data/employees.json";

    /// <summary>
    /// Azure Blob Storage settings. Only used when <see cref="Source"/> is "Blob".
    /// </summary>
    public BlobEmployeeDataOptions Blob { get; set; } = new();
}

public class BlobEmployeeDataOptions
{
    /// <summary>
    /// Blob service endpoint, e.g. "https://&lt;account&gt;.blob.core.windows.net".
    /// Used with Azure AD auth (DefaultAzureCredential) when set. Preferred for local
    /// development against a real storage account and for production (no secrets).
    /// </summary>
    public string? ServiceUri { get; set; }

    /// <summary>
    /// Optional connection string, typically used only for the Azurite local emulator
    /// or short-lived dev/test storage accounts. Must come from configuration/user-secrets/
    /// environment variables - never committed to source control. When set, this takes
    /// precedence over <see cref="ServiceUri"/> + DefaultAzureCredential.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Name of the container that holds the ADF pipeline's processed output.
    /// </summary>
    public string ContainerName { get; set; } = "employee-data";

    /// <summary>
    /// Name/path of the blob within the container, e.g. "processed/employees.json".
    /// </summary>
    public string BlobName { get; set; } = "employees.json";
}
