-- NPT-1078: per-request audit trail for the Neptune.ExternalAPI surface so compliance
-- can answer "who accessed what, when" and admins can identify stale tokens to prune.
-- Written by WebServiceAccessLogMiddleware after each authenticated request resolves;
-- denormalized timestamp also lives on Person.LastWebServiceAccessDate for fast
-- "who's idle" queries without scanning this table.
CREATE TABLE [dbo].[WebServiceAccessLog](
    [WebServiceAccessLogID] [int] IDENTITY(1,1) NOT NULL CONSTRAINT [PK_WebServiceAccessLog_WebServiceAccessLogID] PRIMARY KEY,
    [PersonID] [int] NOT NULL CONSTRAINT [FK_WebServiceAccessLog_Person_PersonID] FOREIGN KEY REFERENCES [dbo].[Person]([PersonID]),
    [Endpoint] [varchar](200) NOT NULL,
    [HttpMethod] [varchar](10) NOT NULL,
    [RequestedDate] [datetime] NOT NULL CONSTRAINT [DF_WebServiceAccessLog_RequestedDate] DEFAULT GETUTCDATE(),
    [ResponseStatusCode] [int] NOT NULL
)
GO

-- Supports the most common "what did this person do" query (admin pruning, debugging).
CREATE INDEX [IX_WebServiceAccessLog_PersonID_RequestedDate] ON [dbo].[WebServiceAccessLog] ([PersonID], [RequestedDate] DESC)
GO

-- Supports the "recent activity across all consumers" query (capacity, anomaly detection).
CREATE INDEX [IX_WebServiceAccessLog_RequestedDate] ON [dbo].[WebServiceAccessLog] ([RequestedDate] DESC)
GO
