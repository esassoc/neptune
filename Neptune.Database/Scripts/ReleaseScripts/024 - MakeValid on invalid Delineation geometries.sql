-- NPT-1030: Backfill SQL-valid geometry on Delineation rows whose DelineationGeometry or DelineationGeometry4326
-- columns fail SQL Server's STIsValid() check. Invalid rows cause GeoServer to throw SQL error 24144 when
-- rendering WMS tiles, silently breaking the Provisional Delineations layer on the LGU map.
EXEC dbo.pDelineationMakeValid;
