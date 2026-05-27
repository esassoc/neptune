import { Component } from "@angular/core";
import { PageHeaderComponent } from "../../../../shared/components/page-header/page-header.component";
import { NeptunePageTypeEnum } from "src/app/shared/generated/enum/neptune-page-type-enum";
import { NeptuneMapComponent, NeptuneMapInitEvent } from "../../../../shared/components/leaflet/neptune-map/neptune-map.component";
import * as L from "leaflet";
import "@geoman-io/leaflet-geoman-free";
import { LandUseBlockLayerComponent } from "../../../../shared/components/leaflet/layers/land-use-block-layer/land-use-block-layer.component";
import { SelectedLandUseBlockLayerComponent } from "../../../../shared/components/leaflet/layers/selected-land-use-block-layer/selected-land-use-block-layer.component";
import { AsyncPipe } from "@angular/common";
import { FormsModule } from "@angular/forms";
import { Router, RouterLink } from "@angular/router";
import { Input } from "@angular/core";
import { Observable, forkJoin } from "rxjs";
import { switchMap, tap } from "rxjs/operators";
import { OnlandVisualTrashAssessmentAreaService } from "src/app/shared/generated/api/onland-visual-trash-assessment-area.service";
import { OnlandVisualTrashAssessmentAreaDetailDto } from "src/app/shared/generated/model/onland-visual-trash-assessment-area-detail-dto";
import { TransectLineLayerComponent } from "../../../../shared/components/leaflet/layers/transect-line-layer/transect-line-layer.component";
import { OnlandVisualTrashAssessmentAreaGeometryDto } from "src/app/shared/generated/model/onland-visual-trash-assessment-area-geometry-dto";
import { OvtaAreaSourceTypeEnum } from "src/app/shared/generated/enum/ovta-area-source-type-enum";
import { BtnGroupRadioInputComponent, IBtnGroupRadioInputOption } from "src/app/shared/components/inputs/btn-group-radio-input/btn-group-radio-input.component";
import { GroupByPipe } from "src/app/shared/pipes/group-by.pipe";
import { WfsService } from "src/app/shared/services/wfs.service";
import { LeafletHelperService } from "src/app/shared/services/leaflet-helper.service";

// NPT-1066: the edit page offers the create workflow's Land Use Block / Parcel source types
// plus the existing Geoman freehand draw. OvtaAreaSourceTypeEnum only covers LUB/Parcel, so
// "Draw" is a local sentinel for the manual-geometry mode.
type EditMode = OvtaAreaSourceTypeEnum | "Draw";

@Component({
    selector: "trash-ovta-area-edit-location",
    imports: [
        PageHeaderComponent,
        NeptuneMapComponent,
        LandUseBlockLayerComponent,
        SelectedLandUseBlockLayerComponent,
        AsyncPipe,
        FormsModule,
        TransectLineLayerComponent,
        RouterLink,
        BtnGroupRadioInputComponent,
    ],
    templateUrl: "./trash-ovta-area-edit-location.component.html",
    styleUrl: "./trash-ovta-area-edit-location.component.scss",
})
export class TrashOvtaAreaEditLocationComponent {
    public customRichTextTypeID = NeptunePageTypeEnum.EditOVTAArea;
    public OvtaAreaSourceTypeEnum = OvtaAreaSourceTypeEnum;

    public onlandVisualTrashAssessmentArea$: Observable<OnlandVisualTrashAssessmentAreaDetailDto>;
    public mapHeight = window.innerHeight - window.innerHeight * 0.4 + "px";
    public map: L.Map;
    public layerControl: L.Control.Layers;
    public legendControl: L.Control;
    public mapIsReady = false;
    public bounds: any;

    public selectedParcelIDs: number[] = [];
    public selectedLandUseBlockIDs: number[] = [];

    // NPT-1066: default to Draw so opening the page shows the existing geometry, editable —
    // preserving the prior behavior. The user opts into LUB/Parcel to rebuild from a selection.
    public mode: EditMode = "Draw";
    public sourceTypeOptions: IBtnGroupRadioInputOption[] = [];
    public jurisdictionHasLandUseBlocks = false;
    public stormwaterJurisdictionID?: number;

    // Captured from the loaded detail DTO so mode-switching can rebuild layers without re-fetching.
    private ovtaAreaID!: number;
    private boundingBox: any;
    private geometryJson: string;

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
        this.onlandVisualTrashAssessmentArea$ = this.onlandVisualTrashAssessmentAreaService.getOnlandVisualTrashAssessmentArea(this.onlandVisualTrashAssessmentAreaID).pipe(
            tap((dto) => {
                this.ovtaAreaID = dto.OnlandVisualTrashAssessmentAreaID;
                this.boundingBox = dto.BoundingBox;
                this.geometryJson = dto.Geometry;
                this.stormwaterJurisdictionID = dto.StormwaterJurisdictionID;
                this.jurisdictionHasLandUseBlocks = dto.JurisdictionHasLandUseBlocks;
                // Mirrors the create workflow's toggle; the Land Use Blocks option is disabled
                // when the jurisdiction has none. "Draw" keeps the existing Geoman freehand option.
                this.sourceTypeOptions = [
                    { label: "Land Use Blocks", value: OvtaAreaSourceTypeEnum.LandUseBlock, disabled: !dto.JurisdictionHasLandUseBlocks },
                    { label: "Parcels", value: OvtaAreaSourceTypeEnum.Parcel },
                    { label: "Draw", value: "Draw" },
                ];
            })
        );
    }

    public handleLayerBoundsCalculated(bounds: any) {
        this.bounds = bounds;
    }

    public setSelectedParcels(event) {
        this.selectedParcelIDs = event;
    }

    public save(ovtaAreaID) {
        const ovtaGeometryDto = new OnlandVisualTrashAssessmentAreaGeometryDto();
        ovtaGeometryDto.OnlandVisualTrashAssessmentAreaID = ovtaAreaID;
        if (this.mode === OvtaAreaSourceTypeEnum.LandUseBlock) {
            ovtaGeometryDto.OvtaAreaSourceTypeID = OvtaAreaSourceTypeEnum.LandUseBlock;
            ovtaGeometryDto.SelectedLandUseBlockIDs = this.selectedLandUseBlockIDs;
        } else if (this.mode === OvtaAreaSourceTypeEnum.Parcel) {
            ovtaGeometryDto.OvtaAreaSourceTypeID = OvtaAreaSourceTypeEnum.Parcel;
            ovtaGeometryDto.ParcelIDs = this.selectedParcelIDs;
        } else {
            // Draw: send the manually drawn geometry; null source type tells the API to use it.
            ovtaGeometryDto.OvtaAreaSourceTypeID = null;
            let geoJson = null;
            this.layer.eachLayer((layer: L.Path & { toGeoJSON: () => GeoJSON.Feature }) => {
                geoJson = layer.toGeoJSON();
            });
            ovtaGeometryDto.GeometryAsGeoJson = geoJson ? JSON.stringify(geoJson) : null;
        }
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

    /** NPT-1066: swap between Land Use Block / Parcel / Draw. Tears down the prior mode's
     *  layers + Geoman controls, then sets up the new mode. LUB mode is rendered by the template's
     *  <selected-land-use-block-layer>; Parcel + Draw populate this.layer as before. */
    public onModeChange(newMode: EditMode) {
        // NB: [(ngModel)] has already written this.mode = newMode by the time ngModelChange fires,
        // so we can't guard on (this.mode === newMode) — that would always early-return and skip
        // the layer setup below. Branch on the newMode argument instead.
        // Clear the inactive selections so we never save a stale mix of sources.
        if (newMode !== OvtaAreaSourceTypeEnum.Parcel) {
            this.selectedParcelIDs = [];
        }
        if (newMode !== OvtaAreaSourceTypeEnum.LandUseBlock) {
            this.selectedLandUseBlockIDs = [];
        }
        this.mode = newMode;

        // The toggle renders as soon as the area loads, which can be before the map fires
        // onMapLoad. ngModel has set this.mode; bail before touching Geoman/map APIs if the map
        // isn't ready yet (handleMapReady sets up the default Draw mode once it is).
        if (!this.map) {
            return;
        }

        this.layer.clearLayers();
        if (this.map.pm.globalEditModeEnabled()) {
            this.map.pm.disableGlobalEditMode();
        }
        this.map.pm.removeControls();

        if (newMode === "Draw") {
            this.addFeatureCollectionToFeatureGroup(JSON.parse(this.geometryJson), this.layer);
            this.addOrRemoveGeomanControl(true);
            if (this.layer.getLayers().length > 0 && !this.map.pm.globalEditModeEnabled()) {
                this.map.pm.toggleGlobalEditMode();
            }
        } else if (newMode === OvtaAreaSourceTypeEnum.Parcel) {
            this.addOVTAAreaToLayer(this.ovtaAreaID);
            this.addParcelsToLayer(this.boundingBox);
        }
        // LandUseBlock: the interactive selected-land-use-block-layer handles its own rendering
        // + click selection via onLandUseBlockClicked.

        if (this.layer.getLayers().length > 0) {
            this.map.fitBounds(this.layer.getBounds());
        }
    }

    /** Toggle membership in the selected LUB list; replace the array (not mutate) so the layer's
     *  ngOnChanges fires and it re-styles. Mirrors the create workflow. */
    public onLandUseBlockClicked(landUseBlockID: number) {
        if (this.selectedLandUseBlockIDs.includes(landUseBlockID)) {
            this.selectedLandUseBlockIDs = this.selectedLandUseBlockIDs.filter((id) => id !== landUseBlockID);
        } else {
            this.selectedLandUseBlockIDs = [...this.selectedLandUseBlockIDs, landUseBlockID];
        }
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
            // NPT-1066: the parcel WFS (whole-area bbox) can be slow; if the user switched away
            // from Parcel mode before it returned, don't re-add parcels onto a now-cleared layer.
            if (this.mode !== OvtaAreaSourceTypeEnum.Parcel) {
                return;
            }
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
                    // NPT-1066 (KE): don't fly the map on each parcel toggle — the user is picking
                    // and the zoom is disorienting.
                }
            }
        });
    }
}
