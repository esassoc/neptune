import { ApplicationRef, Component, OnDestroy, OnInit } from "@angular/core";
import { ActivatedRoute, Router } from "@angular/router";
import { Input } from "@angular/core";
import { BoundingBoxDto, DelineationUpsertDto, ProjectNetworkSolveHistorySimpleDto, ProjectDto, TreatmentBMPDisplayDto } from "src/app/shared/generated/model/models";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { AlertService } from "src/app/shared/services/alert.service";
import { CustomCompileService } from "src/app/shared/services/custom-compile.service";
import * as L from "leaflet";
import { BehaviorSubject, Observable, Subject, combineLatest, distinctUntilChanged, filter, map, merge, shareReplay, switchMap, takeUntil, tap, withLatestFrom } from "rxjs";
import { environment } from "src/environments/environment";
import { MarkerHelper } from "src/app/shared/helpers/marker-helper";
import { ProjectService } from "src/app/shared/generated/api/project.service";
import { ProjectNetworkSolveHistoryStatusTypeEnum } from "src/app/shared/generated/enum/project-network-solve-history-status-type-enum";
import { NeptunePageTypeEnum } from "src/app/shared/generated/enum/neptune-page-type-enum";
import { GrantScoresComponent } from "src/app/pages/planning-module/projects/grant-scores/grant-scores.component";
import { ProjectModelResultsComponent } from "src/app/pages/planning-module/projects/project-model-results/project-model-results.component";
import { FieldDefinitionComponent } from "src/app/shared/components/field-definition/field-definition.component";
import { AsyncPipe } from "@angular/common";
import { CustomRichTextComponent } from "src/app/shared/components/custom-rich-text/custom-rich-text.component";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { WorkflowBodyComponent } from "src/app/shared/components/workflow-body/workflow-body.component";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { DelineationsLayerComponent } from "src/app/shared/components/leaflet/layers/delineations-layer/delineations-layer.component";
import { JurisdictionsLayerComponent } from "src/app/shared/components/leaflet/layers/jurisdictions-layer/jurisdictions-layer.component";
import { RegionalSubbasinsLayerComponent } from "src/app/shared/components/leaflet/layers/regional-subbasins-layer/regional-subbasins-layer.component";
import { StormwaterNetworkLayerComponent } from "src/app/shared/components/leaflet/layers/stormwater-network-layer/stormwater-network-layer.component";
import { WqmpsLayerComponent } from "src/app/shared/components/leaflet/layers/wqmps-layer/wqmps-layer.component";
import { NeptuneMapComponent, NeptuneMapInitEvent } from "src/app/shared/components/leaflet/neptune-map/neptune-map.component";
import { InventoriedBMPsLayerComponent } from "src/app/shared/components/leaflet/layers/inventoried-bmps-layer/inventoried-bmps-layer.component";
import { OverlayMode } from "src/app/shared/components/leaflet/layers/generic-wms-wfs-layer/overlay-mode.enum";
import { routeParams } from "src/app/app.routes";

@Component({
    selector: "modeled-performance",
    templateUrl: "./modeled-performance.component.html",
    styleUrls: ["./modeled-performance.component.scss"],
    imports: [
        CustomRichTextComponent,
        AsyncPipe,
        FieldDefinitionComponent,
        ProjectModelResultsComponent,
        GrantScoresComponent,
        PageHeaderComponent,
        WorkflowBodyComponent,
        AlertDisplayComponent,
        NeptuneMapComponent,
        RegionalSubbasinsLayerComponent,
        DelineationsLayerComponent,
        JurisdictionsLayerComponent,
        WqmpsLayerComponent,
        StormwaterNetworkLayerComponent,
        InventoriedBMPsLayerComponent,
    ],
})
export class ModeledPerformanceComponent implements OnInit, OnDestroy {
    public OverlayMode = OverlayMode;
    public ProjectNetworkHistoryStatusTypeEnum = ProjectNetworkSolveHistoryStatusTypeEnum;

    private readonly destroyed$ = new Subject<void>();

    private readonly mapReadySubject = new BehaviorSubject<NeptuneMapInitEvent | null>(null);
    public readonly mapReady$ = this.mapReadySubject.pipe(
        filter((x): x is NeptuneMapInitEvent => x != null),
        shareReplay({ bufferSize: 1, refCount: true })
    );
    public readonly mapIsReady$ = this.mapReady$.pipe(
        map(() => true),
        distinctUntilChanged(),
        shareReplay({ bufferSize: 1, refCount: true })
    );

    private readonly refreshSolveHistoriesSubject = new BehaviorSubject<void>(undefined);

    private readonly selectedTreatmentBMPIdSubject = new BehaviorSubject<number | null>(null);
    public readonly selectedTreatmentBMPId$ = this.selectedTreatmentBMPIdSubject.pipe(distinctUntilChanged(), shareReplay({ bufferSize: 1, refCount: true }));

    private readonly mapDataInitializedSubject = new BehaviorSubject<boolean>(false);
    private readonly mapDataInitialized$ = this.mapDataInitializedSubject.pipe(distinctUntilChanged(), shareReplay({ bufferSize: 1, refCount: true }));

    public mapHeight: string = "750px";
    public onEachFeatureCallback?: (feature, layer) => void;
    public map: L.Map;
    public layerControl: L.Control.Layers;
    public treatmentBMPsLayer: L.GeoJSON<any>;
    public delineationsLayer: L.GeoJSON<any>;
    private delineationDefaultStyle = {
        color: "blue",
        fillOpacity: 0.2,
        opacity: 1,
    };
    private delineationSelectedStyle = {
        color: "yellow",
        fillOpacity: 0.2,
        opacity: 1,
    };

    public project$: Observable<ProjectDto>;
    public projectTreatmentBMPs$: Observable<TreatmentBMPDisplayDto[]>;
    public delineations$: Observable<DelineationUpsertDto[]>;
    public delineationAcreageByTreatmentBMPId$: Observable<Map<number, string>>;
    public projectNetworkSolveHistories$: Observable<ProjectNetworkSolveHistorySimpleDto[]>;
    public boundingBox$: Observable<BoundingBoxDto>;
    public selectedTreatmentBMP$: Observable<TreatmentBMPDisplayDto | null>;

    private data$: Observable<{
        treatmentBMPs: TreatmentBMPDisplayDto[];
        delineations: DelineationUpsertDto[];
        projectNetworkSolveHistories: ProjectNetworkSolveHistorySimpleDto[];
    }>;
    public customRichTextTypeID = NeptunePageTypeEnum.HippocampModeledPerformance;

    @Input() projectID!: number;
    constructor(
        private projectService: ProjectService,
        private appRef: ApplicationRef,
        private compileService: CustomCompileService,
        private route: ActivatedRoute,
        private router: Router,
        private alertService: AlertService
    ) {}

    private getProjectIdFromRouteSnapshot(): number | null {
        let current: ActivatedRoute | null = this.route;
        while (current) {
            const raw = current.snapshot?.paramMap?.get(routeParams.projectID);
            if (raw != null) {
                const parsed = Number(raw);
                return Number.isFinite(parsed) ? parsed : null;
            }
            current = current.parent;
        }
        return null;
    }

    ngOnInit(): void {
        // This component is routed under `edit/:projectID/...`, so projectID is usually a route param.
        // Keep @Input() support (e.g. if embedded), but fall back to the route when it's not provided.
        if (this.projectID == null) {
            const routeProjectId = this.getProjectIdFromRouteSnapshot();
            if (routeProjectId != null) {
                this.projectID = routeProjectId;
            }
        }

        this.project$ = this.projectService.getProject(this.projectID).pipe(
            tap((project) => {
                // redirect to review step if project is shared with OCTA grant program
                if (project.ShareOCTAM2Tier2Scores) {
                    this.router.navigateByUrl(`/planning/projects/edit/${project.ProjectID}/review-and-share`);
                }
            }),
            shareReplay({ bufferSize: 1, refCount: true })
        );

        this.data$ = this.project$.pipe(
            switchMap((project) =>
                combineLatest({
                    treatmentBMPs: this.projectService.listTreatmentBMPsByProjectIDProject(project.ProjectID),
                    delineations: this.projectService.listDelineationsByProjectIDProject(project.ProjectID),
                    projectNetworkSolveHistories: this.projectService.listProjectNetworkSolveHistoriesForProjectProject(project.ProjectID),
                })
            ),
            tap((data) => {
                if ((data.treatmentBMPs ?? []).length === 0) {
                    this.router.navigateByUrl(`/planning/projects/edit/${this.projectID}`);
                }
            }),
            shareReplay({ bufferSize: 1, refCount: true })
        );

        this.projectTreatmentBMPs$ = this.data$.pipe(
            map((x) => x.treatmentBMPs ?? []),
            shareReplay({ bufferSize: 1, refCount: true })
        );
        this.delineations$ = this.data$.pipe(
            map((x) => x.delineations ?? []),
            shareReplay({ bufferSize: 1, refCount: true })
        );
        this.delineationAcreageByTreatmentBMPId$ = this.delineations$.pipe(
            map((delineations) => {
                const result = new Map<number, string>();
                for (const delineation of delineations ?? []) {
                    if (delineation?.TreatmentBMPID != null) {
                        result.set(delineation.TreatmentBMPID, delineation.DelineationArea != null ? `${delineation.DelineationArea} ac` : "Not provided");
                    }
                }
                return result;
            }),
            shareReplay({ bufferSize: 1, refCount: true })
        );

        this.projectNetworkSolveHistories$ = merge(
            this.data$.pipe(map((x) => x.projectNetworkSolveHistories ?? [])),
            this.refreshSolveHistoriesSubject.pipe(
                withLatestFrom(this.project$),
                switchMap(([_, project]) => this.projectService.listProjectNetworkSolveHistoriesForProjectProject(project.ProjectID))
            )
        ).pipe(shareReplay({ bufferSize: 1, refCount: true }));

        this.boundingBox$ = this.project$.pipe(
            switchMap((project) => {
                return this.projectService.getBoundingBoxByProjectIDProject(project.ProjectID);
            }),
            shareReplay({ bufferSize: 1, refCount: true })
        );

        this.selectedTreatmentBMP$ = combineLatest([this.projectTreatmentBMPs$, this.selectedTreatmentBMPId$]).pipe(
            map(([bmps, selectedId]) => {
                if (selectedId == null) {
                    return null;
                }
                return bmps.find((x) => x.TreatmentBMPID === selectedId) ?? null;
            }),
            shareReplay({ bufferSize: 1, refCount: true })
        );

        // Leaflet side effects: initialize layers after we have both the map and the data.
        combineLatest([this.mapReady$, this.projectTreatmentBMPs$, this.delineations$])
            .pipe(takeUntil(this.destroyed$))
            .subscribe(([mapReady, treatmentBMPs, delineations]) => {
                if (!this.treatmentBMPsLayer && !this.delineationsLayer) {
                    this.initializeMap(treatmentBMPs, delineations);
                    this.mapDataInitializedSubject.next(true);
                }
            });

        // Keep the map selection styling in sync with the selected BMP.
        combineLatest([this.mapDataInitialized$, this.selectedTreatmentBMPId$])
            .pipe(
                takeUntil(this.destroyed$),
                filter(([isInitialized, id]) => isInitialized && id != null)
            )
            .subscribe(([_, id]) => {
                this.applySelectionToMap(id as number);
            });

        this.compileService.configure(this.appRef);
    }

    public handleMapReady(event: NeptuneMapInitEvent): void {
        // Ensure these imperative fields are set before emitting mapReady$.
        // The template gates child layers on mapIsReady$, and those children need map + layerControl.
        this.map = event.map;
        this.layerControl = event.layerControl;
        this.mapReadySubject.next(event);
    }

    ngOnDestroy(): void {
        this.destroyed$.next();
        this.destroyed$.complete();
    }

    public getAboutModelingPerformanceURL(): string {
        return `${environment.ocStormwaterToolsBaseUrl}/Home/AboutModelingBMPPerformance`;
    }

    public onSelectTreatmentBMP(treatmentBMPID: number): void {
        this.selectedTreatmentBMPIdSubject.next(treatmentBMPID);
    }

    private initializeMap(treatmentBMPs: TreatmentBMPDisplayDto[], delineations: DelineationUpsertDto[]): void {
        const delineationGeoJson = this.mapDelineationsToGeoJson(delineations);
        this.delineationsLayer = new L.GeoJSON(delineationGeoJson as any, {
            onEachFeature: (feature, layer: L.Path & { feature?: GeoJSON.Feature }) => {
                layer.setStyle(this.delineationDefaultStyle);
                layer.on("click", (e) => {
                    this.onSelectTreatmentBMP(feature.properties.TreatmentBMPID);
                });
            },
        });
        this.delineationsLayer.addTo(this.map);

        const treatmentBMPsGeoJson = this.mapTreatmentBMPsToGeoJson(treatmentBMPs);
        this.treatmentBMPsLayer = new L.GeoJSON(treatmentBMPsGeoJson as any, {
            pointToLayer: (feature, latlng) => {
                return L.marker(latlng, { icon: MarkerHelper.treatmentBMPMarker });
            },
            onEachFeature: (feature, layer) => {
                layer.on("click", (e) => {
                    this.onSelectTreatmentBMP(feature.properties.TreatmentBMPID);
                });
            },
        });
        this.treatmentBMPsLayer.addTo(this.map);

        let tempFeatureGroup = new L.FeatureGroup([this.treatmentBMPsLayer, this.delineationsLayer]);
        this.map.fitBounds(tempFeatureGroup.getBounds(), { padding: new L.Point(50, 50) });
    }

    private mapTreatmentBMPsToGeoJson(treatmentBMPs: TreatmentBMPDisplayDto[]) {
        return {
            type: "FeatureCollection",
            features: treatmentBMPs.map((x) => {
                let treatmentBMPGeoJson = {
                    type: "Feature",
                    geometry: {
                        type: "Point",
                        coordinates: [x.Longitude ?? 0, x.Latitude ?? 0],
                    },
                    properties: {
                        TreatmentBMPID: x.TreatmentBMPID,
                        TreatmentBMPName: x.TreatmentBMPName,
                        TreatmentBMPTypeName: x.TreatmentBMPTypeName,
                        Latitude: x.Latitude,
                        Longitude: x.Longitude,
                    },
                };
                return treatmentBMPGeoJson;
            }),
        };
    }

    private mapDelineationsToGeoJson(delineations: DelineationUpsertDto[]) {
        return delineations.map((x) => JSON.parse(x.Geometry));
    }

    private applySelectionToMap(treatmentBMPID: number): void {
        if (!this.map || !this.treatmentBMPsLayer || !this.delineationsLayer) {
            return;
        }

        let hasFlownToSelectedObject = false;
        this.delineationsLayer?.eachLayer((layer: L.Polygon) => {
            if (treatmentBMPID == null || treatmentBMPID != layer.feature.properties.TreatmentBMPID) {
                layer.setStyle(this.delineationDefaultStyle);
                return;
            }
            layer.setStyle(this.delineationSelectedStyle);
            this.map.flyToBounds(layer.getBounds(), { padding: new L.Point(50, 50) });
            hasFlownToSelectedObject = true;
        });
        this.treatmentBMPsLayer.eachLayer((layer: L.Marker) => {
            if (treatmentBMPID == null || treatmentBMPID != layer.feature.properties.TreatmentBMPID) {
                layer.setIcon(MarkerHelper.treatmentBMPMarker).setZIndexOffset(1000);
                return;
            }
            layer.setIcon(MarkerHelper.selectedMarker);
            layer.setZIndexOffset(10000);
            if (!hasFlownToSelectedObject) {
                this.map.flyTo(layer.getLatLng(), 18);
            }
        });
    }

    triggerModelRunForProject(): void {
        this.projectService.triggerModeledPerformanceForProjectProject(this.projectID).subscribe({
            next: () => {
                this.alertService.pushAlert(new Alert("Model run was successfully started and will run in the background.", AlertContext.Success, true));
                window.scroll(0, 0);
                this.refreshSolveHistoriesSubject.next();
            },
            error: () => {
                this.alertService.pushAlert(new Alert("Unable to start model run. Please try again.", AlertContext.Danger, true));
                window.scroll(0, 0);
            },
        });
    }

    getModelResultsLastCalculatedText(projectNetworkSolveHistories: ProjectNetworkSolveHistorySimpleDto[]): string {
        if (projectNetworkSolveHistories == null || projectNetworkSolveHistories == undefined || projectNetworkSolveHistories.length == 0) {
            return "";
        }

        //These will be ordered by date by the api
        var successfulResults = projectNetworkSolveHistories.filter((x) => x.ProjectNetworkSolveHistoryStatusTypeID == ProjectNetworkSolveHistoryStatusTypeEnum.Succeeded);

        if (successfulResults == null || successfulResults.length == 0) {
            return "";
        }

        return `Results last calculated at ${new Date(successfulResults[0].LastUpdated).toLocaleString()}`;
    }

    isMostRecentHistoryOfType(projectNetworkSolveHistories: ProjectNetworkSolveHistorySimpleDto[], type: ProjectNetworkSolveHistoryStatusTypeEnum): boolean {
        return projectNetworkSolveHistories != null && projectNetworkSolveHistories.length > 0 && projectNetworkSolveHistories[0].ProjectNetworkSolveHistoryStatusTypeID == type;
    }

    continueToNextStep() {
        this.router.navigateByUrl(`/planning/projects/edit/${this.projectID}/attachments`);
    }
}
