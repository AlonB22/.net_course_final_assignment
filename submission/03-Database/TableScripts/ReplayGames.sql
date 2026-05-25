CREATE TABLE [dbo].[ReplayGames]
(
    [ReplayGameId] INT IDENTITY(1,1) NOT NULL,
    [SessionId] UNIQUEIDENTIFIER NOT NULL,
    [InitialDetailsJson] NVARCHAR(MAX) NOT NULL,
    [LatestSummaryJson] NVARCHAR(MAX) NOT NULL,
    [LatestBoardJson] NVARCHAR(MAX) NOT NULL,
    [LatestTurnIndex] INT NOT NULL,
    [CreatedAtUtc] DATETIME2 NOT NULL,
    [UpdatedAtUtc] DATETIME2 NOT NULL,
    [Status] INT NOT NULL,
    [Outcome] INT NOT NULL,
    CONSTRAINT [PK_ReplayGames] PRIMARY KEY ([ReplayGameId])
);

CREATE UNIQUE INDEX [IX_ReplayGames_SessionId]
    ON [dbo].[ReplayGames]([SessionId]);
