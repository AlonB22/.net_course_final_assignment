# DotNet Final Assignment

Checkers-style network game for Academic .NET course. The solution contains an ASP.NET Core Razor Pages/Web API server, a WinForms client, shared contracts, database deliverables, and automated tests.

## Project Structure

- `src/Game.Server` - Razor Pages website, Web API endpoints, EF Core SQL Server database, registration, queries, admin pages, and server-side game rules.
- `src/Game.Client` - WinForms game client with board rendering, countdown timer, server move submission, annotations, animations, and local replay playback.
- `src/Game.Contracts` - Shared DTOs and enums used by both client and server.
- `tests/Game.Tests` - xUnit tests for selected game rules and registration behavior.
- `submission/03-Database` - SQL table scripts and database packaging notes.
- `submission/04-Known-Issues` - Submission known issues file.
- `tools/Prepare-Submission.ps1` - Builds the solution, prepares database exports, creates `submission-output`, and creates the final submission zip.

## Requirements

- Windows
- .NET 9 SDK
- SQL Server LocalDB
- PowerShell 7 for full packaging
- `sqlpackage` for DACPAC export

Install `sqlpackage` if needed:

```powershell
dotnet tool install -g microsoft.sqlpackage
```

## Build And Test

```powershell
dotnet build DotNetFinalAssignment.sln
dotnet test DotNetFinalAssignment.sln
```

## Run The Server

```powershell
dotnet run --project src\Game.Server\Game.Server.csproj
```

The server uses LocalDB database `DotNetFinalAssignment_GameServer` and seeds sample data on startup.

## Run The Client

Start the server first, then run:

```powershell
dotnet run --project src\Game.Client\Game.Client.csproj
```

The client expects the server base URL from `src/Game.Client/appsettings.json` and stores replay data in LocalDB database `DotNetFinalAssignment_ClientReplay`.

