Create Procedure dbo.pTrashGeneratingUnitStagingDelete
with execute as owner
As

truncate table dbo.TrashGeneratingUnitStaging

GO
