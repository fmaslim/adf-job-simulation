using Azure.Identity;
using Azure.Storage.Blobs;
using EmployeeApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Add services to the container.
builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Allows the Angular dev server (a different origin/port) to call this API locally.
const string AngularDevCorsPolicy = "AngularDev";
var corsAllowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:4200"];

builder.Services.AddCors(options =>
{
    options.AddPolicy(AngularDevCorsPolicy, policy =>
    {
        policy.WithOrigins(corsAllowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.Configure<EmployeeDataOptions>(
    builder.Configuration.GetSection(EmployeeDataOptions.SectionName));

// Data source abstraction: which IEmployeeService implementation backs GET /api/employees
// is selected by the "EmployeeData:Source" setting in appsettings ("Json" or "Blob").
var employeeDataOptions = builder.Configuration
    .GetSection(EmployeeDataOptions.SectionName)
    .Get<EmployeeDataOptions>() ?? new EmployeeDataOptions();

var useBlobStorage = string.Equals(employeeDataOptions.Source, "Blob", StringComparison.OrdinalIgnoreCase);

if (useBlobStorage)
{
    var blobOptions = employeeDataOptions.Blob;

    builder.Services.AddSingleton(_ =>
    {
        BlobServiceClient serviceClient;

        if (!string.IsNullOrWhiteSpace(blobOptions.ConnectionString))
        {
            // Intended for the Azurite local emulator or short-lived dev/test accounts.
            // The connection string itself must come from configuration/user-secrets/
            // environment variables, never be hardcoded here.
            serviceClient = new BlobServiceClient(blobOptions.ConnectionString);
        }
        else if (!string.IsNullOrWhiteSpace(blobOptions.ServiceUri))
        {
            // Preferred: no secrets. Authenticates via Azure AD using whichever credential
            // source is available (Azure CLI/VS/Managed Identity/etc).
            serviceClient = new BlobServiceClient(new Uri(blobOptions.ServiceUri), new DefaultAzureCredential());
        }
        else
        {
            throw new InvalidOperationException(
                "EmployeeData:Blob is missing configuration. Set either 'EmployeeData:Blob:ServiceUri' " +
                "(with DefaultAzureCredential) or 'EmployeeData:Blob:ConnectionString'.");
        }

        return serviceClient.GetBlobContainerClient(blobOptions.ContainerName);
    });

    builder.Services.AddScoped<IEmployeeService, BlobStorageEmployeeService>();
}
else
{
    builder.Services.AddScoped<IEmployeeService, JsonFileEmployeeService>();
}

var app = builder.Build();

if (useBlobStorage)
{
    // Fail fast on bad Blob configuration at startup rather than on the first request.
    _ = app.Services.GetRequiredService<BlobContainerClient>();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    app.UseCors(AngularDevCorsPolicy);
}

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }
