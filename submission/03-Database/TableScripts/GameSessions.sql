CREATE TABLE [dbo].[GameSessions]
(
    [GameSessionId] UNIQUEIDENTIFIER NOT NULL,
    [StartedAtUtc] DATETIME2 NOT NULL,
    [CompletedAtUtc] DATETIME2 NULL,
    [Status] INT NOT NULL,
    [Outcome] INT NOT NULL,
    [MoveTimeLimitSeconds] INT NOT NULL,
    [ParticipantCount] INT NOT NULL,
    [StateJson] NVARCHAR(MAX) NOT NULL,
    CONSTRAINT [PK_GameSessions] PRIMARY KEY ([GameSessionId]),
    CONSTRAINT [CK_GameSessions_MoveTimeLimitSeconds] CHECK ([MoveTimeLimitSeconds] IN (2, 5, 10, 15)),
    CONSTRAINT [CK_GameSessions_ParticipantCount] CHECK ([ParticipantCount] BETWEEN 1 AND 10)
);
