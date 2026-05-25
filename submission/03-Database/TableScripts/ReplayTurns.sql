CREATE TABLE [dbo].[ReplayTurns]
(
    [ReplayTurnEntityId] INT IDENTITY(1,1) NOT NULL,
    [ReplayGameId] INT NOT NULL,
    [TurnIndex] INT NOT NULL,
    [ResolutionJson] NVARCHAR(MAX) NOT NULL,
    [RecordedAtUtc] DATETIME2 NOT NULL,
    CONSTRAINT [PK_ReplayTurns] PRIMARY KEY ([ReplayTurnEntityId]),
    CONSTRAINT [FK_ReplayTurns_ReplayGames_ReplayGameId]
        FOREIGN KEY ([ReplayGameId]) REFERENCES [dbo].[ReplayGames]([ReplayGameId]) ON DELETE CASCADE
);

CREATE UNIQUE INDEX [IX_ReplayTurns_ReplayGameId_TurnIndex]
    ON [dbo].[ReplayTurns]([ReplayGameId], [TurnIndex]);
