# NinetyBackend

ASP.NET Core 9 backend for the Ninety platform.

## Prerequisites

- .NET SDK 9
- PostgreSQL database (Supabase is supported)
- Git

Check the SDK:

```powershell
dotnet --version
```

## Configuration

1. Open the backend project directory:

   ```powershell
   cd NinetyBackend
   ```

2. Create the local environment file:

   ```powershell
   Copy-Item .env.example .env
   ```

3. Edit `.env` and set at least:

   ```env
   NINETY_DATABASE_CONNECTION_STRING=Host=<host>;Port=5432;Database=<database>;Username=<user>;Password=<password>;SSL Mode=Require;Trust Server Certificate=true
   JWT_SECRET_KEY=<random-secret-at-least-32-characters>
   ```

   SMTP and Google OAuth values are optional unless those features are used.

Do not commit `.env` or real credentials.

## Restore and run

From the repository root:

```powershell
dotnet restore NinetyBackend.sln
dotnet run --project NinetyBackend\NinetyBackend.csproj
```

The Development profile uses:

- HTTP: http://localhost:5268
- HTTPS: https://localhost:7006
- Health check: http://localhost:5268/health
- Swagger UI: http://localhost:5268/swagger

Swagger is enabled when `ASPNETCORE_ENVIRONMENT=Development`.

To run explicitly with the HTTPS profile:

```powershell
dotnet run --project NinetyBackend\NinetyBackend.csproj --launch-profile https
```

## Database migrations

The application seeds RBAC data at startup. To apply Entity Framework migrations manually:

```powershell
dotnet ef database update --project NinetyBackend\NinetyBackend.csproj
```

If `dotnet ef` is unavailable:

```powershell
dotnet tool install --global dotnet-ef
```

Ensure `.env` contains a valid database connection string before running migrations or starting the API.

## Run tests

```powershell
dotnet test NinetyBackend.sln
```

## Project structure

- `NinetyBackend/` - API project
- `NinetyBackend/Modules/` - feature modules and endpoints
- `NinetyBackend/Infrastructure/` - database, authentication, email, and SignalR setup
- `NinetyBackend.Tests/` - xUnit tests
- `docs/` - API and integration notes

