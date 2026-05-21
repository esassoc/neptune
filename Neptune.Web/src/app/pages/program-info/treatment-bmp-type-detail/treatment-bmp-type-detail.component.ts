import { Component, inject, Input, numberAttribute, OnInit } from "@angular/core";
import { AsyncPipe } from "@angular/common";
import { Router, RouterLink } from "@angular/router";
import { ColDef, ValueGetterParams } from "ag-grid-community";
import { BehaviorSubject, catchError, combineLatest, EMPTY, map, Observable, shareReplay, switchMap, tap } from "rxjs";
import { escapeHtml } from "src/app/shared/helpers/html-escape";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { LoadingDirective } from "src/app/shared/directives/loading.directive";
import { NeptuneGridComponent } from "src/app/shared/components/neptune-grid/neptune-grid.component";
import { ConfirmService } from "src/app/shared/services/confirm/confirm.service";
import { AlertService } from "src/app/shared/services/alert.service";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { AuthenticationService } from "src/app/services/authentication.service";
import { UtilityFunctionsService } from "src/app/services/utility-functions.service";
import { TreatmentBMPService } from "src/app/shared/generated/api/treatment-bmp.service";
import { TreatmentBMPTypeService } from "src/app/shared/generated/api/treatment-bmp-type.service";
import { TreatmentBMPTypeDetailDto } from "src/app/shared/generated/model/treatment-bmp-type-detail-dto";
import { TreatmentBMPTypeObservationTypeDetailDto } from "src/app/shared/generated/model/treatment-bmp-type-observation-type-detail-dto";
import { TreatmentBMPTypeCustomAttributeTypeDetailDto } from "src/app/shared/generated/model/treatment-bmp-type-custom-attribute-type-detail-dto";
import { TreatmentBMPByTypeGridDto } from "src/app/shared/generated/model/treatment-bmpby-type-grid-dto";
import { CustomAttributeTypePurposeEnum, CustomAttributeTypePurposes } from "src/app/shared/generated/enum/custom-attribute-type-purpose-enum";
import { CustomAttributeDataTypeEnum } from "src/app/shared/generated/enum/custom-attribute-data-type-enum";

interface AttributesByPurpose {
    purposeID: number;
    purposeDisplayName: string;
    rows: TreatmentBMPTypeCustomAttributeTypeDetailDto[];
}

@Component({
    selector: "treatment-bmp-type-detail",
    standalone: true,
    imports: [AsyncPipe, RouterLink, PageHeaderComponent, LoadingDirective, NeptuneGridComponent],
    templateUrl: "./treatment-bmp-type-detail.component.html",
    styleUrl: "./treatment-bmp-type-detail.component.scss",
})
export class TreatmentBmpTypeDetailComponent implements OnInit {
    @Input({ transform: numberAttribute }) treatmentBMPTypeID!: number;

    private bmpTypeService = inject(TreatmentBMPTypeService);
    private bmpService = inject(TreatmentBMPService);
    private alertService = inject(AlertService);
    private authenticationService = inject(AuthenticationService);
    private utility = inject(UtilityFunctionsService);
    private confirmService = inject(ConfirmService);
    private router = inject(Router);

    public bmpType$: Observable<TreatmentBMPTypeDetailDto>;
    // BehaviorSubject + asObservable so local mutations (e.g. row delete) reactively flow
    // through to both the grid rowData binding and the totals-row derivation. A naive
    // applyTransaction({ remove }) on the grid API would only update the grid in place,
    // leaving the pinned totals row computed from stale pre-delete data.
    private bmpsSubject = new BehaviorSubject<TreatmentBMPByTypeGridDto[]>([]);
    public bmps$: Observable<TreatmentBMPByTypeGridDto[]> = this.bmpsSubject.asObservable();
    public bmpTotalsRow$: Observable<TreatmentBMPByTypeGridDto[]>;
    public bmpColumnDefs: ColDef[] = [];
    public bmpDownloadFileName = "treatment-bmps-of-type";
    public isLoading = true;

    public get isAdmin(): boolean {
        return this.authenticationService.isCurrentUserAnAdministrator();
    }

    ngOnInit(): void {
        this.bmpType$ = this.bmpTypeService.getDetailTreatmentBMPType(this.treatmentBMPTypeID).pipe(
            tap((bmpType) => {
                this.isLoading = false;
                this.bmpColumnDefs = this.buildBMPColumnDefs(bmpType);
                this.bmpDownloadFileName = `${bmpType.TreatmentBMPTypeName}-treatment-bmps`;
            }),
            catchError(() => {
                this.isLoading = false;
                this.alertService.pushAlert(new Alert("Failed to load Treatment BMP Type.", AlertContext.Danger));
                return EMPTY;
            }),
            shareReplay(1),
        );

        // Chain off bmpType$ so the column-defs/header build first, then we fetch rows + push
        // them into the BehaviorSubject. Subsequent local mutations (delete handler) push
        // updated arrays without re-hitting the API.
        this.bmpType$
            .pipe(
                switchMap(() => this.bmpTypeService.listBMPsByTypeTreatmentBMPType(this.treatmentBMPTypeID)),
                catchError(() => {
                    this.alertService.pushAlert(new Alert("Failed to load Treatment BMPs of this type.", AlertContext.Danger));
                    return EMPTY;
                }),
            )
            .subscribe((rows) => this.bmpsSubject.next(rows));

        // Pinned-bottom totals row — sums # of Assessments + # of Maintenance Events + every
        // numeric custom-attribute column (Decimal/Integer, non-Maintenance purpose) across
        // the in-scope BMPs, mirroring the legacy MVC DhtmlxGridColumnAggregationType.Total
        // column footer. Custom-attribute sums are stamped back into the
        // `CustomAttributeValues` map as strings so the per-column inline ValueGetter (which
        // parses the string to a number) renders them naturally — no special-case rendering.
        // Non-numeric columns stay blank because their value getters / formatters short-
        // circuit on pinned rows or read nothing from the empty data shape.
        this.bmpTotalsRow$ = combineLatest([this.bmpType$, this.bmps$]).pipe(
            map(([bmpType, rows]) => {
                const customAttributeValues: { [id: number]: string } = {};
                const numericAttrs = (bmpType.CustomAttributeTypes ?? []).filter(
                    (c) =>
                        c.CustomAttributeTypePurposeID !== CustomAttributeTypePurposeEnum.Maintenance &&
                        (c.CustomAttributeDataTypeID === CustomAttributeDataTypeEnum.Decimal ||
                            c.CustomAttributeDataTypeID === CustomAttributeDataTypeEnum.Integer),
                );
                for (const cat of numericAttrs) {
                    const sum = rows.reduce((s, r) => {
                        const v = (r.CustomAttributeValues ?? {})[cat.CustomAttributeTypeID];
                        const n = parseFloat(v ?? "");
                        return s + (isNaN(n) ? 0 : n);
                    }, 0);
                    customAttributeValues[cat.CustomAttributeTypeID] = sum.toString();
                }
                return [
                    {
                        NumberOfAssessments: rows.reduce((sum, r) => sum + (r.NumberOfAssessments ?? 0), 0),
                        NumberOfMaintenanceRecords: rows.reduce((sum, r) => sum + (r.NumberOfMaintenanceRecords ?? 0), 0),
                        CustomAttributeValues: customAttributeValues,
                    } as TreatmentBMPByTypeGridDto,
                ];
            }),
        );
    }

    public weightDisplay(ot: TreatmentBMPTypeObservationTypeDetailDto): string {
        if (ot.AssessmentScoreWeight == null) return "pass/fail";
        return `${Number(ot.AssessmentScoreWeight)}%`;
    }

    public attributesByPurpose(bmpType: TreatmentBMPTypeDetailDto): AttributesByPurpose[] {
        // Match MVC's Detail.cshtml: render one card per CustomAttributeTypePurpose, in enum order,
        // even when a purpose has zero attributes (so readers can confirm "Modeling Attributes: none").
        return CustomAttributeTypePurposes.map((purpose) => ({
            purposeID: purpose.Value,
            purposeDisplayName: purpose.DisplayName,
            rows: (bmpType.CustomAttributeTypes ?? [])
                .filter((c) => c.CustomAttributeTypePurposeID === purpose.Value),
        }));
    }

    // NPT-1038 round 4: builds the "Treatment BMPs of this Type" grid column defs.
    // Static columns mirror the legacy MVC TreatmentBMPsInTreatmentBMPTypeGridSpec subset that
    // already lives in vTreatmentBMPDetailed; dynamic columns are added from the type's
    // CustomAttributeTypes list, picking a column-def helper per data type so filters use the
    // right widget (Yes/No dropdown for PickFromList, date filter for DateTime, etc.).
    private buildBMPColumnDefs(bmpType: TreatmentBMPTypeDetailDto): ColDef[] {
        const canEdit = this.authenticationService.doesCurrentUserHaveJurisdictionEditPermission();
        // NPT-1038 round 4: actions column mirroring the legacy MVC pencil/trash/View icons.
        // View is always present; Edit + Delete are gated on JurisdictionEditPermission
        // (Admin/JM/JE). Edit routes to the basic-info editor since the SPA splits BMP
        // editing into per-section sub-routes (no single /edit page like the legacy had).
        const actionsCol = this.utility.createActionsColumnDef((params: any) => {
            const actions: { ActionName: string; ActionIcon?: string; ActionHandler: () => void }[] = [
                {
                    ActionName: "View",
                    ActionHandler: () => this.router.navigate(["/treatment-bmps", params.data.TreatmentBMPID]),
                },
            ];
            if (canEdit) {
                actions.push({
                    ActionName: "Edit",
                    ActionIcon: "fa fa-pencil",
                    ActionHandler: () => this.router.navigate(["/treatment-bmps", params.data.TreatmentBMPID, "edit-basic-info"]),
                });
                actions.push({
                    ActionName: "Delete",
                    ActionIcon: "fa fa-trash text-danger",
                    ActionHandler: () => this.deleteBMP(params),
                });
            }
            return actions;
        });

        // For the pinned-bottom totals row, suppress the context-menu renderer and show a
        // "Totals" label here instead of in the BMP Name column. cellRendererSelector is
        // authoritative — to keep ag-Grid from falling back to the existing cellRenderer when
        // we return undefined for the pinned row, clear cellRenderer entirely and have the
        // selector explicitly return the ContextMenu renderer for normal rows.
        const originalRenderer = actionsCol.cellRenderer;
        const originalValueGetter = actionsCol.valueGetter as ((params: any) => unknown) | undefined;
        actionsCol.cellRenderer = undefined;
        actionsCol.cellRendererSelector = (params: any) => {
            if (params.node?.rowPinned) return undefined;
            return { component: originalRenderer };
        };
        actionsCol.valueGetter = (params: any) => {
            if (params.node?.rowPinned === "bottom") return "Totals";
            return originalValueGetter ? originalValueGetter(params) : null;
        };
        actionsCol.cellClass = (params: any) => (params.node?.rowPinned ? "fw-bold" : "context-menu-container");

        const cols: ColDef[] = [
            actionsCol,
            this.utility.createLinkColumnDef("Treatment BMP", "TreatmentBMPName", "TreatmentBMPID", {
                InRouterLink: "/treatment-bmps/",
                FieldDefinitionType: "TreatmentBMP",
            }),
            this.utility.createLinkColumnDef("Jurisdiction", "StormwaterJurisdictionName", "StormwaterJurisdictionID", {
                InRouterLink: "/jurisdictions/",
                FieldDefinitionType: "Jurisdiction",
                UseCustomDropdownFilter: true,
            }),
            this.utility.createBasicColumnDef("Owner Organization", "OwnerOrganizationName", { UseCustomDropdownFilter: true }),
            this.utility.createBasicColumnDef("Year Built", "YearBuilt"),
            this.utility.createBasicColumnDef("ID in System of Record", "SystemOfRecordID"),
            this.utility.createLinkColumnDef("Water Quality Management Plan", "WaterQualityManagementPlanName", "WaterQualityManagementPlanID", {
                InRouterLink: "/water-quality-management-plans/",
            }),
            this.utility.createBasicColumnDef("Notes", "Notes"),
            this.utility.createDateColumnDef("Last Assessment Date", "LatestAssessmentDate", "MM/dd/yyyy"),
            // Legacy MVC GridSpec formats Last Assessed Score with one decimal place ("0.0").
            this.utility.createDecimalColumnDef("Last Assessed Score", "LatestAssessmentScore", { DecimalPlacesToDisplay: 1 }),
            this.utility.createBasicColumnDef("# of Assessments", "NumberOfAssessments"),
            this.utility.createDateColumnDef("Last Maintenance Date", "LatestMaintenanceDate", "MM/dd/yyyy"),
            this.utility.createBasicColumnDef("# of Maintenance Events", "NumberOfMaintenanceRecords"),
            this.utility.createBooleanColumnDef("Benchmark and Threshold Set?", "BenchmarkAndThresholdSet", { UseCustomDropdownFilter: true }),
            this.utility.createBasicColumnDef("Required Lifespan of Installation", "TreatmentBMPLifespanTypeDisplayName", {
                UseCustomDropdownFilter: true,
                // Legacy GridSpec: `x.TreatmentBMPLifespanTypeDisplayName ?? "Unknown"`. Both
                // the cell render and the filter chip surface "Unknown" for null rows. Skip
                // the fallback for the pinned totals row — it has no real data so we don't
                // want "Unknown" appearing as a phantom value there.
                ValueFormatter: (params) => (params.node?.rowPinned ? "" : params.value || "Unknown"),
                FilterValueGetter: (params: any) => params.data?.TreatmentBMPLifespanTypeDisplayName || "Unknown",
            }),
            this.utility.createDateColumnDef("Lifespan End Date (if Fixed End Date)", "TreatmentBMPLifespanEndDate", "MM/dd/yyyy"),
            this.utility.createBasicColumnDef("Required Field Visits / Year", "RequiredFieldVisitsPerYear"),
            this.utility.createBasicColumnDef("Required Post-Storm Field Visits / Year", "RequiredPostStormFieldVisitsPerYear"),
            this.utility.createBasicColumnDef("Sizing Basis", "SizingBasisTypeDisplayName", { UseCustomDropdownFilter: true }),
            this.utility.createBasicColumnDef("Trash Capture Status", "TrashCaptureStatusTypeDisplayName", { UseCustomDropdownFilter: true }),
            this.utility.createBasicColumnDef("Delineation Type", "DelineationTypeDisplayName", {
                UseCustomDropdownFilter: true,
                // Legacy GridSpec renders `<p class='systemText'>No Delineation Provided</p>`
                // for null DelineationType cells; mirror with a fallback both for display and
                // for the filter chip. Skip the fallback for the pinned totals row.
                ValueFormatter: (params) => (params.node?.rowPinned ? "" : params.value || "No Delineation Provided"),
                FilterValueGetter: (params: any) => params.data?.DelineationTypeDisplayName || "No Delineation Provided",
            }),
        ];

        // Dynamic custom-attribute columns. Each value is read off
        // `params.data.CustomAttributeValues[customAttributeTypeID]`. For numeric data types we
        // run the value through a tolerant parse so the grid can sort + filter as a number; for
        // dates we stringify in the same MM/dd/yyyy format the static columns use. PickFromList
        // / MultiSelect / String fall through to the generic dropdown filter (Yes/No filter
        // chips work automatically because the dropdown reads the formatted text).
        // Exclude Maintenance-purpose attributes — those are captured per maintenance record,
        // not at BMP inventory time, so they're meaningless on a BMP-list grid. Matches the
        // legacy MVC TreatmentBMPsInTreatmentBMPTypeGridSpec which filters them the same way.
        // Sort by Purpose first (Modeling, then OtherDesignAttributes), then by SortOrder
        // within each purpose — matches the legacy's `foreach (purpose) { foreach (attr) }`
        // outer/inner loop so column order is identical.
        const customAttributeTypes = (bmpType.CustomAttributeTypes ?? [])
            .filter((c) => c.CustomAttributeTypePurposeID !== CustomAttributeTypePurposeEnum.Maintenance)
            .slice()
            .sort((a, b) => {
                const purposeDelta = (a.CustomAttributeTypePurposeID ?? 0) - (b.CustomAttributeTypePurposeID ?? 0);
                if (purposeDelta !== 0) return purposeDelta;
                return (a.SortOrder ?? 0) - (b.SortOrder ?? 0);
            });
        for (const cat of customAttributeTypes) {
            const customAttributeTypeID = cat.CustomAttributeTypeID;
            const header = cat.CustomAttributeTypeName + (cat.MeasurementUnitDisplayName && cat.MeasurementUnitDisplayName.toLowerCase() !== "none" ? ` (${cat.MeasurementUnitDisplayName})` : "");
            const raw = (params: ValueGetterParams) => (params.data?.CustomAttributeValues ?? {})[customAttributeTypeID] ?? "";

            switch (cat.CustomAttributeDataTypeID) {
                case CustomAttributeDataTypeEnum.Decimal:
                case CustomAttributeDataTypeEnum.Integer:
                    // Build inline rather than via createDecimalColumnDef: that helper hard-wires
                    // a string-parsing comparator (`decimalComparator` calls `.replace(",", "")`),
                    // which throws when its valueGetter returns a number — so its built-in sort
                    // is incompatible with a numeric ValueGetter override. Inline keeps
                    // valueGetter/comparator/filter all numeric and consistent.
                    cols.push({
                        headerName: header,
                        valueGetter: (params) => {
                            const v = raw(params);
                            if (!v) return null;
                            const n = parseFloat(v);
                            return isNaN(n) ? null : n;
                        },
                        valueFormatter: (params) => {
                            if (params.value == null) return "";
                            const n = params.value as number;
                            return cat.CustomAttributeDataTypeID === CustomAttributeDataTypeEnum.Integer
                                ? n.toLocaleString("en-US")
                                : n.toLocaleString("en-US", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
                        },
                        comparator: (a: number | null, b: number | null) => {
                            if (a == null && b == null) return 0;
                            if (a == null) return -1;
                            if (b == null) return 1;
                            return a - b;
                        },
                        filter: "agNumberColumnFilter",
                        sortable: true,
                        cellStyle: { "justify-content": "flex-end" },
                    });
                    break;
                case CustomAttributeDataTypeEnum.DateTime:
                    // Same story as numeric: build inline so the valueGetter / comparator /
                    // filter are all date-typed and self-consistent (the createDateColumnDef
                    // helper's defaults don't compose with a custom date ValueGetter).
                    cols.push({
                        headerName: header,
                        valueGetter: (params) => {
                            const v = raw(params);
                            if (!v) return null;
                            const d = new Date(v);
                            return isNaN(d.getTime()) ? null : d;
                        },
                        valueFormatter: (params) => {
                            if (!params.value) return "";
                            const d = params.value as Date;
                            return `${(d.getMonth() + 1).toString().padStart(2, "0")}/${d.getDate().toString().padStart(2, "0")}/${d.getFullYear()}`;
                        },
                        comparator: (a: Date | null, b: Date | null) => {
                            if (a == null && b == null) return 0;
                            if (a == null) return -1;
                            if (b == null) return 1;
                            return a.getTime() - b.getTime();
                        },
                        filter: "agDateColumnFilter",
                        sortable: true,
                    });
                    break;
                case CustomAttributeDataTypeEnum.MultiSelect:
                    cols.push(this.utility.createBasicColumnDef(header, customAttributeTypeID.toString(), {
                        UseCustomDropdownFilter: true,
                        ColumnContainsMultipleValues: true,
                        ValueGetter: (params) => {
                            const v = raw(params);
                            return v ? v.split(", ").filter((s: string) => s.length > 0) : [];
                        },
                        ValueFormatter: (params) => Array.isArray(params.value) ? params.value.join(", ") : (params.value ?? ""),
                    }));
                    break;
                case CustomAttributeDataTypeEnum.PickFromList:
                    cols.push(this.utility.createBasicColumnDef(header, customAttributeTypeID.toString(), {
                        UseCustomDropdownFilter: true,
                        ValueGetter: raw,
                    }));
                    break;
                case CustomAttributeDataTypeEnum.String:
                default:
                    cols.push(this.utility.createBasicColumnDef(header, customAttributeTypeID.toString(), {
                        ValueGetter: raw,
                    }));
                    break;
            }
        }

        return cols;
    }

    private deleteBMP(params: any): void {
        // BMP names can contain user-entered markup; escape before interpolating into the
        // confirm message since alerts/dialogs render via [innerHTML]. Same defensive escape
        // pattern as field-records.component.deleteFieldVisit.
        const bmpName = escapeHtml(params.data.TreatmentBMPName ?? "this BMP");
        this.confirmService
            .confirm({
                title: "Delete BMP",
                message: `<p>You are about to delete ${bmpName}.</p><p>Are you sure you wish to proceed?</p>`,
                buttonClassYes: "btn btn-danger",
                buttonTextYes: "Delete",
                buttonTextNo: "Cancel",
            })
            .then((confirmed) => {
                if (!confirmed) return;
                this.bmpService.deleteTreatmentBMP(params.data.TreatmentBMPID).subscribe(() => {
                    this.alertService.pushAlert(new Alert("Successfully deleted BMP.", AlertContext.Success));
                    // Update the subject so both the grid rowData (bound via `| async`) AND
                    // the totals row (derived via combineLatest off bmps$) re-render. A direct
                    // applyTransaction would refresh the grid but leave the pinned totals
                    // computed from pre-delete data.
                    const currentRows = this.bmpsSubject.value;
                    this.bmpsSubject.next(currentRows.filter((r) => r.TreatmentBMPID !== params.data.TreatmentBMPID));
                });
            });
    }
}
