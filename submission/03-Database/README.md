# Database Deliverable Notes

## What Is In This Folder
- `TableScripts/` contains hand-authored SQL table scripts that mirror the EF Core schemas used by the project.
- These scripts are meant to satisfy the assignment request for per-table SQL definitions.

## DACPAC Export
- The assignment also requires `.dacpac` files that include schema and data.
- Run `tools/Prepare-Submission.ps1` after building the solution. The script creates/refreshes both LocalDB databases and extracts:
  - `DotNetFinalAssignment_GameServer.dacpac`
  - `DotNetFinalAssignment_ClientReplay.dacpac`

## Current Database Names
- Server database: `DotNetFinalAssignment_GameServer`
- Client replay database: `DotNetFinalAssignment_ClientReplay`
