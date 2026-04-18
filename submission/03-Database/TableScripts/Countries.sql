CREATE TABLE [dbo].[Countries]
(
    [CountryId] INT NOT NULL,
    [Name] NVARCHAR(100) NOT NULL,
    [IsoCode] NVARCHAR(2) NOT NULL,
    CONSTRAINT [PK_Countries] PRIMARY KEY ([CountryId]),
    CONSTRAINT [UQ_Countries_Name] UNIQUE ([Name]),
    CONSTRAINT [UQ_Countries_IsoCode] UNIQUE ([IsoCode])
);

INSERT INTO [dbo].[Countries] ([CountryId], [Name], [IsoCode]) VALUES
    (1, N'Israel', N'IL'),
    (2, N'United States', N'US'),
    (3, N'Germany', N'DE'),
    (4, N'France', N'FR'),
    (5, N'Spain', N'ES'),
    (6, N'Italy', N'IT'),
    (7, N'United Kingdom', N'GB'),
    (8, N'Canada', N'CA');
