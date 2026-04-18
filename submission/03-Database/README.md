# Database Deliverable Notes

## What Is In This Folder
- `TableScripts/` contains hand-authored SQL table scripts that mirror the EF Core schema used by the project.
- These scripts are meant to satisfy the assignment request for per-table SQL definitions.

## DACPAC Export
- The assignment also requires a `.dacpac` that includes schema and data.
- In this environment, `SqlPackage` is not installed, so use Visual Studio SQL Server Object Explorer on the LocalDB database:
  1. Run the server once so `DotNetFinalAssignment_GameServer` is created and seeded.
  2. In Visual Studio, connect to `(localdb)\\MSSQLLocalDB`.
  3. Locate `DotNetFinalAssignment_GameServer`.
  4. Choose the extract/export flow and include schema plus data.
  5. Save the exported file in this folder with the database name as the file name.

## Current Database Names
- Server database: `DotNetFinalAssignment_GameServer`
- Client replay database: `DotNetFinalAssignment_ClientReplay`
