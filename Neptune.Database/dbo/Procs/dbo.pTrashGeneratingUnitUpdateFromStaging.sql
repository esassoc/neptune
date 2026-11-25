create procedure dbo.pTrashGeneratingUnitUpdateFromStaging
with execute as owner
as
begin

    -- NPT-1156: the caller (TrashGeneratingUnits.SwapFromStagingAsync) runs this inside a transaction
    -- and verifies the result before committing, so a failure anywhere leaves the previous Trash
    -- Generating Units in place. TrashGeneratingUnit is deleted rather than truncated: a truncate
    -- needs the TrashGeneratingUnit4326 foreign key dropped first, and a failure before it was
    -- re-added used to leave the constraint missing.
    set xact_abort on

    truncate table dbo.TrashGeneratingUnit4326
    delete from dbo.TrashGeneratingUnit

    -- the staging ID becomes the TrashGeneratingUnitID, so the two live tables pair up by it
    set identity_insert dbo.TrashGeneratingUnit on

    insert into dbo.TrashGeneratingUnit(TrashGeneratingUnitID, StormwaterJurisdictionID, OnlandVisualTrashAssessmentAreaID, LandUseBlockID, TrashGeneratingUnitGeometry, LastUpdateDate, DelineationID, WaterQualityManagementPlanID)
    select s.TrashGeneratingUnitStagingID, s.StormwaterJurisdictionID, s.OnlandVisualTrashAssessmentAreaID, s.LandUseBlockID, s.TrashGeneratingUnitGeometry, s.LastUpdateDate, s.DelineationID, s.WaterQualityManagementPlanID
    from dbo.TrashGeneratingUnitStaging s
    order by s.TrashGeneratingUnitStagingID

    set identity_insert dbo.TrashGeneratingUnit off

    insert into dbo.TrashGeneratingUnit4326(StormwaterJurisdictionID, OnlandVisualTrashAssessmentAreaID, LandUseBlockID, TrashGeneratingUnit4326Geometry, LastUpdateDate, DelineationID, WaterQualityManagementPlanID, TrashGeneratingUnitID)
    select s.StormwaterJurisdictionID, s.OnlandVisualTrashAssessmentAreaID, s.LandUseBlockID, s.TrashGeneratingUnitGeometry4326, s.LastUpdateDate, s.DelineationID, s.WaterQualityManagementPlanID, s.TrashGeneratingUnitStagingID
    from dbo.TrashGeneratingUnitStaging s
    order by s.TrashGeneratingUnitStagingID

    exec dbo.pTrashGeneratingUnitMakeValid
    exec dbo.pTrashGeneratingUnit4326MakeValid

end

GO
