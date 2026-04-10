import { Component } from "@angular/core";
import { PageHeaderComponent } from "../../../../shared/components/page-header/page-header.component";
import { NeptunePageTypeEnum } from "src/app/shared/generated/enum/neptune-page-type-enum";
import { NeptuneMapComponent, NeptuneMapInitEvent } from "../../../../shared/components/leaflet/neptune-map/neptune-map.component";
import * as L from "leaflet";
import "@geoman-io/leaflet-geoman-free";
import { LandUseBlockLayerComponent } from "../../../../shared/components/leaflet/layers/land-use-block-layer/land-use-block-layer.component";
import { AsyncPipe } from "@angular/common";
import { Router, RouterLink } from "@angular/router";
import { Input } from "@angular/core";
import { Observable, forkJoin } from "rxjs";
import { switchMap, tap } from "rxjs/operators";
import { OnlandVisualTrashAssessmentAreaService } from "src/app/shared/generated/api/onland-visual-trash-assessment-area.service";
import { OnlandVisualTrashAssessmentAreaDetailDto } from "src/app/shared/generated/model/onland-visual-trash-assessment-area-detail-dto";
import { TransectLineLayerComponent } from "../../../../shared/components/leaflet/layers/transect-line-layer/transect-line-layer.component";
import { OnlandVisualTrashAssessmentAreaGeometryDto } from "src/app/shared/generated/model/onland-visual-trash-assessment-area-geometry-dto";
import { GroupByPipe } from "src/app/shared/pipes/group-by.pipe";
import { WfsService } from "src/app/shared/services/wfs.service";
import { LeafletHelperService } from "src/app/shared/services/leaflet-helper.service";

@Component({
    selector: "trash-ovta-area-edit-location",
    imports: [PageHeaderComponent, NeptuneMapComponent, LandUseBlockLayerComponent, AsyncPipe, TransectLineLayerComponent, RouterLink],
    templateUrl: "./trash-ovta-area-edit-location.component.html",
    styleUrl: "./trash-ovta-area-edit-location.component.scss",
})
export class TrashOvtaAreaEditLocationComponent {
    public customRichTextTypeID = NeptunePageTypeEnum.EditOVTAArea;

    public onlandVisualTrashAssessmentArea$: Observable<OnlandVisualTrashAssessmentAreaDetailDto>;
    public mapHeight = window.innerHeight - window.innerHeight * 0.4 + "px";
    public map: L.Map;
    public layerControl: L.Control.Layers;
    public legendControl: L.Control;
    public mapIsReady = false;
    public bounds: any;

    public selectedParcelIDs: number[] = [];

    public canPickParcels: boolean = false;
    public buttonText = "Pick Parcels";

    public layer: L.FeatureGroup = new L.FeatureGroup();

    private highlightStyle = {
        color: "#fcfc12",
        weight: 2,
        opacity: 0.65,
        fillOpacity: 0.1,
    };
    private wqmpStyle = {
        color: "#ff6ba9",
        weight: 2,
        opacity: 0.9,
        fillOpacity: 0.1,
    };
    private noWQMPsStyle = {
        color: "#bbbbbb",
        weight: 1,
        opacity: 0.7,
    };

    @Input() onlandVisualTrashAssessmentAreaID!: number;
    constructor(
        private router: Router,
        private onlandVisualTrashAssessmentAreaService: OnlandVisualTrashAssessmentAreaService,
        private wfsService: WfsService,
        private groupByPipe: GroupByPipe,
        private leafletHelperService: LeafletHelperService
    ) {}

    public handleMapReady(event: NeptuneMapInitEvent, geometry): void {
        this.map = event.map;
        this.layerControl = event.layerControl;
        this.addFeatureCollectionToFeatureGroup(JSON.parse(geometry), this.layer);
        this.setControl();

        this.layer.addTo(this.map);
        this.mapIsReady = true;

        if (this.layer.getLayers().length > 0) {
            this.map.pm.toggleGlobalEditMode();
        }
    }

    public handleLegendControlReady(legendControl: L.Control) {
        this.legendControl = legendControl;
    }

    ngOnInit(): void {
        this.onlandVisualTrashAssessmentArea$ = this.onlandVisualTrashAssessmentAreaService.getOnlandVisualTrashAssessmentArea(this.onlandVisualTrashAssessmentAreaID);
    }

    public handleLayerBoundsCalculated(bounds: any) {
        this.bounds = bounds;
    }

    public setSelectedParcels(event) {
        this.selectedParcelIDs = event;
    }

    public save(ovtaAreaID) {
        var ovtaGeometryDto = new OnlandVisualTrashAssessmentAreaGeometryDto();
        ovtaGeometryDto.UsingParcels = this.canPickParcels;
        let geoJson = null;
        this.layer.eachLayer((layer: L.Path & { toGeoJSON: () => GeoJSON.Feature }) => {
            geoJson = layer.toGeoJSON();
        });
        ovtaGeometryDto.GeometryAsGeoJson = geoJson ? JSON.stringify(geoJson) : null;
        ovtaGeometryDto.ParcelIDs = this.selectedParcelIDs;
        ovtaGeometryDto.OnlandVisualTrashAssessmentAreaID = ovtaAreaID;
        this.onlandVisualTrashAssessmentAreaService.updateOnlandVisualTrashAssessmentWithParcelsOnlandVisualTrashAssessmentArea(ovtaAreaID, ovtaGeometryDto).subscribe((x) => {
            this.router.navigate(`trash/onland-visual-trash-assessment-areas/${ovtaAreaID}`.split("/"));
        });
    }

    public setControl(): void {
        this.map
            .on("pm:create", (event: { shape: string; layer: L.Path & { toGeoJSON: () => GeoJSON.Feature } }) => {
                const layer = event.layer;
                this.layer.clearLayers();
                this.layer.addLayer(layer);
                this.selectFeatureImpl();
            })
            .on("pm:globaleditmodetoggled", (e: any) => {
                if (e.enabled) {
                    //MP 10/2/25 Because direct comparison of layers is proving to be difficult,
                    // just turn off editing for all layers then re-enable only for the layer we want to edit
                    this.map.eachLayer((layer: any) => {
                        if (layer.pm && (this.layer != layer || !this.layer.hasLayer(layer))) {
                            layer.pm.disable();
                        }
                    });
                    // Only enable editing for layers in this.layer
                    this.layer.eachLayer((layer: L.Path) => {
                        layer.pm.enable();
                    });
                    return;
                }
                this.selectFeatureImpl();
            })
            .on("pm:globalremovalmodetoggled", (e: any) => {
                if (e.enabled) {
                    // Remove geometry
                    this.layer.clearLayers();
                    this.map.pm.toggleGlobalRemovalMode();
                    return;
                }
                this.selectFeatureImpl();
            });
        this.addOrRemoveGeomanControl(true);
    }

    public selectFeatureImpl() {
        if (this.isPerformingGeomanAction(true)) {
            return;
        }
        this.map.pm.removeControls();
        this.addOrRemoveGeomanControl(true);
    }

    public isPerformingGeomanAction(skipDrawCheck: boolean = false): boolean {
        //MP 10/1/25 - Added skipDrawCheck because the global draw mode remains enabled momentarily after drawing a shape is complete
        return (this.map?.pm?.globalDrawModeEnabled() && !skipDrawCheck) || this.map?.pm?.globalEditModeEnabled() || this.map?.pm?.globalRemovalModeEnabled();
    }

    public addFeatureCollectionToFeatureGroup(featureJsons: any, featureGroup: L.FeatureGroup) {
        L.geoJson(featureJsons, {
            onEachFeature: (feature, layer) => {
                if (typeof (layer as any).getLayers === "function") {
                    (layer as any).getLayers().forEach((l) => {
                        featureGroup.addLayer(l);
                    });
                } else {
                    featureGroup.addLayer(layer);
                }
                layer.on("click", (e) => {
                    if (!this.map.pm.globalEditModeEnabled()) {
                        this.map.pm.toggleGlobalEditMode();
                    }
                });
            },
        });
    }

    public addOrRemoveGeomanControl(turnOn: boolean) {
        if (turnOn) {
            const hasPolygon = this.layer.getLayers().length > 0;
            this.leafletHelperService.setupGeomanControls(this.map, !hasPolygon, hasPolygon, hasPolygon, "OVTA Area");
            this.leafletHelperService.moveLegendToBottomOfContainer(this.legendControl);
            return;
        }
        this.map.pm.removeControls();
    }

    public setCanPickParcels(ovtaAreaID, boundingBox, geometry) {
        this.canPickParcels = !this.canPickParcels;
        this.layer.clearLayers();
        if (this.canPickParcels) {
            if (this.map.pm.globalEditModeEnabled()) {
                this.map.pm.disableGlobalEditMode();
            }
            this.map.pm.removeControls();
            this.addOVTAAreaToLayer(ovtaAreaID);
            this.addParcelsToLayer(boundingBox);
            this.buttonText = "Draw OVTA Areas";
        } else {
            this.addFeatureCollectionToFeatureGroup(JSON.parse(geometry), this.layer);
            this.addOrRemoveGeomanControl(true);
            this.buttonText = "Pick Parcels";
        }
        const bounds = this.layer.getBounds();
        this.map.fitBounds(bounds);
    }

    public resetZoom() {
        const bounds = this.layer.getBounds();
        this.map.fitBounds(bounds);
    }

    private addOVTAAreaToLayer(ovtaAreaID) {
        this.onlandVisualTrashAssessmentAreaService.getParcelGeometriesOnlandVisualTrashAssessmentArea(ovtaAreaID).pipe(
            switchMap((parcels) => {
                const parcelIDs = parcels.map((x) => x.ParcelID);
                return forkJoin({
                    response: this.wfsService.getGeoserverWFSLayerWithCQLFilter("OCStormwater:Parcels", `ParcelID in (${parcelIDs})`, "ParcelID"),
                }).pipe(
                    tap(({ response }) => {
                        const geoJson = L.geoJSON(response as any, { style: this.highlightStyle });
                        geoJson.addTo(this.layer);
                        this.selectedParcelIDs = parcels.map((x) => x.ParcelID);
                    })
                );
            })
        );
    }

    private addParcelsToLayer(boundingBox) {
        const bbox = boundingBox != null ? `${boundingBox.Bottom},${boundingBox.Right},${boundingBox.Top},${boundingBox.Left}` : null;
        this.wfsService.getGeoserverWFSLayer("OCStormwater:Parcels", "ParcelID", bbox).subscribe((response) => {
            if (response.length == 0) return;
            const featuresGroupedByParcelID = this.groupByPipe.transform(response, "properties.ParcelID");
            Object.keys(featuresGroupedByParcelID).forEach((parcelID) => {
                const geoJson = L.geoJSON(featuresGroupedByParcelID[parcelID], {
                    style: featuresGroupedByParcelID[parcelID][0].properties.WQMPCount > 0 ? this.wqmpStyle : this.noWQMPsStyle,
                });
                geoJson.on("mouseover", (e) => {
                    geoJson.setStyle({ fillOpacity: 0.5 });
                });
                geoJson.on("mouseout", (e) => {
                    geoJson.setStyle({ fillOpacity: 0.1 });
                });

                geoJson.on("click", (e) => {
                    this.onParcelSelected(Number(parcelID));
                });
                geoJson.addTo(this.layer);
            });
        });
    }

    private onParcelSelected(parcelID: number) {
        if (this.selectedParcelIDs.length > 0 && this.selectedParcelIDs.find((x) => x == parcelID)) {
            this.selectedParcelIDs = this.selectedParcelIDs.filter((x) => x != parcelID);
        } else {
            this.selectedParcelIDs.push(parcelID);
        }
        this.highlightSelectedParcel(parcelID);
    }

    private highlightSelectedParcel(parcelID) {
        this.layer.eachLayer((layer) => {
            // skip if well layer
            if (layer instanceof L.Marker) return;

            if (layer instanceof L.GeoJSON) {
                const geoJsonLayers = layer.getLayers() as L.Polygon[];
                if (geoJsonLayers[0].feature.properties.ParcelID == parcelID) {
                    if (geoJsonLayers[0].options.color == this.highlightStyle.color) {
                        layer.setStyle(geoJsonLayers[0].feature.properties.WQMPCount > 0 ? this.wqmpStyle : this.noWQMPsStyle);
                    } else {
                        layer.setStyle(this.highlightStyle);
                    }
                    this.map.fitBounds(layer.getBounds());
                }
            }
        });
    }
}
