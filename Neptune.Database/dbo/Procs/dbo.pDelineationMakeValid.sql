create procedure dbo.pDelineationMakeValid
as
begin

    update dbo.Delineation set DelineationGeometry = DelineationGeometry.MakeValid() where DelineationGeometry.STIsValid() = 0
    update dbo.Delineation set DelineationGeometry4326 = DelineationGeometry4326.MakeValid() where DelineationGeometry4326.STIsValid() = 0
end

GO
