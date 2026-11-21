# Overlay units: TGUs, LGUs and PLGUs

How Neptune builds its three sets of overlay polygons, the rules each one applies, and the gaps known as of November 2026.

**Applies to:** `Neptune.OverlayAPI` (`OverlayController`, `OverlayEngine`), the `vPyQgis*` views in `Neptune.Database/dbo/Views/`, and the Hangfire jobs that call the service.
**History:** the overlays ran in QGIS until July 2026 (NPT-1105). `docs/qgisapi-performance-stability.md` records why they moved to NetTopologySuite; this document describes what the pipeline does now.

## The three units at a glance

| | Trash Generating Unit (TGU) | Load Generating Unit (LGU) | Project Load Generating Unit (PLGU) |
|---|---|---|---|
| Used by | Trash module results and maps | Modeling module (Nereid) for existing conditions | Planning module: "what would change if this project were built" |
| Table | `TrashGeneratingUnit` (+ `TrashGeneratingUnit4326`) | `LoadGeneratingUnit` | `ProjectLoadGeneratingUnit` |
| Layers overlaid | Land Use Blocks, OVTA Areas, delineations, WQMP boundaries | Regional Subbasins, WQMP boundaries, delineations | Same as LGU, plus the project's own delineations |
| Scope of a run | Whole county, every time | Whole county (total) or one refresh area (delta) | One project: its Regional Subbasins and everything upstream |
| Triggered by | Nightly recurring job (cron `30 6 * * *`, i.e. 22:30 Pacific standard time) | Every delineation / BMP / WQMP boundary edit (delta); Regional Subbasin refresh (total) | Each project network solve |
| Endpoint | `POST /overlay/generate-tgus` | `POST /overlay/generate-lgus` | `POST /overlay/generate-plgus` |

Every unit is a single polygon carrying the IDs of the input features that cover it. A unit answers "for this piece of ground, which land use block / assessment area / BMP delineation / WQMP / subbasin applies?" so that downstream SQL and Nereid can work with attributes only.

All geometry is in EPSG:2771 (NAD83(HARN) California zone 6, metres). Pieces smaller than 1 m² are dropped.

## Shared engine steps

`OverlayEngine` (`Neptune.OverlayAPI/Services/Overlay/OverlayEngine.cs`) provides the building blocks. Each has unit tests in `Neptune.Tests/OverlayEngineTests.cs`.

| Step | What it does |
|---|---|
| `Clean` | Repairs invalid geometry, forces polygons and strict 2D, drops features that clean to empty. |
| `Flatten` | Removes overlaps **within one layer** using a winner rule: exact duplicates (loser deleted), containment (losing inner deleted, or winning inner punched out of the outer), partial overlap (loser keeps what the winner does not cover). |
| `UnionLayers(A, B)` | Attribute-carrying union of two layers. Intersection pieces carry both sides' IDs (layer A wins a clash); remainders keep their own. **Assumes each layer has no internal overlaps.** If layer B overlaps itself, the result contains overlapping pieces. |
| `Clip` | Cuts a layer to a boundary. |
| `FilterIntersecting` | Keeps whole pieces that touch a filter geometry (used by the delta LGU refresh). |
| `ExplodeAndDropSlivers` | Splits multipolygons into single polygons and drops pieces under 1 m². |

The service allows one overlay run at a time (a process-wide gate); a second caller gets HTTP 409.

## Trash Generating Units

### Inputs

| Layer | View | Which rows |
|---|---|---|
| Land Use Blocks | `vPyQgisLandUseBlockTGUInput` | All land use blocks. |
| Delineations | `vPyQgisDelineationTGUInput` | **Verified** delineations of any type (centralized and distributed). No filter on the BMP's project. |
| WQMP boundaries | `vPyQgisWaterQualityManagementPlanTGUInput` | Every WQMP with a boundary, regardless of WQMP status. |
| OVTA Areas | `vPyQgisOnlandVisualTrashAssessmentAreaDated` | Areas with **two or more** assessments that have a completed date, dated by the most recent one. Whether this should drop to one is an open question (NPT-1144). |

Trash capture effectiveness (`TCEffect`) for delineations and WQMPs is computed in the views: Full = 100, None / Not Provided = 0, otherwise the entered effectiveness (0 when blank).

### Rules

1. **Flatten each of the three overlay layers** so no ground is claimed twice within a layer:
   - Delineations and WQMPs: higher `TCEffect` wins. On a tie, the better trash capture **status** wins (Full > Partial > None / Not Provided). On a further tie, the higher ID wins.
   - OVTA Areas: the later assessment date wins; on a tie, the higher ID.
2. **Union in this order:** (OVTA Areas ∪ delineations) ∪ WQMPs, then Land Use Blocks ∪ that result. Land Use Blocks go first so the block's jurisdiction is the one kept.
3. **Keep only pieces that have a Land Use Block.** Delineation, WQMP or OVTA ground with no block under it is not a TGU.
4. Each surviving polygon becomes one TGU with `LandUseBlockID`, `StormwaterJurisdictionID` (from the block) and optional `DelineationID`, `WaterQualityManagementPlanID`, `OnlandVisualTrashAssessmentAreaID`. A reprojected copy goes to `TrashGeneratingUnit4326`.

The status tie-break exists because two BMPs can both have `TCEffect` 0 with different statuses (an in-stream trash boom is Partial; an unscreened inlet is None), and the trash results classify acreage by status. QGIS broke such ties by iteration order, which moved acreage between Partial and Untreated from run to run.

### Persistence

`pTrashGeneratingUnitDelete` truncates both tables, then rows are inserted in chunks of 2,000, then `pTrashGeneratingUnitMakeValid` and `pTrashGeneratingUnit4326MakeValid` run. The whole table is rebuilt every night; there is no delta refresh.

### Downstream

`vTrashGeneratingUnitLoadStatistic` joins each TGU back to its block, BMP, WQMP and OVTA Area to compute baseline, progress and current loading rates. Everything on the trash results pages and maps reads from that view.

## Load Generating Units

### Inputs

| Layer | View | Which rows |
|---|---|---|
| Regional Subbasins | `vPyQgisRegionalSubbasinLGUInput` | All, with their model basin. |
| Model basins | `vPyQgisModelBasinLGUInput` | All. Used only as the clip extent; they contribute no attributes. |
| WQMP boundaries | `vPyQgisWaterQualityManagementPlanLGUInput` | WQMPs with a boundary whose modeling approach is not Detailed. No filter on WQMP status. |
| Delineations | `vPyQgisDelineationLGUInput` | **Distributed** delineations on BMPs of a modeled type that do **not** belong to a project. Verified and provisional alike. |

Centralized delineations are not overlaid: a centralized BMP treats its whole upstream Regional Subbasin network, which Nereid handles through the subbasin graph.

### Rules

1. Clip all three layers to the modeling extent (the union of the model basins).
2. Union Regional Subbasins ∪ WQMPs, then that result ∪ delineations.
3. Keep only pieces that have a Regional Subbasin.
4. Each polygon becomes one LGU with `RegionalSubbasinID`, `ModelBasinID` and optional `WaterQualityManagementPlanID`, `DelineationID`.

**No layer is flattened.** See "Known gaps" below.

### Total and delta refresh

- **Total refresh** (no refresh area): `pDeleteLoadGeneratingUnitsPriorToTotalRefresh` empties `HRUCharacteristic` and `LoadGeneratingUnit` and reseeds the identity, then every piece is inserted. Enqueued by `RegionalSubbasinRefreshJob`; it then enqueues a full HRU refresh.
- **Delta refresh** (a `LoadGeneratingUnitRefreshArea`): the full county overlay is still computed, but only pieces touching the refresh area are kept. `pDeleteLoadGeneratingUnitsPriorToDeltaRefresh` deletes existing LGUs (and their HRU rows) that intersect the area, then the kept pieces are inserted. The refresh area is the union of the old and new shape of whatever was edited (`ModelingEngineUtilities.QueueLGURefreshForArea`).

Just before insert, pieces that reference a delineation deleted during the run are dropped (NPT-981).

### Downstream

1. `HRURefreshJob` sends LGUs that have no HRU characteristics yet (area over 10 m²) to OCGIS and stores land use, soil group, slope and impervious area per LGU in `HRUCharacteristic`.
2. `vNereidLoadingInput` turns those into land surfaces. `NereidUtilities.LandSurfaceNodeID` decides which model node each one drains to:
   - its **delineation's BMP**, if the delineation is verified and the BMP is not on a Simplified WQMP;
   - otherwise its **WQMP**, unless that WQMP is Detailed;
   - otherwise its **Regional Subbasin**.

   So a provisional delineation is tracked in the LGU layer but contributes no runoff to its BMP.
3. The Nereid delta solve and nightly total solve read those land surfaces.

## Project Load Generating Units

PLGUs answer a what-if question: how would loads and reductions change if this project's proposed BMPs were built?

### Inputs and rules

The pipeline is the LGU pipeline with two differences:

- **Delineations** come from `vPyQgisProjectDelineationLGUInput`, which returns every distributed, modeled delineation with its BMP's `ProjectID`. The controller keeps existing ones (`ProjectID` null) plus those of the project being solved. Other projects' delineations are excluded.
- **Regional Subbasins** are limited to the project's subbasins and everything upstream of them (`ProjectNetworkSolveJob` computes the list from `vRegionalSubbasinUpstream`).

Rows are written to `ProjectLoadGeneratingUnit` keyed by `ProjectID`, after `pDeleteProjectLoadGeneratingUnitsPriorToRefreshForProject` removes that project's previous rows. Existing-condition LGUs are never touched.

### How the comparison is made

`ProjectNetworkSolveJob` runs three steps for one project: regenerate PLGUs, fetch HRU characteristics for them (`ProjectHRUCharacteristic`), then one Nereid solve (`NereidService.ProjectNetworkSolve`).

- The solve reads land surfaces **only** from `vNereidProjectLoadingInput` (PLGUs). It does not read `LoadGeneratingUnit`.
- It includes the existing BMPs in those subbasins and the project's proposed BMPs, in a single run.
- A proposed delineation routes to its BMP whether or not it is verified. An existing provisional delineation still falls back to its WQMP or subbasin.
- Results are stored per node in `ProjectNereidResult`.

There is no separate "before" run. `fProjectGrantScore` splits the one run by whether each BMP belongs to the project:

```
percent reduced = reduction by the project's BMPs
                  / (load generated − reduction by existing BMPs)
```

The existing-BMP term is zero when the project has only distributed BMPs.

The project's scoring footprint is narrower than the area the solve covers. It is the union of:

- for the project's **distributed** BMPs, only the PLGUs whose delineation belongs to one of those BMPs;
- for the project's **centralized** BMPs, every PLGU in the Regional Subbasins upstream of the BMP's subbasin.

Existing-condition and untreated ground elsewhere in the solve area is not part of a distributed project's footprint.

## Known gaps (as of November 2026)

Measurements and affected records are kept on the Jira cards named below, not in this file.

### LGUs and PLGUs overlap each other

Because the LGU and PLGU pipelines do not flatten their inputs and `UnionLayers` assumes non-overlapping layers, overlapping WQMP boundaries or overlapping delineations produce overlapping units. The ground under them is counted more than once.

This affects both `LoadGeneratingUnit` and, within a single project, `ProjectLoadGeneratingUnit`. Measurements are on NPT-1155. TGUs can overlap too, from a different cause; see "Overlay results can be silently wrong" below.

Overlapping pairs fall into three groups: pairs that differ by WQMP, pairs that differ by delineation, and pairs with identical attributes. The first two follow from overlapping inputs. The identical-attribute duplicates do not; the delta refresh is the suspected cause and this has not been confirmed (NPT-1158).

What the double counting does downstream, for two overlapping delineations in `LoadGeneratingUnit`:

| Verified status of the two delineations | Effect |
|---|---|
| Both verified | The ground's load is routed to two BMPs. |
| Neither verified | Both pieces fall back to the same WQMP or subbasin node; its load is doubled. |
| One verified, one not | Counted once as treated and once as untreated. |

In the planning module, a proposed delineation can overlap an existing one. Since the score divides the project's reduction by generated load, that ground inflates both numbers.

**No winner rule has been decided for modeling.** For planning, the intent is that a proposed delineation takes the ground it covers and the existing delineation keeps the remainder. For existing conditions (two WQMPs, or two delineations) the rule is open; the trash rule is specific to trash capture and should not be assumed to apply.

### Overlay results can be silently wrong (NPT-1143)

TGUs for some Land Use Blocks total more than the block's area, in many cases almost exactly twice it.

Traced on one block: the flatten and the union of the hit geometries are correct, but `OverlayEngine.Intersection` / `Difference` of the block against a large unioned polygon return the wrong answer. OverlayNG in floating-precision mode reports an intersection of 0 and a difference equal to the whole block, without throwing, so `OverlayNGRobust` never falls back and the `Buffer(0)` retry never runs. The block is then written twice: once as attributed pieces and once as an unattributed remainder. On the same inputs, OverlayNG with a fixed 1 mm precision model, the legacy overlay, and subtracting the hits one at a time all give the correct result.

Other affected blocks have no delineations, so their duplicates come from a different union step (OVTA Areas or WQMPs). The same failure is likely there but has not been confirmed. The LGU and PLGU pipelines use the same two functions, so some LGU and PLGU overlaps may come from this cause and not from overlapping inputs.

### Table replacement is not atomic

TGU and total-LGU refreshes delete first and insert afterwards with no transaction. A failure during insert leaves the table empty or partial until the next run, and readers can see partial data while a run is in progress. `pTrashGeneratingUnitDelete` also drops and re-adds a foreign key on every run. A total LGU refresh reseeds the identity, so every `LoadGeneratingUnitID` changes.

### `LoadGeneratingUnitRefreshArea.ProcessDate` does not show whether a delta ran

The column is set when the row is created and again when the refresh completes, so a failed or never-run delta looks the same as a successful one. The table is never pruned. Tracked in NPT-1158.

### Inputs that rely on convention

- The OVTA gate of two or more assessments keeps most progress-scored areas out of TGUs, because a progress score needs only one completed assessment (NPT-1144). The view also counts an assessment as completed by a non-null `CompletedDate`, which disagrees with the assessment status on some rows.
- Duplicate OVTA Areas lose silently. `Flatten` deletes exact geometric duplicates, and on an assessment-date tie the lower ID loses, so the losing area gets no TGUs (an example is on NPT-1144).
- `vPyQgisDelineationTGUInput` does not exclude delineations on project BMPs. They stay out of TGUs only as long as no project delineation is verified.
- Neither WQMP view filters on WQMP status, so Inactive and Draft WQMPs are in the overlay inputs.
- The views match trash capture status on display-name strings and use literal IDs (`DelineationTypeID = 2`, modeling approach `<> 1`). The `TCEffect` expression is duplicated in two views.
- `vPyQgisWaterQualityManagementPlanTGUInput` joins `vStormwaterJurisdictionOrganizationMapping` and selects nothing from it.

### Unused objects

`vProjectWetWeatherWQLRIScore` and `vProjectDryWeatherWQLRIScore` are not referenced by any hand-written application code or other SQL object (only by their generated EF classes); `fProjectGrantScore` replaced them. They sum reductions across all BMPs in a project run without separating the project's own.

## Where things live

| What | Where |
|---|---|
| Endpoints, pipelines, winner rules | `Neptune.OverlayAPI/Controllers/OverlayController.cs` |
| Geometry operations | `Neptune.OverlayAPI/Services/Overlay/OverlayEngine.cs`, `OverlayFeature.cs` |
| Engine tests | `Neptune.Tests/OverlayEngineTests.cs` |
| Input views | `Neptune.Database/dbo/Views/dbo.vPyQgis*.sql` |
| Delete / make-valid procs | `Neptune.Database/dbo/Procs/` (`pTrashGeneratingUnitDelete`, `pDeleteLoadGeneratingUnitsPriorTo*`, `pDeleteProjectLoadGeneratingUnitsPriorToRefreshForProject`, `p*MakeValid`) |
| HTTP client used by the API | `Neptune.Common/Services/OverlayAPIService.cs` |
| Jobs | `Neptune.Jobs/Hangfire/` (`TrashGeneratingUnitRefreshJob`, `LoadGeneratingUnitRefreshJob`, `ProjectNetworkSolveJob`, `HRURefreshJob`, `RegionalSubbasinRefreshJob`) |
| Delta refresh queueing | `Neptune.API/Common/ModelingEngineUtilities.cs` |
| Land-surface routing | `Neptune.EFModels/Nereid/NereidUtilities.cs` (`LandSurfaceNodeID`) |
| Planning score | `Neptune.Database/dbo/Functions/dbo.fProjectGrantScore.sql` |
