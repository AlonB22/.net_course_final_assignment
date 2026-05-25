CREATE TABLE [dbo].[ReplayStrokes]
(
    [ReplayStrokeEntityId] INT IDENTITY(1,1) NOT NULL,
    [ReplayGameId] INT NOT NULL,
    [TurnIndex] INT NOT NULL,
    [StrokeIndex] INT NOT NULL,
    [StrokeJson] NVARCHAR(MAX) NOT NULL,
    [RecordedAtUtc] DATETIME2 NOT NULL,
    CONSTRAINT [PK_ReplayStrokes] PRIMARY KEY ([ReplayStrokeEntityId]),
    CONSTRAINT [FK_ReplayStrokes_ReplayGames_ReplayGameId]
        FOREIGN KEY ([ReplayGameId]) REFERENCES [dbo].[ReplayGames]([ReplayGameId]) ON DELETE CASCADE
);

CREATE UNIQUE INDEX [IX_ReplayStrokes_ReplayGameId_TurnIndex_StrokeIndex]
    ON [dbo].[ReplayStrokes]([ReplayGameId], [TurnIndex], [StrokeIndex]);
