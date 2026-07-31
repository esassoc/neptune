-- NPT-943: normalize legacy WaterQualityManagementPlan.MaintenanceContactState values to the
-- canonical 2-letter USPS codes the editor dropdown (US_STATES) now enforces. QA review found mixed
-- values ('California', 'Colorado', 'CA ' with trailing whitespace, …) entered before the dropdown /
-- via the XLSX importer, which are exposed for the first time by the WQMP GDB export's discrete State
-- field. Idempotent: already-clean 2-letter codes are untouched; re-running is a no-op. Zip formats
-- (5-digit vs ZIP+4) are intentionally left alone (flagged only).
SET NOCOUNT ON;

-- 1) Trim surrounding whitespace (e.g. 'CA ').
UPDATE dbo.WaterQualityManagementPlan
SET MaintenanceContactState = LTRIM(RTRIM(MaintenanceContactState))
WHERE MaintenanceContactState IS NOT NULL
  AND MaintenanceContactState <> LTRIM(RTRIM(MaintenanceContactState));

-- 2) Map any full state / territory name to its 2-letter code (default CI collation = case-insensitive).
;WITH StateMap(FullName, Abbr) AS (
    SELECT * FROM (VALUES
        ('Alabama','AL'),('Alaska','AK'),('Arizona','AZ'),('Arkansas','AR'),('California','CA'),
        ('Colorado','CO'),('Connecticut','CT'),('Delaware','DE'),('District of Columbia','DC'),
        ('Florida','FL'),('Georgia','GA'),('Hawaii','HI'),('Idaho','ID'),('Illinois','IL'),
        ('Indiana','IN'),('Iowa','IA'),('Kansas','KS'),('Kentucky','KY'),('Louisiana','LA'),
        ('Maine','ME'),('Maryland','MD'),('Massachusetts','MA'),('Michigan','MI'),('Minnesota','MN'),
        ('Mississippi','MS'),('Missouri','MO'),('Montana','MT'),('Nebraska','NE'),('Nevada','NV'),
        ('New Hampshire','NH'),('New Jersey','NJ'),('New Mexico','NM'),('New York','NY'),
        ('North Carolina','NC'),('North Dakota','ND'),('Ohio','OH'),('Oklahoma','OK'),('Oregon','OR'),
        ('Pennsylvania','PA'),('Puerto Rico','PR'),('Rhode Island','RI'),('South Carolina','SC'),
        ('South Dakota','SD'),('Tennessee','TN'),('Texas','TX'),('Utah','UT'),('Vermont','VT'),
        ('Virginia','VA'),('Washington','WA'),('West Virginia','WV'),('Wisconsin','WI'),('Wyoming','WY')
    ) AS s(FullName, Abbr)
)
UPDATE w
SET w.MaintenanceContactState = m.Abbr
FROM dbo.WaterQualityManagementPlan w
JOIN StateMap m ON w.MaintenanceContactState = m.FullName
WHERE w.MaintenanceContactState <> m.Abbr;
