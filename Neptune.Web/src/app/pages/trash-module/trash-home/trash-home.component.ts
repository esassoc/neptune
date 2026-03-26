import { Component, DestroyRef, OnInit, inject } from "@angular/core";
import { AuthenticationService } from "src/app/services/authentication.service";
import { environment } from "src/environments/environment";
import { ActivatedRoute, Router, RouterLink } from "@angular/router";
import { PersonDto } from "src/app/shared/generated/model/person-dto";
import { RoleEnum } from "src/app/shared/generated/enum/role-enum";
import { NeptunePageTypeEnum } from "src/app/shared/generated/enum/neptune-page-type-enum";
import { CustomRichTextComponent } from "src/app/shared/components/custom-rich-text/custom-rich-text.component";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { AsyncPipe, DatePipe, DecimalPipe } from "@angular/common";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import {
    BehaviorSubject,
    Observable,
    Subject,
    combineLatest,
    defer,
    distinctUntilChanged,
    filter,
    finalize,
    map,
    scan,
    shareReplay,
    startWith,
    switchMap,
    withLatestFrom,
} from "rxjs";
import { NeptuneMapComponent, NeptuneMapInitEvent } from "../../../shared/components/leaflet/neptune-map/neptune-map.component";
import "leaflet.markercluster";
import * as L from "leaflet";
import { DelineationsLayerComponent } from "src/app/shared/components/leaflet/layers/delineations-layer/delineations-layer.component";
import { JurisdictionsLayerComponent } from "src/app/shared/components/leaflet/layers/jurisdictions-layer/jurisdictions-layer.component";
import { RegionalSubbasinsLayerComponent } from "src/app/shared/components/leaflet/layers/regional-subbasins-layer/regional-subbasins-layer.component";
import { LandUseBlockLayerComponent } from "src/app/shared/components/leaflet/layers/land-use-block-layer/land-use-block-layer.component";
import { TrashGeneratingUnitLayerComponent } from "src/app/shared/components/leaflet/layers/trash-generating-unit-layer/trash-generating-unit-layer.component";
import { NgSelectModule } from "@ng-select/ng-select";
import { FormsModule, ReactiveFormsModule } from "@angular/forms";
import { FieldDefinitionComponent } from "src/app/shared/components/field-definition/field-definition.component";
import { StormwaterJurisdictionService } from "src/app/shared/generated/api/stormwater-jurisdiction.service";
import { AreaBasedAcreCalculationsDto } from "src/app/shared/generated/model/area-based-acre-calculations-dto";
import { LoadResultsDto } from "src/app/shared/generated/model/load-results-dto";
import { OVTAResultsDto } from "src/app/shared/generated/model/ovta-results-dto";
import { LeafletHelperService } from "src/app/shared/services/leaflet-helper.service";
import { BoundingBoxDto } from "src/app/shared/generated/model/bounding-box-dto";
import { IFeature, OnlandVisualTrashAssessmentAreaDetailDto, StormwaterJurisdictionDisplayDto, TrashGeneratingUnitDto } from "src/app/shared/generated/model/models";
import { WqmpsTrashCaptureLayerComponent } from "src/app/shared/components/leaflet/layers/wqmps-trash-capture-layer/wqmps-trash-capture-layer.component";
import { OvtaAreasLayerComponent } from "src/app/shared/components/leaflet/layers/ovta-areas-layer/ovta-areas-layer.component";
import { TrashGeneratingUnitLoadsLayerComponent } from "src/app/shared/components/leaflet/layers/trash-generating-unit-loads-layer/trash-generating-unit-loads-layer.component";
import { TrashGeneratingUnitByStormwaterJurisdictionService } from "src/app/shared/generated/api/trash-generating-unit-by-stormwater-jurisdiction.service";
import { LoadingDirective } from "src/app/shared/directives/loading.directive";
import { WfsService } from "src/app/shared/services/wfs.service";
import { TrashGeneratingUnitService } from "src/app/shared/generated/api/trash-generating-unit.service";
import { ModalService, ModalSizeEnum, ModalThemeEnum } from "src/app/shared/services/modal/modal.service";
import { ScoreDescriptionsComponent } from "../ovtas/score-descriptions/score-descriptions.component";
import { TreatmentBMPService } from "src/app/shared/generated/api/treatment-bmp.service";
import { TrashCaptureStatusTypeEnum } from "src/app/shared/generated/enum/trash-capture-status-type-enum";
import { MarkerHelper } from "src/app/shared/helpers/marker-helper";
import { PermitTypeLayerComponent } from "src/app/shared/components/leaflet/layers/permit-type-layer/permit-type-layer.component";
import { OverlayMode } from "src/app/shared/components/leaflet/layers/generic-wms-wfs-layer/overlay-mode.enum";
import { OnlandVisualTrashAssessmentAreaService } from "src/app/shared/generated/api/onland-visual-trash-assessment-area.service";

@Component({
    selector: "trash-home",
    templateUrl: "./trash-home.component.html",
    styleUrls: ["./trash-home.component.scss"],
    imports: [
        AlertDisplayComponent,
        CustomRichTextComponent,
        AsyncPipe,
        RouterLink,
        NeptuneMapComponent,
        RegionalSubbasinsLayerComponent,
        DelineationsLayerComponent,
        JurisdictionsLayerComponent,
        WqmpsTrashCaptureLayerComponent,
        LandUseBlockLayerComponent,
        PermitTypeLayerComponent,
        TrashGeneratingUnitLayerComponent,
        TrashGeneratingUnitLoadsLayerComponent,
        OvtaAreasLayerComponent,
        NgSelectModule,
        FormsModule,
        ReactiveFormsModule,
        FieldDefinitionComponent,
        DecimalPipe,
        DatePipe,
        LoadingDirective,
    ],
})
export class TrashHomeComponent implements OnInit {
    public OverlayMode = OverlayMode;

    private readonly destroyRef = inject(DestroyRef);
    public currentUser$: Observable<PersonDto>;

    public richTextTypeID: number = NeptunePageTypeEnum.TrashHomePage;

    public map: L.Map;
    public mapIsReady: boolean = false;
    public layerControl: L.Control.Layers;

    public stormwaterJurisdictions$: Observable<Array<StormwaterJurisdictionDisplayDto>>;
    private readonly selectedStormwaterJurisdictionIdSubject = new BehaviorSubject<number | null>(null);

    public currentStormwaterJurisdiction$!: Observable<StormwaterJurisdictionDisplayDto>;
    public selectedStormwaterJurisdictionId$!: Observable<number>;

    public selectedStormwaterJurisdictionLayer: L.GeoJSON<any>;
    private selectedJurisdictionStyle = {
        color: "#FF6C2D",
        weight: 5,
        fill: false,
    };

    public currentResultType: string = "Area-Based Results";
    public resultTypes = [
        "Area-Based Results",
        "Current Net Loading Rate With Controls",
        "Net Change In Trash Loading Rate With Controls",
        "OVTA-Based Results",
        "No Metric, Map Overlay",
    ];

    public areaBasedAcreCalculationsDto$: Observable<AreaBasedAcreCalculationsDto>;
    public loadResultsDto$: Observable<LoadResultsDto>;
    public ovtaResultsDto$: Observable<OVTAResultsDto>;
    public boundingBox$: Observable<BoundingBoxDto>;

    public isLoadingAreaBased$: Observable<boolean>;
    public isLoadingLoadResults$: Observable<boolean>;
    public isLoadingOvtaResults$: Observable<boolean>;

    private readonly loadingDeltaSubject = new Subject<number>();
    public readonly isLoading$ = this.loadingDeltaSubject.pipe(
        startWith(0),
        scan((count, delta) => Math.max(0, count + delta), 0),
        map((count) => count > 0),
        distinctUntilChanged(),
        shareReplay({ bufferSize: 1, refCount: true })
    );

    // Subjects and observables for the selected feature (TGU and OVTA)
    private tguSubject = new BehaviorSubject<TrashGeneratingUnitDto | null>(null);
    public tguDto$: Observable<TrashGeneratingUnitDto | null> = this.tguSubject.asObservable(); //OnlandVisualTrashAssessmentAreaSimpleDto
    public tguLayer: L.GeoJSON<any>;
    private highlightStyle = {
        color: "#FF6C2D",
        weight: 2,
        opacity: 0.65,
        fillOpacity: 0.3,
    };
    private ovtaSubject = new BehaviorSubject<OnlandVisualTrashAssessmentAreaDetailDto | null>(null);
    public ovtaAreaDto$: Observable<OnlandVisualTrashAssessmentAreaDetailDto | null> = this.ovtaSubject.asObservable();

    // Combined observable used by the template for a single async subscription
    public selectedFeature$: Observable<{ tgu: TrashGeneratingUnitDto | null; ovta: OnlandVisualTrashAssessmentAreaDetailDto | null }>;

    public lastUpdateDate$: Observable<string>;

    public treatmentBMPClusterLayer: L.MarkerClusterGroup = new L.MarkerClusterGroup({
        iconCreateFunction: function (cluster) {
            var childCount = cluster.getChildCount();

            return new L.DivIcon({
                html: "<div><span>" + childCount + "</span></div>",
                className: "treatment-bmp-cluster",
                iconSize: new L.Point(40, 40),
            });
        },
    });

    public treatmentBMPs$: Observable<IFeature[]>;

    private readonly mapReadySubject = new BehaviorSubject<NeptuneMapInitEvent | null>(null);
    private readonly mapReady$ = this.mapReadySubject.pipe(
        filter((x): x is NeptuneMapInitEvent => x != null),
        shareReplay({ bufferSize: 1, refCount: true })
    );

    constructor(
        private authenticationService: AuthenticationService,
        private router: Router,
        private route: ActivatedRoute,
        private stormwaterJurisdictionService: StormwaterJurisdictionService,
        private trashResultsByJurisdictionService: TrashGeneratingUnitByStormwaterJurisdictionService,
        private leafletHelperService: LeafletHelperService,
        private wfsService: WfsService,
        private trashGeneratingUnitService: TrashGeneratingUnitService,
        private treatmentBMPService: TreatmentBMPService,
        private onlandVisualTrashAssessmentAreaService: OnlandVisualTrashAssessmentAreaService,
        private modalService: ModalService
    ) {}

    private trackRequest$<T>(source$: Observable<T>): Observable<T> {
        return defer(() => {
            this.loadingDeltaSubject.next(1);
            return source$.pipe(finalize(() => this.loadingDeltaSubject.next(-1)));
        });
    }

    public ngOnInit(): void {
        this.currentUser$ = this.authenticationService.getCurrentUser();

        this.stormwaterJurisdictions$ = this.stormwaterJurisdictionService.listViewableStormwaterJurisdiction().pipe(shareReplay({ bufferSize: 1, refCount: true }));

        this.currentStormwaterJurisdiction$ = combineLatest([this.stormwaterJurisdictions$, this.selectedStormwaterJurisdictionIdSubject]).pipe(
            map(([jurisdictions, selectedId]) => {
                const selected = jurisdictions.find((x) => x.StormwaterJurisdictionID === selectedId);
                return selected ?? jurisdictions[0] ?? null;
            }),
            filter((x): x is StormwaterJurisdictionDisplayDto => x != null),
            shareReplay({ bufferSize: 1, refCount: true })
        );

        this.selectedStormwaterJurisdictionId$ = this.currentStormwaterJurisdiction$.pipe(
            map((x) => x.StormwaterJurisdictionID),
            distinctUntilChanged(),
            shareReplay({ bufferSize: 1, refCount: true })
        );

        this.lastUpdateDate$ = this.trashGeneratingUnitService.getLastUpdateDateTrashGeneratingUnit();

        this.areaBasedAcreCalculationsDto$ = this.currentStormwaterJurisdiction$.pipe(
            switchMap((x) =>
                this.trackRequest$(this.trashResultsByJurisdictionService.getAreaBasedResultsCalculationsTrashGeneratingUnitByStormwaterJurisdiction(x.StormwaterJurisdictionID))
            ),
            shareReplay({ bufferSize: 1, refCount: true })
        );

        this.loadResultsDto$ = this.currentStormwaterJurisdiction$.pipe(
            switchMap((x) =>
                this.trackRequest$(this.trashResultsByJurisdictionService.getLoadBasedResultsCalculationsTrashGeneratingUnitByStormwaterJurisdiction(x.StormwaterJurisdictionID))
            ),
            shareReplay({ bufferSize: 1, refCount: true })
        );

        this.ovtaResultsDto$ = this.currentStormwaterJurisdiction$.pipe(
            switchMap((x) =>
                this.trackRequest$(this.trashResultsByJurisdictionService.getOVTABasedResultsCalculationsTrashGeneratingUnitByStormwaterJurisdiction(x.StormwaterJurisdictionID))
            ),
            shareReplay({ bufferSize: 1, refCount: true })
        );

        this.boundingBox$ = this.currentStormwaterJurisdiction$.pipe(
            switchMap((x) => this.trackRequest$(this.stormwaterJurisdictionService.getBoundingBoxByJurisdictionIDStormwaterJurisdiction(x.StormwaterJurisdictionID))),
            shareReplay({ bufferSize: 1, refCount: true })
        );

        // Per-section loading: true until the corresponding data observable emits,
        // OR when a jurisdiction-change HTTP request is in flight (tracked by isLoading$).
        this.isLoadingAreaBased$ = combineLatest([
            this.areaBasedAcreCalculationsDto$.pipe(map(() => false), startWith(true)),
            this.isLoading$,
        ]).pipe(map(([waiting, loading]) => waiting || loading));
        this.isLoadingLoadResults$ = combineLatest([
            this.loadResultsDto$.pipe(map(() => false), startWith(true)),
            this.isLoading$,
        ]).pipe(map(([waiting, loading]) => waiting || loading));
        this.isLoadingOvtaResults$ = combineLatest([
            this.ovtaResultsDto$.pipe(map(() => false), startWith(true)),
            this.isLoading$,
        ]).pipe(map(([waiting, loading]) => waiting || loading));

        this.treatmentBMPs$ = this.currentStormwaterJurisdiction$.pipe(
            switchMap((x) =>
                this.trackRequest$(this.treatmentBMPService.listInventoryVerifiedTreatmentBMPsByJurisdictionIDAsFeatureCollectionTreatmentBMP(x.StormwaterJurisdictionID))
            ),
            shareReplay({ bufferSize: 1, refCount: true })
        );

        // combined selected feature stream (tgu + ovta)
        this.selectedFeature$ = combineLatest([this.tguDto$, this.ovtaAreaDto$]).pipe(map(([tgu, ovta]) => ({ tgu, ovta })));

        // Map side effects that depend on both the map instance and selected jurisdiction/bounding box.
        combineLatest([this.mapReady$, this.currentStormwaterJurisdiction$])
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(([, jurisdiction]) => {
                this.addSelectedJurisdictionLayer(jurisdiction.StormwaterJurisdictionID);
            });

        combineLatest([this.mapReady$, this.boundingBox$])
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(([, boundingBox]) => {
                this.leafletHelperService.fitMapToBoundingBox(this.map, boundingBox);
            });

        combineLatest([this.mapReady$, this.treatmentBMPs$])
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(([, treatmentBMPs]) => {
                this.updateTreatmentBMPClusterLayer(treatmentBMPs);
            });
    }

    private updateTreatmentBMPClusterLayer(treatmentBMPs: any): void {
        if (!this.map || !this.layerControl) {
            return;
        }

        const isCurrentlyOn = this.map.hasLayer(this.treatmentBMPClusterLayer);

        if (this.treatmentBMPClusterLayer) {
            this.treatmentBMPClusterLayer.clearLayers();
            this.map.removeLayer(this.treatmentBMPClusterLayer);
            this.layerControl.removeLayer(this.treatmentBMPClusterLayer);
        }

        const inventoriedTreatmentBMPsLayer = new L.GeoJSON(treatmentBMPs as any, {
            pointToLayer: (feature, latlng) => {
                let iconSrc = "./assets/main/map-icons/marker-icon-orange.png";
                switch (feature.properties["TrashCaptureStatusTypeID"]) {
                    case TrashCaptureStatusTypeEnum.Full:
                        iconSrc = "./assets/main/map-icons/marker-icon-red.png";
                        break;
                    case TrashCaptureStatusTypeEnum.Partial:
                        iconSrc = "./assets/main/map-icons/marker-icon-blue.png";
                        break;
                    case TrashCaptureStatusTypeEnum.None:
                        iconSrc = "./assets/main/map-icons/marker-icon-black.png";
                        break;
                    case TrashCaptureStatusTypeEnum.NotProvided:
                        iconSrc = "./assets/main/map-icons/marker-icon-gray.png";
                        break;
                    default:
                        iconSrc = "./assets/main/map-icons/marker-icon-orange.png";
                        break;
                }
                return L.marker(latlng, { icon: MarkerHelper.buildDefaultLeafletMarkerFromMarkerPath(iconSrc) });
            },
            onEachFeature: (feature, layer) => {
                layer.bindPopup(
                    `<b>Name:</b> <a target="_blank" href="${this.ocstBaseUrl()}/TreatmentBMP/Detail/${feature.properties.TreatmentBMPID}">${
                        feature.properties.TreatmentBMPName
                    }</a><br>` + `<b>Type:</b> ${feature.properties.TreatmentBMPTypeName}`
                );
            },
        });

        this.treatmentBMPClusterLayer.addLayer(inventoriedTreatmentBMPsLayer);
        this.treatmentBMPClusterLayer["legendHtml"] = "<img src='/assets/main/map-legend-images/bmpTrashCaptureLegend.png' />";
        this.layerControl.addOverlay(this.treatmentBMPClusterLayer, "BMPs");
        if (isCurrentlyOn) {
            this.map.addLayer(this.treatmentBMPClusterLayer);
        }
    }

    public handleMapReady(event: NeptuneMapInitEvent): void {
        this.map = event.map;
        this.layerControl = event.layerControl;
        this.mapIsReady = true;

        this.mapReadySubject.next(event);

        this.registerClickEvents();
    }

    public onJurisdictionSelected(selectedJurisdiction: number | StormwaterJurisdictionDisplayDto | null) {
        const selectedId = typeof selectedJurisdiction === "number" ? selectedJurisdiction : selectedJurisdiction?.StormwaterJurisdictionID;
        if (selectedId == null) {
            return;
        }

        this.selectedStormwaterJurisdictionIdSubject.next(selectedId);

        // Clear selected feature state on jurisdiction change.
        this.tguSubject.next(null);
        this.ovtaSubject.next(null);
        if (this.tguLayer && this.map) {
            this.map.removeLayer(this.tguLayer);
        }
    }

    public registerClickEvents(): void {
        const wfsService = this.wfsService;
        const self = this;
        this.map.on("click", (event: L.LeafletMouseEvent): void => {
            wfsService.getTrashGeneratingUnitByCoordinate(event.latlng.lng, event.latlng.lat).subscribe((tguFeatureCollection: GeoJSON.FeatureCollection) => {
                if (tguFeatureCollection.features.length == 0) {
                    this.tguSubject.next(null);
                    if (this.tguLayer) {
                        this.map.removeLayer(this.tguLayer);
                    }
                }
                var featuresInRenderedOrder = tguFeatureCollection.features
                    .sort((a, b) => {
                        if (a.properties.IsPriorityLandUse > b.properties.IsPriorityLandUse) {
                            return 1;
                        }
                        if (b.properties.IsPriorityLandUse > a.properties.IsPriorityLandUse) {
                            return -1;
                        }
                        return 0;
                    })
                    .sort((a, b) => {
                        // sort by AssessmentScore descending
                        if (a.properties.AssessmentScore < b.properties.AssessmentScore) {
                            return 1;
                        }
                        if (b.properties.AssessmentScore < a.properties.AssessmentScore) {
                            return -1;
                        }
                        return 0;
                    })
                    .sort((a, b) => {
                        // sort by TrashCaptureStatusSortOrder descending
                        if (a.properties.TrashCaptureStatusSortOrder < b.properties.TrashCaptureStatusSortOrder) {
                            return 1;
                        }
                        if (b.properties.TrashCaptureStatusSortOrder < a.properties.TrashCaptureStatusSortOrder) {
                            return -1;
                        }
                        return 0;
                    });

                featuresInRenderedOrder.forEach((feature: GeoJSON.Feature) => {
                    // load the selected TGU and push into the subject so selectedFeature$ updates
                    this.trashGeneratingUnitService.getTrashGeneratingUnit(feature.properties.TrashGeneratingUnitID).subscribe((dto) => {
                        this.tguSubject.next(dto);
                    });
                    const geoJson = L.geoJSON(feature, {
                        style: this.highlightStyle,
                    });
                    if (this.tguLayer) {
                        this.map.removeLayer(this.tguLayer);
                    }
                    this.tguLayer = L.geoJSON(feature, {
                        style: this.highlightStyle,
                    });
                    //this.map.fitBounds(this.tguLayer.getBounds());
                    this.tguLayer.addTo(this.map);
                });
            });
            wfsService.getOVTAAreaByCoordinate(event.latlng.lng, event.latlng.lat).subscribe((ovtaAreaFeatureCollection: GeoJSON.FeatureCollection) => {
                if (ovtaAreaFeatureCollection.features.length == 0) {
                    // if there is more than one OVTA area, we can't decide which one to show
                    this.ovtaSubject.next(null);
                } else {
                    this.onlandVisualTrashAssessmentAreaService
                        .getOnlandVisualTrashAssessmentArea(ovtaAreaFeatureCollection.features[0].properties.OnlandVisualTrashAssessmentAreaID)
                        .subscribe((dto) => {
                            this.ovtaSubject.next(dto);
                        });
                }
            });
        });
    }

    public addSelectedJurisdictionLayer(stormwaterJurisdictionID: number) {
        this.wfsService
            .getGeoserverWFSLayerWithCQLFilter("OCStormwater:Jurisdictions", `StormwaterJurisdictionID = ${stormwaterJurisdictionID}`, "StormwaterJurisdictionID")
            .subscribe((response) => {
                if (this.map) {
                    if (this.selectedStormwaterJurisdictionLayer) {
                        this.map.removeLayer(this.selectedStormwaterJurisdictionLayer);
                    }
                    this.selectedStormwaterJurisdictionLayer = L.geoJSON(response as any, { style: this.selectedJurisdictionStyle });
                    this.selectedStormwaterJurisdictionLayer.addTo(this.map);
                }
            });
    }

    public userIsUnassigned(currentUser: PersonDto) {
        if (!currentUser) {
            return false; // doesn't exist != unassigned
        }

        return currentUser.RoleID === RoleEnum.Unassigned;
    }

    public userIsOCTAGrantReviewer(currentUser: PersonDto) {
        if (!currentUser) {
            return false;
        }

        return currentUser.IsOCTAGrantReviewer;
    }

    public isUserAnAdministrator(currentUser: PersonDto) {
        return this.authenticationService.isUserAnAdministrator(currentUser);
    }

    public login(): void {
        this.authenticationService.login();
    }

    public signUp(): void {
        this.authenticationService.signUp();
    }

    public requestSupportUrl(): string {
        return `${this.ocstBaseUrl()}/Help/Support`;
    }

    public ocstBaseUrl(): string {
        return environment.ocStormwaterToolsBaseUrl;
    }

    showScoreDefinitions() {
        this.modalService.open(ScoreDescriptionsComponent, null, { CloseOnClickOut: false, TopLayer: false, ModalSize: ModalSizeEnum.Large, ModalTheme: ModalThemeEnum.Light });
    }
}
