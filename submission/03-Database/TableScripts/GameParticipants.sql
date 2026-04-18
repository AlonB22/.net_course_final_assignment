CREATE TABLE [dbo].[GameParticipants]
(
    [GameSessionId] UNIQUEIDENTIFIER NOT NULL,
    [PlayerId] UNIQUEIDENTIFIER NOT NULL,
    [TurnOrder] INT NOT NULL,
    [JoinedAtUtc] DATETIME2 NOT NULL,
    CONSTRAINT [PK_GameParticipants] PRIMARY KEY ([GameSessionId], [PlayerId]),
    CONSTRAINT [FK_GameParticipants_GameSessions_GameSessionId]
        FOREIGN KEY ([GameSessionId]) REFERENCES [dbo].[GameSessions]([GameSessionId]) ON DELETE CASCADE,
    CONSTRAINT [FK_GameParticipants_Players_PlayerId]
        FOREIGN KEY ([PlayerId]) REFERENCES [dbo].[Players]([PlayerId])
);

CREATE UNIQUE INDEX [IX_GameParticipants_GameSessionId_TurnOrder]
    ON [dbo].[GameParticipants]([GameSessionId], [TurnOrder]);

CREATE INDEX [IX_GameParticipants_PlayerId]
    ON [dbo].[GameParticipants]([PlayerId]);
