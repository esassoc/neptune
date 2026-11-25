# Overlay comparison tools

For checking that a change to the overlay code does not change its output: run the
overlay before and after the change on the same database, and compare the two results.

The overlays run in-process on NetTopologySuite (`Services/Overlay/`). The rules are in
`TrashGeneratingUnitOverlay` and `LoadGeneratingUnitOverlay`, the geometry operations in
`OverlayEngine`. QGIS and the Python scripts these tools were first written against
were retired in July 2026 (NPT-1105).

## Getting an output file from a run

Set `OverlayDebugExportFolder` in the Overlay API's configuration (local only; leave it
unset in QA and prod). Each TGU or LGU run then also writes its result as GeoJSON to
that folder, as `TGU<ticks>_nts.geojson` or `LGU<ticks>_nts.geojson`.

Trigger a run through the app, or directly against the Overlay API:
`POST /overlay/generate-tgus` with `{}` for a total TGU refresh, and
`POST /overlay/generate-lgus` with `{}` for a total LGU refresh (omit
`loadGeneratingUnitRefreshAreaID`).

A run replaces the contents of the TGU or LGU tables. On a database you want to keep as
it is, back those tables up first and restore them afterwards.

Output files are county-sized. Keep them in a local folder outside the repo.

## compare_geojson.py

Compares two TGU or LGU output GeoJSON files, ignoring feature order and IDs. Pure
Python 3 standard library (planar shoelace areas, exact for EPSG:2771), so it needs
nothing installed.

```bash
python3 compare_geojson.py before/TGU_output.geojson after/TGU_output.geojson --type tgu
python3 compare_geojson.py before/LGU_output.geojson after/LGU_output.geojson --type lgu
```

It reports feature and group counts, total area, groups present in only one output, and
per-group area differences above `--tolerance` (default 0.5 m²). A group is one
combination of IDs (for example land use block, delineation, WQMP and OVTA Area).
Exit code 0 means the files match within tolerance, 1 means they differ, 2 means an
input error.

## What to expect

The engine is deterministic: two runs of the same code on the same inputs give the
same output. A change that is meant to leave the output alone should therefore give no
missing or extra groups and no area differences.

For a change that is meant to alter the output, also compare acreage grouped by trash
capture status per jurisdiction. Grouping by IDs cannot see a status flip: two runs can
agree on total and per-group area while giving contested ground to delineations with
different capture statuses, which moves acreage between Partial and Untreated in the
trash results. That happened once (July 2026) and is why effectiveness ties are now
broken on capture status before ID.

## Tests

The rules have tests that need no database: `TrashGeneratingUnitOverlayTests`,
`LoadGeneratingUnitOverlayTests` and `OverlayEngineTests` in `Neptune.Tests`. The input
queries are covered by `ModelingInputViewTests` and `OvtaOverlayInputViewTests`, which
run against the local database.
