-- NPT-1115: Repair any Delineation rows whose DelineationGeometry / DelineationGeometry4326 fail
-- SQL Server's STIsValid() check. Invalid geometry causes GeoServer to throw SQL error 24144 when
-- rendering WMS tiles, silently blanking the Provisional Delineations layer (same symptom as the
-- one-time NPT-1030 backfill in script 024). Recurred because the delineation write paths had no
-- validity guard; this ships alongside the view guard (vGeoServerDelineation), save-time repair,
-- and the nightly full-model-run repair so it cannot recur. Idempotent — a no-op where all rows
-- are already valid (e.g. QA).
EXEC dbo.pDelineationMakeValid;
