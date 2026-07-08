# Neptune.QGISAPI — Performance & Stability Analysis

*Analysis date: July 2026. Companion to the phased improvement roadmap below; Phase 1 shipped as the PR that added this document.*

## What the service does

Neptune.QGISAPI is an ASP.NET microservice on the `qgis/qgis:release-3_28` Docker image that computes the platform's polygon-overlay products:

| Endpoint | Product | Script | Trigger |
|---|---|---|---|
| `POST /qgis/generate-lgus` | LoadGeneratingUnits (full or delta) | `ModelingOverlayAnalysis.py` | Every delineation/BMP/WQMP boundary edit (delta); chained from RSB refresh (full, currently disabled) |
| `POST /qgis/generate-tgus` | TrashGeneratingUnits | `ComputeTrashGeneratingUnits.py` | Nightly at 22:30 |
| `POST /qgis/generate-plgus` | ProjectLoadGeneratingUnits | `ModelingOverlayAnalysis.py` | On-demand project network solve |

Callers (Hangfire jobs via `QGISAPIService`) send only IDs. The service reads geometry from the `vPyQgis*` database views, serializes each input layer to GeoJSON in the temp folder, shells out to `python3 <script>`, reads the output GeoJSON back, and bulk-inserts the results (`QgisRunnerController.cs`).

## Measured scale (prod-restored NeptuneDB, July 2026)

| Layer | Features | Total vertices | Avg/Max vertices |
|---|---:|---:|---:|
| **TGU inputs** | | | |
| LandUseBlock | 127,469 | 3,229,982 | 25 / 5,088 |
| Delineation | 5,401 | 274,122 | 50 / 2,401 |
| WQMP | 1,581 | 87,469 | 55 / 1,393 |
| OVTA | 724 | 195,777 | 270 / 1,594 |
| **LGU inputs** | | | |
| RegionalSubbasin | 8,235 | 522,420 | 63 / 1,534 |
| Delineation | 2,774 | 38,167 | 13 / 259 |
| WQMP | 437 | 20,483 | 46 / 1,393 |
| ModelBasin | 95 | 67,161 | 706 / 3,925 |
| **Outputs** | | | |
| TrashGeneratingUnit (×2 with 4326 twin) | 149,609 | 3,439,675 | 22 / 3,827 |
| LoadGeneratingUnit | 15,151 | 657,382 | 43 / 1,533 |
| ProjectLoadGeneratingUnit | 15,401 | 609,359 | 39 / 934 |

Physical: LandUseBlock 240 MB; TGU + TGU4326 ~150 MB combined; everything else ≤75 MB.

**Interpretation.** The nightly TGU refresh — the heaviest run — overlays ~135K features / ~3.8M vertices and writes ~300K rows. By modern GEOS/JTS overlay standards this is *medium*-scale: raw coordinates are ~60 MB of doubles, and the full working set fits in-process in well under 2 GB. The cost driver is not geometry volume but the machinery around it:

- **~9 county-wide GeoJSON serialize/parse round trips per TGU run** — each layer write/read is hundreds of MB of JSON text (GeoJSON inflates binary coordinates ~10×), on both the C# and Python sides.
- **Per-pair edit-buffer commits** in the Flatten algorithm (each `commitChanges()` rebuilds layer state) plus O(n) attribute-expression scans per pair.
- **A ~300K-row EF `AddRange` + single `SaveChangesAsync`** on insert.

## Pain-point inventory (as found, pre-Phase-1)

1. **Timeouts effectively infinite.** Process timeout 250,000,000 ms (~69 h, `QgisService.cs`), HttpClient 1 day (`Neptune.API/Startup.cs`), Kestrel keep-alive 1 day. A wedged QGIS run blocked the single Hangfire worker (`WorkerCount = 1`) indefinitely, stalling every background job.
2. **Temp files leaked on failure.** `DeleteTempFiles` ran only on the success path; failed runs left county-sized GeoJSON in `/tmp`. (Historically left deliberately for debugging — but unbounded.)
3. **No Kubernetes safety rails.** No resource requests/limits, no liveness/readiness probes, single replica, and a PodDisruptionBudget whose selector matched no pods (a repo-wide chart bug — every chart's `pdb.yaml` selects `app: <fullname>` while pods carry `app.kubernetes.io/*` labels).
4. **TGU Flatten algorithm mechanics.** `handleOverlaps` calls `startEditing()`/`commitChanges()` inside the per-pair loop and re-queries features by full-table attribute expression per pair (`ComputeTrashGeneratingUnits.py`). O(pairs × features) plus per-pair commit overhead.
5. **Full in-memory materialization** on the C# side: whole FeatureCollection → single string → file; full read-back lists before insert.
6. **No retries, no concurrency guards** on the LGU/TGU jobs (global Hangfire `Attempts = 0`; no `[DisableConcurrentExecution]`).
7. **QGIS 3.28 base image is EOL** (tag last pushed Feb 2024). The `release-3_XX` tag scheme itself is dead; current LTR line is 3.40 (`3.40.15` final point release).
8. **Latent bugs found during analysis:**
   - *Flatten stale-variable bug*: when an ID scan yields nothing (feature already deleted as an earlier loser), `left_feat`/`right_feat` silently retain the previous iteration's values and the geometry difference is applied to the wrong features. The `isValid()` guard does not catch this.
   - *`RSB_IDs` global bug* in `ModelingOverlayAnalysis.py`: `parseArguments` never declares `global RSB_IDs`, so the `--rsb_ids` branch never executes (and its clip clips the RSB layer against itself). PLGU works only because the controller pre-filters RSB features server-side.
   - *NPT-981 race*: runs long enough that users delete delineations mid-run; mitigated by an orphan-drop before insert, but the root cause is run duration.

## Phase 1 (shipped with this document)

- **Config-driven timeouts**, ordered process (240 min, `QgisProcessTimeoutMinutes`) < HTTP client (270 min, `QGISAPIHttpClientTimeoutMinutes`) < Kestrel keep-alive (300 min), so the process timeout fires first and the controller returns a real 500 with captured stderr → existing support-email flow. Retunable via the qgisapi configmap without a deploy.
- **Failure-run quarantine instead of leak-or-delete.** On failure, the run's temp GeoJSON moves to `/tmp/qgis-failed-runs/<prefix>/` (last 3 failed runs kept, older pruned) so artifacts remain debuggable without unbounded disk growth; success deletes as before. Files are container-local — `kubectl cp` them out before a pod restart.
- **Process-tree kill + reliable output capture** in `ProcessUtility`: `Kill(entireProcessTree: true)` (python3 spawns children) and the documented parameterless-`WaitForExit()` drain pattern replacing a 2011-era `Thread.Sleep(250ms)` hack. Also benefits GDALAPI, which shares this utility.
- **Helm safety rails**: resources (requests 2Gi/250m, limit 8Gi memory, no CPU limit — starting guesses, tune after measuring a nightly run), deliberately lenient liveness/readiness probes (~2 min of consecutive failures before restart, since the pod is CPU-saturated for hours mid-run), broken no-op PDB deleted.
- **`[DisableConcurrentExecution]`** on the LGU/TGU refresh jobs (defense in depth vs `WorkerCount = 1`).
- **Dead code removed**: legacy `QgisRunner.cs`, unused `AzureStorage`/`IAzureStorage` + `Azure.Storage.Blobs` dependency, unused `fetchLayerFromDatabase`, stale launchSettings profile name.

Deliberately *not* done: Hangfire retries (the delete-before-insert stored procs make a retry restart a multi-hour run), and touching the other HttpClients (Nereid/OCGIS/GDAL keep their 1-day timeouts — latent, not active, harm).

## Roadmap

| Phase | What | Risk |
|---|---|---|
| ~~1. Stability quick wins~~ | Shipped (above) | Low |
| 2. Parity harness + NTS benchmark spike (the decision gate) | Capture golden TGU/LGU inputs+outputs; run the captured TGU inputs through an NTS OverlayNG prototype; measure wall time + memory | Low (throwaway prototype; harness is reusable under every future) |
| 3. Engine decision → port **or** stay-on-QGIS track | **Port**: NTS in the dedicated overlay service (see below), validated against the harness. **Stay**: QGIS 3.40.15 upgrade *then* Flatten rework | Medium-high (output parity either way) |
| 4. C# pipeline slimming | Streaming GeoJSON write (`GeoJsonSerializer.SerializeToFileAsync` already exists), async process invocation, chunked inserts — largely dissolves into the port if 3 goes that way | Low-medium |

**The QGIS 3.40 upgrade is deliberately *not* scheduled ahead of the engine decision.** Its real cost is not the one-line Dockerfile bump but the `qgis:` → `native:` algorithm-id migration, a parity run, and a ~week QA soak — all sunk cost if the port follows. It happens only on the stay-on-QGIS track (or if the port is deferred long enough that maintaining QGIS becomes ongoing reality). EOL exposure is tolerable in the meantime: qgisapi is ClusterIP-only with no ingress.

Cheap insurance to do regardless: **mirror `qgis/qgis:release-3_28` into the project ACR** (`containersesaqa.azurecr.io`) — the `release-3_XX` tag scheme is abandoned upstream, and a pruned Docker Hub tag would break CI builds with no recovery path.

Pre-work for tuning: pull actual job durations from Hangfire dashboard history and pod memory high-water from Datadog/`kubectl top` during a nightly run; raise the 240-min default if full TGU exceeds ~3 h.

## Strategic direction — should QGIS stay?

**Assessment: QGIS is the wrong tool for this pipeline long-term.** Everything the scripts use (buffer(0), snap, fix-geometries, union, clip, dissolve, multipart-explode, spatial-predicate joins) is a thin wrapper over GEOS. QGIS contributes a 4–6 GB EOL-treadmill desktop-GIS image with Qt, processing-framework layer round-trips, the edit-buffer machinery behind the Flatten hot spot, and the python3 subprocess boundary that forces the temp-file/process-kill/output-capture apparatus.

The measured scale confirms feasibility of alternatives: ~4M vertices / ~135K polygons is comfortable for any GEOS-lineage engine with spatial indexing.

**Constraint (deliberate, keep it):** the overlay work stays in its own container. Hangfire runs inside Neptune.API, so moving the computation "in-process" would put multi-GB memory spikes and hours of CPU saturation on the pod serving interactive SPA traffic. The isolation boundary is right where it is — what should change is what's inside it.

- **NetTopologySuite in a dedicated overlay service (recommended end state).** Same service shape as today — own container/pod/resource limits, same three endpoints, same Hangfire orchestration, same direct-DB reads — with the internals swapped: EF query → STRtree/OverlayNG in C# → insert, all inside the service. Same JTS/GEOS algorithm lineage (OverlayNG, STRtree, GeometryFixer, GeometrySnapper); the data already lives as NTS geometries via EF, and both sides of today's python boundary already use NTS. What disappears: the python3 subprocess, all temp-GeoJSON round trips (the single biggest measured cost), the process-kill/output-capture apparatus, and the 4–6 GB Qt desktop image (base drops to plain `aspnet:10`, ~220 MB) with its EOL treadmill. Effectively `Neptune.QGISAPI` → `Neptune.OverlayAPI` with an unchanged contract. **Caveat at this scale:** managed NTS typically runs 2–5× slower than native GEOS on large overlays — likely minutes instead of tens of seconds for the 127K-block union, still far better than hours, but **gate the decision on a benchmark spike** running the captured TGU inputs through an NTS OverlayNG prototype.
- **Shapely 2 / GeoPandas (fallback, same isolation).** If the NTS spike disappoints: keep the container boundary, replace the QGIS image with python:slim + GeoPandas (~500 MB). Native GEOS speed retained; `geopandas.overlay(how="union")` replaces `native:union`. Least rewrite, but keeps the subprocess-or-python-web-service layer and a second language in the stack.
- **Rejected:** in-process in Neptune.API (violates the isolation constraint above); PostGIS (right tool, wrong stack — adds a second DB engine to an Azure/SQL Server shop); SQL Server native spatial (poor overlay performance at this scale, no layer-union primitive).

**Decision (2026-07-08): ditch QGIS — port to NTS in the dedicated overlay service.** The Phase 2 benchmark spike remains the go/no-go check on NTS overlay performance before the full port is built; Shapely 2/GeoPandas is the fallback engine *only if* the spike shows managed-NTS overlay times that are operationally unacceptable. The QGIS 3.40 upgrade is off the roadmap; QGIS retires on the pinned 3.28 image (mirror it to ACR as insurance until retirement completes).

## Key files

- `Neptune.QGISAPI/Controllers/QgisRunnerController.cs` — the whole C# pipeline
- `Neptune.QGISAPI/ComputeTrashGeneratingUnits.py` — TGU overlay + Flatten algorithm
- `Neptune.QGISAPI/ModelingOverlayAnalysis.py` — LGU/PLGU overlay
- `Neptune.QGISAPI/pyqgis_utils.py` — shared PyQGIS helpers (`bufferSnapFix` cleaning pipeline)
- `Neptune.Common/Services/QGISAPIService.cs` — the only HTTP client
- `Neptune.Common/ProcessUtility.cs` — subprocess spawn/kill/capture
- `Neptune.Jobs/Hangfire/{LoadGeneratingUnitRefreshJob,TrashGeneratingUnitRefreshJob,ProjectNetworkSolveJob}.cs` — callers
- `charts/neptune/charts/neptune-qgisapi/` — deployment, probes, resources
