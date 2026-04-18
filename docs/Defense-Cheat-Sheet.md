# DotNet Final Assignment Defense Cheat Sheet

## Architecture
- `src/Game.Server`: ASP.NET Core Razor Pages site plus Web API controllers.
- `src/Game.Client`: WinForms client for the 8x4 board, timers, drawing, gameplay, and replay.
- `src/Game.Contracts`: shared DTOs and enums only. No rule engine lives in the client.
- SQL Server LocalDB is the central persistence store for the server.
- The client also uses EF Core for a local replay database.

## Main Flow
1. Register players on the Razor site.
2. `RegistrationService` validates the input, creates the players and session, and initializes the board state JSON.
3. The WinForms client loads the session by session id through `GET /api/sessions/{id}`.
4. The client submits a human move through `POST /api/sessions/{id}/human-move`.
5. The server validates the move, applies it, computes one server response move, updates session state, and returns the new board.

## Why The Server Owns The Rules
- The assignment explicitly says the client should not generate legal moves.
- The client only sends intent and renders the returned board.
- All rule logic is in `GameRulesEngine` under `src/Game.Server/GameLogic`.

## Data Model
- `Country`: lookup table for the registration combo.
- `Player`: internal `Guid` key plus immutable assignment ID in `ExternalId`.
- `GameSession`: metadata for a played game plus `StateJson` for persisted board state.
- `GameParticipant`: join table between players and sessions with turn order.

## Delete Strategy
- `GameSession -> GameParticipant` is cascade.
- `Player -> GameParticipant` is restrict.
- `DeletePlayerAsync` first deletes all sessions that player participated in, then deletes the player.
- This avoids SQL Server multiple-cascade-path problems while still matching the assignment requirement that deleting a player deletes their games.

## Board Logic
- The visible board is `8 x 4` playable cells.
- Pieces start on rows `0-2` for the server and `5-7` for the human side.
- Normal moves: one diagonal step forward.
- Backward move: one diagonal step backward once per piece, non-capturing only.
- Capture: forward only, single jump over one opponent piece.
- Win: reach the opposite back row or leave the opponent with no legal move.

## Likely Defense Questions
- Why is `ExternalId` not the database primary key?
- How do you map diagonal movement on an `8 x 4` playable-cell board?
- Why did you store gameplay state in JSON on `GameSession`?
- Why is the client not allowed to validate moves?
- How do you enforce case-sensitive ordering in the query pages?
- How do you guarantee the query-preparation methods are LINQ-only?

## Likely Live Code Changes
- Add one more validation rule to registration.
- Change the server move ordering strategy.
- Add another query column.
- Change a board color or timer default in WinForms.
- Add one more field to a Razor table or admin form.

## Quick Code Pointers
- Registration and player creation: `src/Game.Server/Services/RegistrationService.cs`
- EF schema and constraints: `src/Game.Server/Data/GameDbContext.cs`
- Server move/rule engine: `src/Game.Server/GameLogic/GameRulesEngine.cs`
- Gameplay API: `src/Game.Server/Controllers/SessionsController.cs`
- Client gameplay shell: `src/Game.Client/Form1.cs`
- Query dashboard: `src/Game.Server/Pages/Queries/Index.cshtml.cs`
- Admin page: `src/Game.Server/Pages/Admin/Index.cshtml.cs`
