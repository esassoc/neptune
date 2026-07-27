create procedure dbo.pDelineationMakeValid
    -- NPT-1115: optional scoping — null repairs every invalid row (nightly full run / one-time
    -- backfill), a value repairs just that delineation (save-time repair after an upsert).
    @DelineationID int = null
as
begin

    update dbo.Delineation set DelineationGeometry = DelineationGeometry.MakeValid()
    where DelineationGeometry.STIsValid() = 0 and (@DelineationID is null or DelineationID = @DelineationID)

    update dbo.Delineation set DelineationGeometry4326 = DelineationGeometry4326.MakeValid()
    where DelineationGeometry4326.STIsValid() = 0 and (@DelineationID is null or DelineationID = @DelineationID)
end

GO
