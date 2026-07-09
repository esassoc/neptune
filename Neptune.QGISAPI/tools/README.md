# QGISAPI parity tools (NPT-1105 Part 2)

Tooling for validating the QGIS → NetTopologySuite engine replacement: capture a
"golden" baseline from the current QGIS pipeline, then compare any candidate run
(reworked script, NTS port) against it on identical inputs.

## compare_geojson.py

Order/fid-insensitive comparison of two TGU or LGU output GeoJSON files. Pure
Python 3 standard library (planar shoelace areas — exact for EPSG:2771), so it
runs on a dev host, in the QGIS container, or against NTS output without
installing anything.

```bash
python3 compare_geojson.py golden/TGU_output.geojson candidate/TGU_output.geojson --type tgu
python3 compare_geojson.py golden/LGU_output.geojson candidate/LGU_output.geojson --type lgu
```

Reports feature/group counts, total area, groups present in only one output, and
per-group area deltas above `--tolerance` (default 0.5 m²). Exit code 0 = match
within tolerance, 1 = differences, 2 = input error — usable in scripts/CI.

## Capturing a golden run

Golden sets are county-sized — keep them in a **local folder outside the repo**
(convention: `C:\neptune-golden-runs\<yyyy-MM-dd>\`), never committed.

> Temp-file cleanup is **disabled** during the Part 2 capture/port window
> (`DeleteTempFiles` is a logging no-op — see `QgisRunnerController`), so every
> run keeps its inputs, intermediates, and output in the container's `/tmp` with
> no timing pressure. Nothing releases to prod before the NTS cutover; the whole
> temp-file pipeline is deleted at retirement.

1. Restore a recent prod backup locally (`Build/DatabaseRestore.ps1`) and run the
   stack via Visual Studio Container Tools as usual.
2. Trigger a run — either through the app (nightly TGU job / delineation edit for
   LGU) or directly against the QGIS API swagger at `http://localhost:8232/swagger`:
   `POST /qgis/generate-tgus` with `{}` (full TGU) and `POST /qgis/generate-lgus`
   with `{}` (full LGU; omit `loadGeneratingUnitRefreshAreaID` for a total refresh).
3. After the run completes, copy everything for its prefix out of the container:

   ```powershell
   $dest = "C:\neptune-golden-runs\$(Get-Date -Format yyyy-MM-dd)"
   New-Item -ItemType Directory -Force $dest
   $qgis = docker ps --format '{{.Names}}' | Select-String qgisapi
   # everything for one run shares one prefix, e.g. TGU638…
   docker exec $qgis sh -c "ls /tmp/TGU* /tmp/LGU* 2>/dev/null"
   docker cp "${qgis}:/tmp/." $dest   # or cp individual <prefix>* files
   ```

   The `<prefix>{delineation,ovta,wqmp,landUseBlock}Layer.geojson` files are the
   **inputs**; `<prefix>.geojson` is the golden **output**; the rest are Python
   intermediates (useful for debugging, not needed for parity).
4. Record the baseline numbers alongside the files in a `baseline.md`: wall time
   (Hangfire dashboard or container log timestamps around `Starting Process:`)
   and peak container memory (`docker stats` during the run).
5. `/tmp` accumulates a few hundred MB per run while cleanup is disabled — clear
   it occasionally (`docker exec $qgis sh -c "rm /tmp/TGU* /tmp/LGU* /tmp/PLGU*"`)
   or just restart the container.

The captured **inputs** are what the NTS spike and any script rework must be fed;
the captured **output** is the golden file `compare_geojson.py` compares against.

## Expected (whitelisted) differences for the NTS port

- Diffs traceable to the Flatten stale-pair fix (pairs referencing already-deleted
  features are now skipped and logged instead of corrupting an unrelated feature) —
  the log lines identify the pair IDs; verify each flagged group against them.
- Sub-tolerance vertex-level noise from GEOS/JTS version differences. Use area
  tolerances; do not expect exact vertex equality.

## The parity bar is calibrated by QGIS's own nondeterminism

Two back-to-back QGIS LGU total refreshes on **checksum-identical inputs** (2026-07-08
capture, see `C:\neptune-golden-runs\2026-07-08\LGU\baseline.md`) differed by: ±1
feature, 29 of ~12,700 groups with area deltas > 0.5 m² (max 53 m²), total area within
3.5e-6 relative — with **zero** missing/extra groups. The engine cannot reproduce
itself exactly (the wobble starts at the `bufferSnapFix` snap stage).

Candidate acceptance therefore is:
1. **zero missing/extra attribute groups** (hard requirement),
2. per-group area deltas of the same order as the QGIS self-noise envelope,
3. total-area delta within ~1e-5 relative.

Do not chase individual sub-envelope area deltas — they are indistinguishable from
re-running QGIS itself.
