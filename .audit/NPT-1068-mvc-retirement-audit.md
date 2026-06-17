# NPT-1068 Pre-Delete Audit: Full MVC → SPA Coverage

**Goal:** Retire `Neptune.WebMvc` entirely. This audit classifies every public action on every MVC controller against SPA coverage so deletion PRs can be scoped intentionally and gaps surface before anything irreversible ships.

## Executive Summary

| | Count |
|---|---|
| Controllers audited | 41 |
| Total actions | ~358 |
| ✅ SPA-covered | ~292 |
| 🗑️ Retired (intentional, classified by audit) | ~12 |
| ⚠️ Gaps blocking total MVC retirement | ~54 |

**Inventory-cluster status:** ✅ All gaps filled. PR 2 (the MVC retirement) is unblocked.

Gaps closed during the audit pass:

- ✅ **RefreshOCTAPrioritizationLayerFromOCSurvey** — shipped in `7f580e219`. New `/octa-prioritizations/enqueue-refresh` endpoint + sixth tile in the SPA `/data-hub` County GIS tab, with seeded NeptunePageType 100 rich-text matching the sibling tiles.
- ✅ **WQMP supporting-document CRUD** — shipped in `c19693a96` after PO confirmed jurisdiction managers still need to upload typed supporting docs (Final WQMP / As-built drawings / O&M Plan / Other). New POST/PUT/DELETE on `WaterQualityManagementPlanController` under the existing `/documents` prefix; gated on `JurisdictionManageFeature`. Single `wqmp-document-modal` handles add + edit modes with optional file-replace on edit.

**False positives caught during audit review** (originally flagged ⚠️, now ✅):

- TreatmentBMP documents — card-header "Upload Document" button at treatment-bmp-detail.component.html:546 calls `openDocumentUploadModal()` → `FileUploadModalComponent` → `uploadDocument()` → `createTreatmentBMPDocumentByTreatmentBMP`. The `[allowUploading]="false"` on the inner file-resource-list is intentional because upload lives in the card header.
- EditModelingAttributes — Modeling Attributes panel at treatment-bmp-detail.component.html:160 has an "Edit" button routing to `/treatment-bmps/:id/edit-custom-attributes/Modeling`. SPA consolidates MVC's two separate actions (`EditOtherDesignAttributes` + `EditModelingAttributes`) into one `treatment-bmp-update-custom-attributes` component parameterized by `CustomAttributeTypePurposeEnum`.

**Intentional UX changes** (retired, not gaps) to call out in PR 2 description:
- Bulk-delete BMPs (manager-only batch cleanup) — SPA has per-row delete only
- BMP Type observation- and attribute-type sort-order modals — SPA uses insertion-order = display-order in the type editor

Total MVC retirement (a future-story scope) faces broader gaps in: NeptunePage CMS admin, NeptuneHomePageImage admin, RoleController, PersonOrganization primary contacts, and several admin/help flows.

## Recommended Action Plan

### Immediate (NPT-1068 PR 2 — Inventory Delete)

**Blockers:** None remaining. Both inventory-cluster gaps are closed:
- ✅ OCTA Prioritization refresh — `7f580e219`
- ✅ WQMP supporting-document CRUD — `c19693a96`

**Intentional UX changes to call out in PR 2 description:**
- **Bulk-delete BMPs retired.** MVC let jurisdiction managers checkbox-select N BMPs and delete in one shot with a preview modal. SPA only supports per-row delete via the grid's row action menu. If batch cleanups become painful in practice, we'll revisit.
- **BMP Type observation- and attribute-type sort-order modals retired.** MVC had admin-only drag-to-reorder modals (`EditObservationTypesSortOrder`, `EditAttributeTypesSortOrder`). SPA's BMP type editor uses insertion-order = display-order. To reinsert in the middle, admins remove subsequent rows and re-add. BMP types are configured rarely so the trade-off is acceptable; revisit with `cdkDragDrop` if friction surfaces.
- **OCTA Prioritization refresh moved to Data Hub.** MVC parked the refresh link in the BMP Index admin dropdown; SPA puts it in the `/data-hub` County GIS tab alongside the other OC Survey refreshes, where its actual consumer (planning module OCTA M2 Tier 2 dashboard) sits architecturally.

**Then PR 2 can safely delete:** 4 inventory controllers + 7 satellites (assessments, types, benchmarks, images, maintenance records, delineation geometry, assessment list) + LaunchPad section of HomeController.

### Future stories (Total MVC retirement)
- **CMS admin gap** — NeptunePage (7 actions) + NeptuneHomePageImage (6 actions). Currently the only way to edit `NeptunePages` content.
- **Role management gap** — RoleController has no SPA equivalent (Index, Detail, PersonWithRole grid).
- **Specialized support flows** — RequestOrganizationNameChange, RequestToChangePrivileges, BulkUploadRequest. SPA has generic support form only.
- **PersonOrganization primary contacts** — no SPA admin UI.
- **VerifyEmailRequired** — Auth0 email-unverified UI not in SPA.
- **DataHub: `BulkUploadRequest` help page** — verify SPA Data Hub already covers what this page documented.

---

## Per-Controller Classification

Status legend: ✅ SPA-covered · 🗑️ Retired (intentional) · ⚠️ Gap

### 1. Inventory Core (NPT-1068 primary delete targets)

#### TreatmentBMPController (52 actions, 1,286 LOC, 17 views)

| Action | Status | SPA equivalent / notes |
|---|---|---|
| FindABMP | ✅ | `/find-bmp` → find-bmp.component |
| Index | ✅ | `/treatment-bmps` → treatment-bmps.component |
| TreatmentBMPGridJsonData | ✅ | `TreatmentBMPService.listTreatmentBMP()` |
| TreatmentBMPAssessmentSummary | ✅ | `/latest-bmp-assessments` |
| TreatmentBMPAssessmentSummaryGridJsonData | ✅ | `TreatmentBMPAssessmentService.listTreatmentBMPAssessment()` |
| Detail | ✅ | `/treatment-bmps/:id` → treatment-bmp-detail.component |
| HRUCharacteristicGridJsonData | ✅ | `TreatmentBMPService.listHRUCharacteristicsTreatmentBMP()` |
| New | ✅ | `/treatment-bmps/new` → create-treatment-bmp.component |
| Edit | ✅ | `/treatment-bmps/:id/edit-basic-info` |
| EditUpstreamBMP | ✅ | Modal: treatment-bmp-update-upstream-bmp-modal |
| RemoveUpstreamBMP | ✅ | `TreatmentBMPService.updateUpstreamBMPTreatmentBMP()` |
| VerifyInventory | ✅ | Delineation verification step in project workflow |
| ConvertTreatmentBMPType | ✅ | Modal: treatment-bmp-update-type-modal |
| Delete | ✅ | `TreatmentBMPService.deleteTreatmentBMP()` |
| BulkDeleteTreatmentBMPs | 🗑️ Retired | Per-row delete in SPA grid is the new UX. **Call out in PR 2 description.** |
| BulkDeleteTreatmentBMPsModal | 🗑️ Retired | Same as above. |
| QueueLGURefreshForTreatmentBMP | ✅ | `treatment-bmp-detail.component.ts:521` `confirmRefreshLandUse` → `queueRefreshLandUseTreatmentBMP` (API `PUT /treatment-bmps/{id}/queue-refresh-land-use`) |
| SummaryForMap | ✅ | Map popups in treatment-bmps.component |
| FindByName | ✅ | Search in treatment-bmps grid |
| EditOtherDesignAttributes | ✅ | `/treatment-bmps/:id/edit-custom-attributes/:purposeID` |
| ViewTreatmentBMPModelingAttributes | ✅ | Read-only panel in treatment-bmp-detail |
| ViewTreatmentBMPModelingAttributesGridJsonData | ✅ | `TreatmentBMPService.listWithModelingAttributesTreatmentBMP()` |
| EditModelingAttributes | ✅ | "Edit" button on Modeling Attributes panel (treatment-bmp-detail.component.html:160) → `/treatment-bmps/:id/edit-custom-attributes/Modeling` (parameterized custom-attributes editor, same component as EditOtherDesignAttributes) |
| EditLocation | ✅ | `/treatment-bmps/:id/edit-location` |
| EditLocationFromDelineationMap | ✅ | Delineations step in project workflow |
| RefreshModelBasinsFromOCSurvey | ✅ | `/data-hub` County GIS tab → `modelBasinService.enqueueRefreshModelBasin()` |
| RefreshPrecipitationZonesFromOCSurvey | ✅ | `/data-hub` County GIS tab → `precipitationZoneService.enqueueRefreshPrecipitationZone()` |
| RefreshOCTAPrioritizationLayerFromOCSurvey | ✅ | `/data-hub` County GIS tab → `octaPrioritizationService.enqueueRefreshOCTAPrioritization()` (added in `7f580e219`) |
| MapPopup | ✅ | NeptuneMapComponent layer popups |
| DownloadBMPsToGIS | ✅ | `/data-hub/treatment-bmp-download` |
| BMPInventoryExport | ✅ | Same as DownloadBMPsToGIS (Data Hub) |
| UploadBMPs | ✅ | `/data-hub/treatment-bmp-upload` |
| GetModelResults | ✅ | `TreatmentBMPService.getLoadReducingResultTreatmentBMP()` |
| TrashMapAssetPanel | 🗑️ | Confirmed retired. SPA `trash-home` uses simple Leaflet `bindPopup` with name + link instead of the MVC sidebar asset panel. |

#### WaterQualityManagementPlanController (50 actions, 1,350 LOC, 22 views)

All actions ✅ SPA-covered. Full mapping:
- FindAWQMP/FindByName → wqmps.component search
- Index, IndexGridData, VerificationGridData, LGUAudit, LGUAuditGridData → `/water-quality-management-plans`, `/manage/wqmp-lgu-audit`
- Detail + nested grids → `/water-quality-management-plans/:id`
- New, Edit, EditNotes, Delete → wqmp-detail + modals
- EditTreatmentBMPs, EditSimplifiedStructuralBMPs, EditSourceControlBMPs, EditParcels, RefineArea → routed editor pages under wqmp detail
- WqmpVerify, NewWqmpVerify, EditWqmpVerify, DeleteVerify, EditWqmpVerifyModal → `/water-quality-management-plan-verifications` + verification-wizard-outlet
- EditModelingApproach, GetModelResults → wqmp-detail
- Upload* + template downloads → Data Hub pages (`/data-hub/wqmp-upload`, etc.)
- WqmpModelingOptions → `/wqmp-modeling-options`
- AnnualReport + 2 grid data endpoints → `/wqmp-annual-report`

#### DelineationController (14 actions, 395 LOC, 3 views)

All actions ✅ SPA-covered (delineation-map, reconciliation-report, delineation editing on BMP detail, discrepancy check).

#### FieldVisitController (41 actions, 1,612 LOC, 18 views)

All actions ✅ SPA-covered via the field-visit wizard routes under `/field-visits/:fieldVisitID/...` (inventory, location, photos, attributes, assessment, post-maintenance, maintenance, summary, observations, etc.) + Data Hub trash-screen upload.

### 2. Inventory Satellites

#### TreatmentBMPAssessmentController (7 actions) — all ✅

#### TreatmentBMPAssessmentObservationTypeController (17 actions)
- Index, Manage, Grid endpoints, New, Edit, Detail, Delete — ✅ at `/program-info/observation-types` + `/manage/observation-types`
- **DiscreteDetailSchema, PassFailDetailSchema, PercentageDetailSchema** — 🗑️ Retired. Inlined in SPA `observation-type-detail.component.html` (typed sections rendered per collection method).
- **RateDetailSchema** — 🗑️ Retired. The `Rate` collection method (enum value 2) was removed from the system entirely; only DiscreteValue/PassFail/Percentage remain in `ObservationTypeCollectionMethodEnum`. MVC action is dead code.
- **PreviewObservationType (GET+POST)** — 🗑️ Retired. Replaced by `ObservationTypePreviewModalComponent` (opened from a "Preview" button on the SPA observation-type detail page).

#### TreatmentBMPBenchmarkAndThresholdController (3 actions) — all ✅

#### TreatmentBMPDocumentController (6 actions) — fully covered
| Action | Status | Notes |
|---|---|---|
| New (GET+POST) | ✅ | Card-header "Upload Document" button at treatment-bmp-detail.component.html:546 → `openDocumentUploadModal()` → `FileUploadModalComponent` → `uploadDocument()` → `createTreatmentBMPDocumentByTreatmentBMP`. (`[allowUploading]="false"` on inner file-resource-list is intentional.) |
| Edit (GET+POST) | ✅ | `<file-resource-list>` `fileResourceUpdated` → `onDocumentUpdated()` |
| Delete (GET+POST) | ✅ | `<file-resource-list>` `fileResourceDeleted` → `onDocumentDeleted()` |

#### TreatmentBMPImageController (2 actions) — ✅ via `/treatment-bmps/:id/edit-images`

#### TreatmentBMPTypeController (15 actions)
- Manage/Grids/New/Edit/Index/Detail/Delete — ✅ at `/manage/treatment-bmp-types`, `/program-info/treatment-bmp-types`
- **EditObservationTypesSortOrder (GET+POST), EditAttributeTypesSortOrder (GET+POST)** — 🗑️ Retired. SPA `treatment-bmp-type-edit` uses insertion-order = display-order (no drag-reorder UI). SortOrder is auto-assigned on add and renumbered on remove. Call out in PR 2 description.

#### WaterQualityManagementPlanDocumentController (6 actions) — covered as of `c19693a96`
| Action | Status | Notes |
|---|---|---|
| New (GET+POST) | ✅ | "Upload Document" button in wqmp-detail Documents card header opens `wqmp-document-modal` (add mode) → `POST /water-quality-management-plans/{id}/documents` (multipart: File + DisplayName + DocumentTypeID + Description), `[JurisdictionManageFeature]` |
| Edit (GET+POST) | ✅ | Per-row "Edit" link opens `wqmp-document-modal` (edit mode) with file optional → `PUT /water-quality-management-plans/{id}/documents/{docID}`. When File present, old blob is deleted after entity updates to the new FileResource |
| Delete (GET+POST) | ✅ | Per-row "Delete" link → ConfirmService → `DELETE /water-quality-management-plans/{id}/documents/{docID}`. Blob cleaned up |

#### MaintenanceRecordController (4 actions) — all ✅

#### DelineationGeometryController (6 actions) — all ✅ via `/delineation/gdb-{upload,download,approve}`

#### AssessmentController (2 actions) — ✅ via `/latest-bmp-assessments`

### 3. Trash / OVTA

#### OnlandVisualTrashAssessmentController (3) / OnlandVisualTrashAssessmentAreaController (4) / OnlandVisualTrashAssessmentExportController (2)

All 9 actions ✅ SPA-covered via Data Hub pages: `/data-hub/ovta-upload`, `/data-hub/ovta-area-upload`, `/data-hub/ovta-area-approve`, `/data-hub/ovta-area-download`.

### 4. Modeling / GIS

#### HRUCharacteristicController (4) — all ✅ via `pages/hru-characteristics`
#### LoadGeneratingUnitController (4) — all ✅ via `pages/load-generating-units`
#### RegionalSubbasinController (11) — all ✅ via `pages/regional-subbasins`
#### RegionalSubbasinRevisionRequestController (8) — all ✅ via `/delineation/revision-requests`
#### LandUseBlockGeometryController (4) — all ✅ via `/data-hub/land-use-block-{upload,download}`

#### ParcelController (11)
- Index, FindByAddress, FindSimpleByAddress, FindByAPN, FindSimpleByAPN, RefreshParcelsFromOCSurvey — ✅
- **SummaryForMap, TrashMapAssetPanel, Union (GET+POST)** — agent flagged ⚠️. **Verify**: these may be MVC partials replaced by SPA map popups, or `Union` may still be called by WQMP boundary creation. Grep needed.

### 5. Admin / Lookups

#### FieldDefinitionController (5) — all ✅ via `/field-definitions`
#### FundingSourceController (9) — all ✅ via `/funding-sources`
#### FundingEventController (6) — all ✅ via funding event modal in treatment-bmp-detail
#### CustomAttributeTypeController (10) — all ✅ via `/manage/custom-attributes`
#### OrganizationController (9) — all ✅ via `/organizations`

#### **PersonOrganizationController (2) — ⚠️ GAP**
| Action | Status | Notes |
|---|---|---|
| EditPersonOrganizationPrimaryContacts (GET+POST) | ⚠️ Gap | No SPA UI for primary contact assignment |

#### JurisdictionController (6) — all ✅ via `/jurisdictions`

#### **RoleController (4) — ⚠️ ALL GAPS**
| Action | Status | Notes |
|---|---|---|
| Index | ⚠️ Gap | No SPA role list |
| IndexGridJsonData | ⚠️ Gap | No SPA endpoint |
| PersonWithRoleGridJsonData | ⚠️ Gap | No SPA page for "people by role" |
| Detail | ⚠️ Gap | Role info only embedded in user-detail; no standalone role page |

#### UserController (12) — all ✅ via `/users`

### 6. Infrastructure / Pages

#### AccountController (5)
- Login, Register, Logout, NotAuthorized — ✅ via Auth0 Angular SDK
- **VerifyEmailRequired** — ⚠️ Gap, no SPA UI for email-unverified state

#### HomeController (10)
- **Index (LaunchPad)** — 🗑️ retired per NPT-1068 AC
- ExportGridToExcel — ✅ via ag-Grid built-in export
- Error, ViewPageContent, About, AboutModelingBMPPerformance, Legal, Modeling, Training — ✅ via SPA routes
- **ManageHomePageImages** — ⚠️ Gap (no SPA admin UI)

#### HelpController (7)
- Support (GET+POST) — ✅ via `/support`
- **RequestOrganizationNameChange (GET+POST)** — ⚠️ Gap (no specialized form; generic support only)
- **RequestToChangePrivileges (GET+POST)** — ⚠️ Gap (same)
- **BulkUploadRequest** — ⚠️ Gap (no SPA help page; verify Data Hub copy already covers)

#### DataHubController (1) — ✅ via `/data-hub`

#### BulkRowController (12) — all ✅ via Manager Dashboard tabs and bulk-verify endpoints

#### FileResourceController (2)
- DisplayResource (by GUID) — ✅ via `fileResourceUrl()` helper
- **DisplayResourceByID (by primary key)** — ⚠️ Verify: SPA uses GUID-only. If no live consumers, classify as 🗑️ retired.

#### **NeptuneHomePageImageController (6) — ⚠️ ALL GAPS** (admin-only image gallery management)

#### **NeptunePageController (7) — ⚠️ ALL GAPS** (CMS-style editable content pages with TinyMCE)

#### ManagerDashboardController (4) — all ✅ via `/dashboard`

---

## Gap Inventory (for tracking)

### Inventory-cluster gaps (block NPT-1068 PR 2)
- [x] OCTA Prioritization refresh — `7f580e219`
- [x] WQMP supporting-document CRUD — `c19693a96` (PO confirmed still required)

### Adjacent-cluster gaps (future MVC-retirement stories)
- [ ] NeptunePageController (CMS admin) — 7 actions
- [ ] NeptuneHomePageImageController (image gallery admin) — 6 actions
- [ ] RoleController — 4 actions
- [ ] PersonOrganizationController.EditPersonOrganizationPrimaryContacts — 2 actions
- [ ] AccountController.VerifyEmailRequired
- [ ] HelpController specialized request forms — 5 actions
- [ ] Confirm/retire: FileResourceController.DisplayResourceByID, Parcel.Union, Parcel.SummaryForMap, Parcel.TrashMapAssetPanel
