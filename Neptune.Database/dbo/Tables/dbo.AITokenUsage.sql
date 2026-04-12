CREATE TABLE [dbo].[AITokenUsage]
(
    [AITokenUsageID]        INT IDENTITY(1,1) NOT NULL,
    [PersonID]              INT NOT NULL,
    [Model]                 VARCHAR(100) NOT NULL,
    [InputTokens]           INT NOT NULL,
    [CachedInputTokens]     INT NOT NULL CONSTRAINT [DF_AITokenUsage_CachedInputTokens] DEFAULT 0,
    [OutputTokens]          INT NOT NULL,
    [RequestDate]           DATETIME NOT NULL,
    [RequestContext]         VARCHAR(200) NULL,

    CONSTRAINT [PK_AITokenUsage_AITokenUsageID] PRIMARY KEY ([AITokenUsageID]),
    CONSTRAINT [FK_AITokenUsage_Person_PersonID] FOREIGN KEY ([PersonID]) REFERENCES dbo.Person([PersonID]),
)
GO
