create procedure dbo.pLoadGeneratingUnitUpdateFromStaging
with execute as owner
as
begin

    -- NPT-1156: the total Load Generating Unit refresh. The caller
    -- (LoadGeneratingUnits.SwapFromStagingAsync) runs this inside a transaction and verifies the
    -- result before committing, so a failure anywhere leaves the previous Load Generating Units and
    -- their HRU characteristics in place.
    set xact_abort on

    truncate table dbo.HRUCharacteristic
    delete from dbo.LoadGeneratingUnit

    -- Decided on NPT-1156: LoadGeneratingUnitID is not stable across total refreshes. Every unit
    -- takes its staging ID, so the IDs start again at 1, and the HRU characteristics are requested
    -- again for all of them.
    set identity_insert dbo.LoadGeneratingUnit on

    insert into dbo.LoadGeneratingUnit(LoadGeneratingUnitID, LoadGeneratingUnitGeometry, ModelBasinID, RegionalSubbasinID, DelineationID, WaterQualityManagementPlanID, LoadGeneratingUnitGeometry4326)
    select s.LoadGeneratingUnitStagingID, s.LoadGeneratingUnitGeometry, s.ModelBasinID, s.RegionalSubbasinID, s.DelineationID, s.WaterQualityManagementPlanID, s.LoadGeneratingUnitGeometry4326
    from dbo.LoadGeneratingUnitStaging s
    order by s.LoadGeneratingUnitStagingID

    set identity_insert dbo.LoadGeneratingUnit off

    exec dbo.pLoadGeneratingUnitMakeValid

end

GO
