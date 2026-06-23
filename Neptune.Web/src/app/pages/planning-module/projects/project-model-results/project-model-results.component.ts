import { Component, DestroyRef, Input, OnInit, inject } from "@angular/core";
import { BehaviorSubject, Observable, combineLatest, distinctUntilChanged, filter, forkJoin, map, merge, shareReplay, startWith, switchMap } from "rxjs";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { ProjectService } from "src/app/shared/generated/api/project.service";
import { ProjectNetworkSolveHistoryStatusTypeEnum } from "src/app/shared/generated/enum/project-network-solve-history-status-type-enum";
import { DelineationUpsertDto } from "src/app/shared/generated/model/delineation-upsert-dto";
import { ProjectLoadReducingResultDto } from "src/app/shared/generated/model/project-load-reducing-result-dto";
import { ProjectNetworkSolveHistorySimpleDto } from "src/app/shared/generated/model/project-network-solve-history-simple-dto";
import { TreatmentBMPHRUCharacteristicsSummarySimpleDto } from "src/app/shared/generated/model/treatment-bmphru-characteristics-summary-simple-dto";
import { FormsModule, ReactiveFormsModule, FormControl } from "@angular/forms";
import { TreatmentBMPUpsertDto } from "src/app/shared/generated/model/treatment-bmp-upsert-dto";
import { TreatmentBMPDisplayDto } from "src/app/shared/generated/model/treatment-bmp-display-dto";
import { ModeledBmpPerformanceComponent } from "src/app/shared/components/modeled-bmp-performance/modeled-bmp-performance.component";
import { LandUseTableComponent } from "src/app/shared/components/land-use-table/land-use-table.component";
import { FieldDefinitionComponent } from "src/app/shared/components/field-definition/field-definition.component";
import { AsyncPipe, DecimalPipe } from "@angular/common";
import { LoadingDirective } from "src/app/shared/directives/loading.directive";
import { FormFieldComponent, FormFieldType, FormInputOption } from "src/app/shared/components/forms/form-field/form-field.component";

@Component({
    selector: "project-model-results",
    templateUrl: "./project-model-results.component.html",
    styleUrls: ["./project-model-results.component.scss"],
    imports: [
        FormsModule,
        ReactiveFormsModule,
        DecimalPipe,
        AsyncPipe,
        ModeledBmpPerformanceComponent,
        LandUseTableComponent,
        FieldDefinitionComponent,
        LoadingDirective,
        FormFieldComponent,
    ],
})
export class ProjectModelResultsComponent implements OnInit {
    private readonly destroyRef = inject(DestroyRef);

    public FormFieldType = FormFieldType;

    public isLoading$!: Observable<boolean>;

    private readonly projectIDSubject = new BehaviorSubject<number | null>(null);
    private readonly projectNetworkSolveHistoriesSubject = new BehaviorSubject<ProjectNetworkSolveHistorySimpleDto[] | null>(null);
    private readonly treatmentBMPsSubject = new BehaviorSubject<Array<TreatmentBMPUpsertDto | TreatmentBMPDisplayDto> | null>(null);
    private readonly delineationsSubject = new BehaviorSubject<DelineationUpsertDto[] | null>(null);

    public modelingSelectListOptions: { TreatmentBMPID: number; TreatmentBMPName: string }[] = [];
    public modelingSelectFormInputOptions: FormInputOption[] = [];
    public treatmentBMPIDForSelectedProjectLoadReducingResult: number = -1;
    public treatmentBMPIDControl = new FormControl<number>(-1);
    public projectLoadReducingResults: Array<ProjectLoadReducingResultDto>;
    public selectedProjectLoadReducingResult: ProjectLoadReducingResultDto;
    public treatmentBMPHRUCharacteristicSummaries: Array<TreatmentBMPHRUCharacteristicsSummarySimpleDto>;
    public selectedTreatmentBMPHRUCharacteristicSummaries: Array<TreatmentBMPHRUCharacteristicsSummarySimpleDto>;
    public ProjectNetworkHistoryStatusTypeEnum = ProjectNetworkSolveHistoryStatusTypeEnum;

    private lastLoadedProjectID: number | null = null;

    private _projectID: number;
    @Input("projectID")
    set projectID(value: number) {
        this._projectID = value;
        this.projectIDSubject.next(value);
    }
    get projectID(): number {
        return this._projectID;
    }

    private _projectNetworkSolveHistories: ProjectNetworkSolveHistorySimpleDto[];
    @Input("projectNetworkSolveHistories")
    set projectNetworkSolveHistories(value: ProjectNetworkSolveHistorySimpleDto[]) {
        this._projectNetworkSolveHistories = value;
        this.projectNetworkSolveHistoriesSubject.next(value ?? null);
    }
    get projectNetworkSolveHistories(): ProjectNetworkSolveHistorySimpleDto[] {
        return this._projectNetworkSolveHistories;
    }

    private _treatmentBMPs: Array<TreatmentBMPUpsertDto | TreatmentBMPDisplayDto> = [];
    @Input("treatmentBMPs")
    set treatmentBMPs(value: Array<TreatmentBMPUpsertDto | TreatmentBMPDisplayDto>) {
        this._treatmentBMPs = value ?? [];
        this.treatmentBMPsSubject.next(this._treatmentBMPs);
    }
    get treatmentBMPs(): Array<TreatmentBMPUpsertDto | TreatmentBMPDisplayDto> {
        return this._treatmentBMPs;
    }

    private _delineations: DelineationUpsertDto[] = [];
    @Input("delineations")
    set delineations(value: DelineationUpsertDto[]) {
        this._delineations = value ?? [];
        this.delineationsSubject.next(this._delineations);
    }
    get delineations(): DelineationUpsertDto[] {
        return this._delineations;
    }

    constructor(private projectService: ProjectService) {}

    ngOnInit(): void {
        const projectID$ = this.projectIDSubject.asObservable().pipe(
            filter((id): id is number => id != null && Number.isFinite(id)),
            distinctUntilChanged()
        );

        const selectedTreatmentBMPID$ = this.treatmentBMPIDControl.valueChanges.pipe(
            startWith(this.treatmentBMPIDControl.value ?? -1),
            map((value) => (typeof value === "string" ? parseInt(value, 10) : value)),
            distinctUntilChanged()
        );

        // Load modeled results whenever we have a project — independent of network-solve history status.
        // Previously this was gated on a Succeeded ProjectNetworkSolveHistory, which left fully-parameterized
        // projects that already have results (e.g. from a prior/total solve) stuck on a spinner with no Succeeded
        // history row. The endpoint returns [] when no results exist, so the empty-state handles that case.
        const shouldLoadProjectID$ = projectID$.pipe(distinctUntilChanged(), shareReplay(1));

        const load$ = shouldLoadProjectID$.pipe(
            switchMap((projectID) =>
                forkJoin({
                    projectID: [projectID] as const,
                    modeledResults: this.projectService.listLoadReducingResultsForProjectProject(projectID),
                    treatmentBMPHRUCharacteristicSummaries: this.projectService.listTreatmentBMPHRUCharacteristicsForProjectProject(projectID),
                })
            ),
            shareReplay(1)
        );

        // Show spinner while waiting for modeled results to come back for the current project.
        this.isLoading$ = merge(shouldLoadProjectID$.pipe(map(() => true)), load$.pipe(map(() => false))).pipe(startWith(false), distinctUntilChanged(), shareReplay(1));

        // Single subscription: keeps component state synchronized with both
        // (a) incoming modeled results and (b) selection changes.
        combineLatest([load$.pipe(startWith(null as any)), selectedTreatmentBMPID$])
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(([loadResult, selectedTreatmentBMPID]) => {
                this.treatmentBMPIDForSelectedProjectLoadReducingResult = selectedTreatmentBMPID;

                if (loadResult) {
                    const loadedProjectID = loadResult.projectID[0];
                    if (this.lastLoadedProjectID !== loadedProjectID) {
                        this.lastLoadedProjectID = loadedProjectID;
                        this.projectLoadReducingResults = loadResult.modeledResults ?? [];
                        this.treatmentBMPHRUCharacteristicSummaries = loadResult.treatmentBMPHRUCharacteristicSummaries ?? [];
                        this.populateModeledResultsOptions();

                        // If nothing selected yet, default to "All".
                        if (this.treatmentBMPIDControl.value == null) {
                            this.treatmentBMPIDControl.setValue(-1);
                        }
                    }
                }

                this.updateSelectedProjectLoadReducingResult();
            });
    }

    populateModeledResultsOptions() {
        const tempOptions: { TreatmentBMPID: number; TreatmentBMPName: string }[] = [];
        tempOptions.push({ TreatmentBMPID: -1, TreatmentBMPName: "All Treatment BMPs" });

        (this.projectLoadReducingResults ?? []).forEach((x) => {
            const treatmentBMP = (this.treatmentBMPs ?? []).find((y) => y.TreatmentBMPID == x.TreatmentBMPID);
            if (!treatmentBMP?.TreatmentBMPID || !treatmentBMP?.TreatmentBMPName) {
                return;
            }
            tempOptions.push({ TreatmentBMPID: treatmentBMP.TreatmentBMPID, TreatmentBMPName: treatmentBMP.TreatmentBMPName });
        });

        this.modelingSelectListOptions = tempOptions;

        this.modelingSelectFormInputOptions = tempOptions.map((x) => ({
            Value: x.TreatmentBMPID,
            Label: x.TreatmentBMPName,
            disabled: false,
        }));
    }

    updateSelectedProjectLoadReducingResult() {
        if (!Array.isArray(this.projectLoadReducingResults) || !Array.isArray(this.treatmentBMPHRUCharacteristicSummaries)) {
            return;
        }

        if (this.treatmentBMPIDForSelectedProjectLoadReducingResult > 0) {
            this.selectedProjectLoadReducingResult = this.projectLoadReducingResults.find((x) => x.TreatmentBMPID == this.treatmentBMPIDForSelectedProjectLoadReducingResult);
            this.selectedTreatmentBMPHRUCharacteristicSummaries = this.hruCharacteristicsGroupByLandUse(
                this.treatmentBMPHRUCharacteristicSummaries.filter((x) => x.TreatmentBMPID == this.treatmentBMPIDForSelectedProjectLoadReducingResult)
            );
            return;
        }

        // -1 means 'All Treatment BMPs'
        this.selectedProjectLoadReducingResult = new ProjectLoadReducingResultDto();
        //We get the property names of the first one so we have a fully populated object because Typescript doesn't always populate the keys which is VERY annoying
        if (this.projectLoadReducingResults.length > 0) {
            for (let key of Object.getOwnPropertyNames(this.projectLoadReducingResults[0])) {
                this.selectedProjectLoadReducingResult[key] = this.projectLoadReducingResults.reduce((sum, current) => sum + (current[key] ?? 0), 0);
            }
        }

        this.selectedTreatmentBMPHRUCharacteristicSummaries = this.hruCharacteristicsGroupByLandUse([
            ...new Map(this.treatmentBMPHRUCharacteristicSummaries.map((item) => [item["ProjectHRUCharacteristicID"], item])).values(),
        ]);
    }

    private hruCharacteristicsGroupByLandUse(
        distinctHRUCharacteristicSummaries: TreatmentBMPHRUCharacteristicsSummarySimpleDto[]
    ): TreatmentBMPHRUCharacteristicsSummarySimpleDto[] {
        return [
            ...distinctHRUCharacteristicSummaries
                .reduce((r, o) => {
                    const key = o.LandUse;

                    const item =
                        r.get(key) ||
                        Object.assign({}, o, {
                            Area: 0,
                            ImperviousCover: 0,
                        });

                    item.Area += o.Area;
                    item.ImperviousCover += o.ImperviousCover;

                    return r.set(key, item);
                }, new Map())
                .values(),
        ].sort((a, b) => {
            if (a.LandUse > b.LandUse) {
                return 1;
            }
            if (b.LandUse > a.LandUse) {
                return -1;
            }
            return 0;
        });
    }

    getModelResultsLastCalculatedText(): string {
        if (this.projectNetworkSolveHistories == null || this.projectNetworkSolveHistories == undefined || this.projectNetworkSolveHistories.length == 0) {
            return "";
        }

        //These will be ordered by date by the api
        var successfulResults = this.projectNetworkSolveHistories.filter((x) => x.ProjectNetworkSolveHistoryStatusTypeID == ProjectNetworkSolveHistoryStatusTypeEnum.Succeeded);

        if (successfulResults == null || successfulResults.length == 0) {
            return "";
        }

        return `Results last calculated at ${new Date(successfulResults[0].LastUpdated).toLocaleString()}`;
    }

    isMostRecentHistoryOfType(type: ProjectNetworkSolveHistoryStatusTypeEnum): boolean {
        return (
            this.projectNetworkSolveHistories != null &&
            this.projectNetworkSolveHistories.length > 0 &&
            this.projectNetworkSolveHistories[0].ProjectNetworkSolveHistoryStatusTypeID == type
        );
    }

    getNotFullyParameterizedBMPNames(): string[] {
        return (this.treatmentBMPs ?? [])
            .filter((x) => x?.IsFullyParameterized === false)
            .map((x) => x.TreatmentBMPName)
            .filter((name): name is string => !!name);
    }

    getBMPNamesForDelineationsWithDiscrepancies(): string[] {
        if (this.delineations == null || this.delineations.length == 0) {
            return [];
        }

        var treatmentBMPIDsForDelineationsWithDiscrepancies = this.delineations.filter((x) => x.HasDiscrepancies).map((x) => x.TreatmentBMPID);

        if (treatmentBMPIDsForDelineationsWithDiscrepancies == null || treatmentBMPIDsForDelineationsWithDiscrepancies.length == 0) {
            return [];
        }

        return (this.treatmentBMPs ?? [])
            .filter((x) => treatmentBMPIDsForDelineationsWithDiscrepancies.includes(x.TreatmentBMPID))
            .map((x) => x.TreatmentBMPName)
            .filter((name): name is string => !!name);
    }
}
