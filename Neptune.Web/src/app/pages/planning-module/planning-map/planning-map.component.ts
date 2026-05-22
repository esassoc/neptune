import { ApplicationRef, ChangeDetectorRef, Component, OnInit } from "@angular/core";
import { combineLatest, Observable, tap } from "rxjs";
import * as L from "leaflet";
import { BoundingBoxDto, DelineationDto, ProjectDto, TreatmentBMPDisplayDto } from "src/app/shared/generated/model/models";
import { CustomCompileService } from "src/app/shared/services/custom-compile.service";
import { environment } from "src/environments/environment";
import { MarkerHelper } from "src/app/shared/helpers/marker-helper";
import { PrioritizationMetric } from "src/app/shared/models/prioritization-metric";
import { WfsService } from "src/app/shared/services/wfs.service";
import { OctaPrioritizationDetailPopupComponent } from "src/app/pages/planning-module/octa-prioritization-detail-popup/octa-prioritization-detail-popup.component";
import { ColDef, GridApi, GridReadyEvent } from "ag-grid-community";
import { UtilityFunctionsService } from "src/app/services/utility-functions.service";
import { AgGridModule } from "ag-grid-angular";
import { DelineationService } from "src/app/shared/generated/api/delineation.service";
import { ProjectService } from "src/app/shared/generated/api/project.service";
import { StormwaterJurisdictionService } from "src/app/shared/generated/api/stormwater-jurisdiction.service";
import { TreatmentBMPService } from "src/app/shared/generated/api/treatment-bmp.service";
import { NeptunePageTypeEnum } from "src/app/shared/generated/enum/neptune-page-type-enum";
import { RouterLink } from "@angular/router";
import { FieldDefinitionComponent } from "src/app/shared/components/field-definition/field-definition.component";
import { AsyncPipe } from "@angular/common";
import { FormsModule } from "@angular/forms";
import { NgSelectModule } from "@ng-select/ng-select";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { NeptuneGridComponent } from "src/app/shared/components/neptune-grid/neptune-grid.component";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { IconComponent } from "src/app/shared/components/icon/icon.component";
import { ExpandCollapseDirective } from "src/app/shared/directives/expand-collapse.directive";
import { NeptuneMapComponent, NeptuneMapInitEvent } from "src/app/shared/components/leaflet/neptune-map/neptune-map.component";
import { RegionalSubbasinsLayerComponent } from "src/app/shared/components/leaflet/layers/regional-subbasins-layer/regional-subbasins-layer.component";
import { DelineationsLayerComponent } from "src/app/shared/components/leaflet/layers/delineations-layer/delineations-layer.component";
import { JurisdictionsLayerComponent } from "src/app/shared/components/leaflet/layers/jurisdictions-layer/jurisdictions-layer.component";
import { WqmpsLayerComponent } from "src/app/shared/components/leaflet/layers/wqmps-layer/wqmps-layer.component";
import { StormwaterNetworkLayerComponent } from "src/app/shared/components/leaflet/layers/stormwater-network-layer/stormwater-network-layer.component";
import { InventoriedBMPsLayerComponent } from "src/app/shared/components/leaflet/layers/inventoried-bmps-layer/inventoried-bmps-layer.component";
import { OverlayMode } from "src/app/shared/components/leaflet/layers/generic-wms-wfs-layer/overlay-mode.enum";

@Component({
    selector: "planning-map",
    templateUrl: "./planning-map.component.html",
    styleUrls: ["./planning-map.component.scss"],
    imports: [
        AlertDisplayComponent,
        ExpandCollapseDirective,
        NgSelectModule,
        FormsModule,
        FieldDefinitionComponent,
        RouterLink,
        AgGridModule,
        NeptuneGridComponent,
        PageHeaderComponent,
        IconComponent,
        NeptuneMapComponent,
        RegionalSubbasinsLayerComponent,
        DelineationsLayerComponent,
        JurisdictionsLayerComponent,
        WqmpsLayerComponent,
        StormwaterNetworkLayerComponent,
        InventoriedBMPsLayerComponent,
        AsyncPipe,
    ],
})
export class PlanningMapComponent implements OnInit {
    public OverlayMode = OverlayMode;
    public customRichTextTypeID = NeptunePageTypeEnum.HippocampPlanningMap;

    public mapIsReady: boolean = false;
    public projects: Array<ProjectDto>;
    public planningMapInitData$: Observable<PlanningMapInitData>;
    private treatmentBMPs: Array<TreatmentBMPDisplayDto>;
    private delineations: Array<DelineationDto>;
    public selectedTreatmentBMP: TreatmentBMPDisplayDto;
    public relatedTreatmentBMPs: Array<TreatmentBMPDisplayDto>;
    public selectedDelineation: DelineationDto;
    public selectedProject: ProjectDto;
    public gridApi: GridApi;

    public mapHeight = window.innerHeight - window.innerHeight * 0.2 + "px";
    public map: L.Map;
    public plannedProjectTreatmentBMPsLayer: L.GeoJSON<any>;
    public selectedProjectDelineationsLayer: L.GeoJSON<any>;
    public boundingBox$: Observable<BoundingBoxDto>;
    public layerControl: L.Control.Layers;

    //this is needed to allow binding to the static class
    public PrioritizationMetric = PrioritizationMetric;
    public prioritizationMetrics = Object.values(PrioritizationMetric);
    public selectedPrioritizationMetric = PrioritizationMetric.NoMetric;
    public prioritizationMetricOverlayLayer: L.Layer;

    private delineationDefaultStyle = {
        color: "#51F6F8",
        fillOpacity: 0.2,
        opacity: 1,
    };
    private delineationSelectedStyle = {
        color: "yellow",
        fillOpacity: 0.2,
        opacity: 1,
    };
    private plannedTreatmentBMPOverlayName = "Project BMPs";

    public columnDefs: ColDef[];
    public paginationPageSize: number = 100;

    constructor(
        private treatmentBMPService: TreatmentBMPService,
        private delineationService: DelineationService,
        private appRef: ApplicationRef,
        private compileService: CustomCompileService,
        private stormwaterJurisdictionService: StormwaterJurisdictionService,
        private projectService: ProjectService,
        private wfsService: WfsService,
        private utilityFunctionsService: UtilityFunctionsService
    ) {}

    ngOnInit(): void {
        this.boundingBox$ = this.stormwaterJurisdictionService.getBoundingBoxStormwaterJurisdiction();

        this.columnDefs = [
            this.utilityFunctionsService.createLinkColumnDef("Project ID", "ProjectID", "ProjectID", {
                InRouterLink: "/planning/projects/",
            }),
            this.utilityFunctionsService.createLinkColumnDef("Project Name", "ProjectName", "ProjectID", {
                InRouterLink: "/planning/projects/",
            }),
            this.utilityFunctionsService.createLinkColumnDef("Organization", "Organization.OrganizationName", "Organization.OrganizationID", {
                InRouterLink: "/organizations/",
            }),
            this.utilityFunctionsService.createBasicColumnDef("Jurisdiction", "StormwaterJurisdiction.Organization.OrganizationName"),
            this.utilityFunctionsService.createDateColumnDef("Date Created", "DateCreated", "M/d/yyyy", { Width: 120 }),
            this.utilityFunctionsService.createBasicColumnDef("Project Description", "ProjectDescription", { MaxWidth: 250 }),
        ];

        this.compileService.configure(this.appRef);
    }

    public handleMapReady(event: NeptuneMapInitEvent): void {
        this.map = event.map;
        this.layerControl = event.layerControl;
        this.mapIsReady = true;

        this.registerClickEvents();

        this.map.on("overlayadd overlayremove", (e) => {
            if (e["name"] != this.plannedTreatmentBMPOverlayName) {
                return;
            }
        });

        this.planningMapInitData$ = combineLatest({
            Projects: this.projectService.listProject(),
            TreatmentBMPs: this.treatmentBMPService.listPlannedProjectsTreatmentBMP(),
            Delineations: this.delineationService.listDelineation(),
        }).pipe(
            tap((data) => {
                this.projects = data.Projects;
                this.treatmentBMPs = data.TreatmentBMPs;
                this.delineations = data.Delineations;
                this.initializePanes();
                this.addTreatmentBMPLayersToMap();
            })
        );
    }

    public initializePanes(): void {
        let hippocampChoroplethPane = this.map.createPane("hippocampChoroplethPane");
        hippocampChoroplethPane.style.zIndex = "300";
    }

    public registerClickEvents(): void {
        const wfsService = this.wfsService;
        const self = this;
        this.map.on("click", (event: L.LeafletMouseEvent): void => {
            wfsService.getOCTAPrioritizationMetricsByCoordinate(event.latlng.lng, event.latlng.lat).subscribe((octaPrioritizationFeatureCollection: GeoJSON.FeatureCollection) => {
                octaPrioritizationFeatureCollection.features.forEach((feature: GeoJSON.Feature) => {
                    new L.Popup({
                        minWidth: 500,
                        autoPanPadding: new L.Point(100, 100),
                    })
                        .setLatLng(event.latlng)
                        .setContent(
                            this.compileService.compile(OctaPrioritizationDetailPopupComponent, (c) => {
                                c.instance.feature = feature;
                            })
                        )
                        .openOn(self.map);
                });
            });
        });
    }

    public addTreatmentBMPLayersToMap(): void {
        if (this.plannedProjectTreatmentBMPsLayer) {
            this.map.removeLayer(this.plannedProjectTreatmentBMPsLayer);
            this.layerControl.removeLayer(this.plannedProjectTreatmentBMPsLayer);
        }
        // add planned project BMPs layer
        const projectTreatmentBMPGeoJSON = this.mapTreatmentBMPsToGeoJson(this.treatmentBMPs);
        this.plannedProjectTreatmentBMPsLayer = new L.GeoJSON(projectTreatmentBMPGeoJSON as any, {
            pointToLayer: (feature, latlng) => {
                return L.marker(latlng, { icon: MarkerHelper.treatmentBMPMarker });
            },
            onEachFeature: (feature, layer) => {
                layer.on("click", (e) => {
                    this.selectTreatmentBMPImpl(feature.properties.TreatmentBMPID);
                });
            },
        });
        this.plannedProjectTreatmentBMPsLayer["legendHtml"] = "<img src='./assets/main/map-icons/marker-icon-violet.png' style='height:17px'>";
        this.plannedProjectTreatmentBMPsLayer["sortOrder"] = 100;
        this.plannedProjectTreatmentBMPsLayer.addTo(this.map);
        this.layerControl.addOverlay(this.plannedProjectTreatmentBMPsLayer, this.plannedTreatmentBMPOverlayName);
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

    private mapDelineationsToGeoJson(delineations: DelineationDto[]) {
        return delineations.map((x) => JSON.parse(x.Geometry));
    }

    public selectTreatmentBMPImpl(treatmentBMPID: number) {
        if (!this.map.hasLayer(this.plannedProjectTreatmentBMPsLayer)) {
            this.plannedProjectTreatmentBMPsLayer.addTo(this.map);
        }

        let selectedTreatmentBMP = this.treatmentBMPs.find((x) => x.TreatmentBMPID == treatmentBMPID);
        this.selectedTreatmentBMP = selectedTreatmentBMP;
        this.selectProjectImpl(selectedTreatmentBMP.ProjectID);
        this.plannedProjectTreatmentBMPsLayer.eachLayer((layer: L.Marker) => {
            if (!layer.feature.properties.DefaultZIndexOffset) {
                layer.feature.properties.DefaultZIndexOffset = layer.options.zIndexOffset;
            }
            layer.setZIndexOffset(10000);
            layer.setIcon(MarkerHelper.buildDefaultLeafletMarkerFromMarkerPath("/assets/main/map-icons/marker-icon-red.png"));
        });
        this.selectedDelineation = this.delineations.find((x) => x.TreatmentBMPID == treatmentBMPID);
    }

    public selectProjectImpl(projectID: number) {
        if (this.selectedProjectDelineationsLayer) {
            this.map.removeLayer(this.selectedProjectDelineationsLayer);
            this.selectedProjectDelineationsLayer = null;
        }

        this.selectedDelineation = null;

        this.selectedProject = this.projects.find((x) => x.ProjectID == projectID);
        this.relatedTreatmentBMPs = this.treatmentBMPs.filter((x) => x.ProjectID == projectID);
        let relatedTreatmentBMPIDs = this.relatedTreatmentBMPs.map((x) => x.TreatmentBMPID);
        let featureGroupForZoom = L.featureGroup();

        let relatedDelineations = this.delineations.filter((x) => relatedTreatmentBMPIDs.includes(x.TreatmentBMPID));
        if (relatedDelineations != null && relatedDelineations.length > 0) {
            this.selectedProjectDelineationsLayer = new L.GeoJSON(this.mapDelineationsToGeoJson(relatedDelineations) as any, {
                style: (feature) => {
                    if (this.selectedTreatmentBMP == null || this.selectedTreatmentBMP.TreatmentBMPID != feature.properties.TreatmentBMPID) {
                        return this.delineationDefaultStyle;
                    }
                    return this.delineationSelectedStyle;
                },
            });
            this.selectedProjectDelineationsLayer.addTo(this.map);
            this.selectedProjectDelineationsLayer.addTo(featureGroupForZoom);
        }

        this.map.fitBounds(featureGroupForZoom.getBounds(), { padding: new L.Point(50, 50) });
        this.plannedProjectTreatmentBMPsLayer.eachLayer((layer: L.Marker) => {
            layer.setIcon(MarkerHelper.treatmentBMPMarker);
            if (relatedTreatmentBMPIDs.includes(layer.feature.properties.TreatmentBMPID)) {
                layer.addTo(featureGroupForZoom);
            }
        });

        this.gridApi.forEachNode((node) => {
            if (node.data.ProjectID === projectID) {
                node.setSelected(true);
                var rowIndex = node.rowIndex;
                //I am honestly kind of flabbergasted that ag-grid doesn't tell me what page the node is on
                this.gridApi.paginationGoToPage(Math.floor(rowIndex / this.paginationPageSize));
                this.gridApi.ensureIndexVisible(node.rowIndex);
            }
        });
    }

    public applyPrioritizationMetricOverlay(): void {
        if (!this.map) {
            return null;
        }

        if (this.prioritizationMetricOverlayLayer) {
            this.map.removeLayer(this.prioritizationMetricOverlayLayer);
            this.prioritizationMetricOverlayLayer = null;
        }

        if (this.selectedPrioritizationMetric == PrioritizationMetric.NoMetric) {
            return null;
        }

        let prioritizationMetricsWMSOptions = {
            layers: "Neptune:OCTAPrioritization",
            transparent: true,
            format: "image/png",
            tiled: true,
            styles: this.selectedPrioritizationMetric.geoserverStyle,
            pane: "hippocampChoroplethPane",
        } as L.WMSOptions;

        this.prioritizationMetricOverlayLayer = L.tileLayer.wms(environment.geoserverMapServiceUrl + "/wms?", prioritizationMetricsWMSOptions);
        this.prioritizationMetricOverlayLayer.addTo(this.map);
    }

    public onSelectionChanged() {
        const selectedNode = this.gridApi.getSelectedNodes()[0];
        // If we have no selected node or our node has already been selected so we can stop infinite looping
        if (!selectedNode || (this.selectedProject != null && this.selectedProject.ProjectID == selectedNode.data.ProjectID)) {
            return;
        }
        let treatmentBMPID = this.treatmentBMPs.filter((x) => x.ProjectID == selectedNode.data.ProjectID).map((x) => x.TreatmentBMPID)[0];
        if (treatmentBMPID != null) {
            this.selectTreatmentBMPImpl(treatmentBMPID);
            return;
        }

        this.selectProjectImpl(selectedNode.data.ProjectID);
    }

    public getRelatedBMPsToShow() {
        let selectedTreatmentBMPID = this.selectedTreatmentBMP?.TreatmentBMPID;
        return this.relatedTreatmentBMPs.filter((x) => x.TreatmentBMPID != selectedTreatmentBMPID);
    }

    public onGridReady(event: GridReadyEvent) {
        this.gridApi = event.api;
    }
}

export interface PlanningMapInitData {
    TreatmentBMPs: Array<TreatmentBMPDisplayDto>;
    Delineations: Array<DelineationDto>;
    Projects: Array<ProjectDto>;
}
