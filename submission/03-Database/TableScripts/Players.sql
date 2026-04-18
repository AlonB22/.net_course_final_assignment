CREATE TABLE [dbo].[Players]
(
    [PlayerId] UNIQUEIDENTIFIER NOT NULL,
    [ExternalId] INT NOT NULL,
    [FirstName] NVARCHAR(60) NOT NULL,
    [PhoneNumber] NVARCHAR(10) NOT NULL,
    [CountryId] INT NOT NULL,
    CONSTRAINT [PK_Players] PRIMARY KEY ([PlayerId]),
    CONSTRAINT [FK_Players_Countries_CountryId] FOREIGN KEY ([CountryId]) REFERENCES [dbo].[Countries]([CountryId]),
    CONSTRAINT [UQ_Players_ExternalId] UNIQUE ([ExternalId]),
    CONSTRAINT [CK_Players_ExternalId_Range] CHECK ([ExternalId] BETWEEN 1 AND 1000),
    CONSTRAINT [CK_Players_FirstName_Length] CHECK (LEN([FirstName]) >= 2),
    CONSTRAINT [CK_Players_PhoneNumber_Format] CHECK (LEN([PhoneNumber]) = 10 AND [PhoneNumber] NOT LIKE '%[^0-9]%')
);

CREATE INDEX [IX_Players_CountryId] ON [dbo].[Players]([CountryId]);
