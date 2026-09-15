# ADF Job Simulation

A simulated enterprise workflow: an Azure Data Factory pipeline produces employee
data, a .NET API exposes it, and an Angular UI displays it.

```
ADF pipeline (CSV) → Azure Blob Storage → EmployeeApi (.NET 8) → employee-ui (Angular)
                                              ^
                                 local JSON file (default, no Azure required)
```

## Projects

| Path | What it is |
|---|---|
| `src/EmployeeApi` | ASP.NET Core 8 Web API. Exposes `GET /api/employees`. |
| `src/employee-ui` | Angular 18 app. Calls the API and renders an employee table. |
| `tests/EmployeeApi.Tests` | xUnit tests for the API (service + controller + endpoint level). |

## Employee contract

Both the API and the UI agree on this shape (it matches the real ADF pipeline's
CSV output: `id,name,department,salary`):

```json
{ "id": 1, "name": "Alice", "department": "Engineering", "salary": 95000 }
```

## Data source

`EmployeeApi` reads employee data through an `IEmployeeService` abstraction so the
source can change without touching the controller or the Angular app. Two
implementations exist today, selected by `EmployeeData:Source` in
`src/EmployeeApi/appsettings.json`:

- **`Json`** (default) — reads `src/EmployeeApi/Data/employees.json`. No Azure
  access required; this is what local development and CI use.
- **`Blob`** — reads the real CSV written by the ADF pipeline from Azure Blob
  Storage, authenticating with `DefaultAzureCredential`. Configuration lives
  under `EmployeeData:Blob` (`ServiceUri`, `ContainerName`, `BlobName`).

## Running locally

**API** (from `src/EmployeeApi`):

```bash
dotnet run --urls http://localhost:5037
```

Swagger UI: `http://localhost:5037/swagger`

To run against the real Azure Blob output instead of the local JSON file (requires
`az login` and `Storage Blob Data Reader` on the storage account):

```bash
EmployeeData__Source=Blob dotnet run --urls http://localhost:5037
```

**Angular UI** (from `src/employee-ui`):

```bash
npm ci
npm start
```

Open `http://localhost:4200`. The dev environment (`src/environments/environment.development.ts`)
already points at `http://localhost:5037/api`; the production environment
(`environment.ts`) uses a relative `/api` base path so no URL is hard-coded.

The API only enables CORS for `http://localhost:4200` in the `Development`
environment (`src/EmployeeApi/appsettings.json` → `Cors:AllowedOrigins`).

## Testing

```bash
# Backend
dotnet test AdfJobSimulation.slnx

# Frontend
cd src/employee-ui
npm test -- --watch=false --browsers=ChromeHeadless
```

Both run in CI on every push/PR to `main` (`.github/workflows/ci.yml`), along with
`dotnet build` (Release) and `npm run build`.

## Remaining manual step for a real deployment

Everything above works end-to-end locally. The one step that can't be automated
from a repo checkout is Azure-side: whichever identity runs `EmployeeApi` in a
real environment (a developer via `az login`, or the app's managed identity once
deployed) needs the **Storage Blob Data Reader** role on the storage account
backing the ADF pipeline's output container, and `EmployeeData:Source` needs to
be set to `Blob` for that environment. Locally this was already verified against
the real pipeline output; it isn't the default so that local dev and CI stay
Azure-independent.
