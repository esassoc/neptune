import { Component } from "@angular/core";
import { PageHeaderComponent } from "../../../../shared/components/page-header/page-header.component";
import { FormControl, FormGroup, FormsModule, ReactiveFormsModule } from "@angular/forms";
import { FormFieldComponent, FormFieldType, FormInputOption } from "src/app/shared/components/forms/form-field/form-field.component";
import { StormwaterJurisdictionService } from "src/app/shared/generated/api/stormwater-jurisdiction.service";
import { BehaviorSubject, Observable, switchMap, tap } from "rxjs";
import { AsyncPipe } from "@angular/common";
import { NeptuneMapComponent, NeptuneMapInitEvent } from "../../../../shared/components/leaflet/neptune-map/neptune-map.component";
import * as L from "leaflet";
import { OnlandVisualTrashAssessmentService } from "src/app/shared/generated/api/onland-visual-trash-assessment.service";
import {
    OnlandVisualTrashAssessmentSimpleDtoForm,
    OnlandVisualTrashAssessmentSimpleDtoFormControls,
} from "src/app/shared/generated/model/onland-visual-trash-assessment-simple-dto";
import { AlertService } from "src/app/shared/services/alert.service";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { OvtaWorkflowProgressService } from "src/app/shared/services/ovta-workflow-progress.service";
import { ActivatedRoute, Router } from "@angular/router";
import { WfsService } from "src/app/shared/services/wfs.service";
import { LandUseBlockLayerComponent } from "../../../../shared/components/leaflet/layers/land-use-block-layer/land-use-block-layer.component";
import { ParcelLayerComponent } from "../../../../shared/components/leaflet/layers/parcel-layer/parcel-layer.component";
import { WorkflowBodyComponent } from "../../../../shared/components/workflow-body/workflow-body.component";
import { AlertDisplayComponent } from "../../../../shared/components/alert-display/alert-display.component";
import { OnlandVisualTrashAssessmentAreaSimpleDto, StormwaterJurisdictionDisplayDto } from "src/app/shared/generated/model/models";
import { OnlandVisualTrashAssessmentAreaService } from "src/app/shared/generated/api/onland-visual-trash-assessment-area.service";
import { NgSelectModule } from "@ng-select/ng-select";

@Component({
    selector: "trash-initiate-ovta",
    imports: [
        PageHeaderComponent,
        ReactiveFormsModule,
        FormsModule,
        FormFieldComponent,
        AsyncPipe,
        NeptuneMapComponent,
        LandUseBlockLayerComponent,
        ParcelLayerComponent,
        WorkflowBodyComponent,
        AlertDisplayComponent,
        NgSelectModule,
    ],
    templateUrl: "./trash-initiate-ovta.component.html",
    styleUrl: "./trash-initiate-ovta.component.scss",
})
export class TrashInitiateOvtaComponent {
    public FormFieldType = FormFieldType;
    public isLoading: boolean = false;

    public map: L.Map;
    public layerControl: L.Control.Layers;
    public mapIsReady = false;
    public isLoadingSubmit = false;
    public layer: L.FeatureGroup = L.featureGroup();

    public selectedOVTAArea: FormControl = new FormControl("");
    public selectedOVTAAreaID: number;
    public selectedOVTAAreaName: string = "";
    // Set when arriving from the homepage Field Actions deep link; the area highlight owns the initial zoom
    private preselectedAreaID: number | null = null;

    public layerIsOnByDefaultOptions: FormInputOption[] = [
        { Value: false, Label: "Reassess existing area", disabled: false },
        { Value: true, Label: "Assess new area", disabled: false },
    ];

    private defaultStyle = {
        color: "blue",
        fillOpacity: 0.2,
        opacity: 0,
    };

    private highlightStyle = {
        color: "#fcfc12",
        weight: 2,
        opacity: 0.65,
        fillOpacity: 0.1,
    };

    public stormwaterJurisdictions$: Observable<StormwaterJurisdictionDisplayDto[]>;
    private stormwaterJurisdictionSubject = new BehaviorSubject<StormwaterJurisdictionDisplayDto | null>(null);
    public stormwaterJurisdiction$ = this.stormwaterJurisdictionSubject.asObservable();
    public onlandVisualTrashAssessmentAreas$: Observable<OnlandVisualTrashAssessmentAreaSimpleDto[]>;

    public formGroup: FormGroup<OnlandVisualTrashAssessmentSimpleDtoForm> = new FormGroup<any>({
        AssessingNewArea: OnlandVisualTrashAssessmentSimpleDtoFormControls.AssessingNewArea(),
        OnlandVisualTrashAssessmentAreaID: OnlandVisualTrashAssessmentSimpleDtoFormControls.OnlandVisualTrashAssessmentAreaID(),
        StormwaterJurisdictionID: OnlandVisualTrashAssessmentSimpleDtoFormControls.StormwaterJurisdictionID(),
    });

    constructor(
        private stormwaterJurisdictionService: StormwaterJurisdictionService,
        private onlandVisualTrashAssessmentAreaService: OnlandVisualTrashAssessmentAreaService,
        private onlandVisualTrashAssessmentService: OnlandVisualTrashAssessmentService,
        private alertService: AlertService,
        private ovtaWorkflowProgressService: OvtaWorkflowProgressService,
        private router: Router,
        private route: ActivatedRoute,
        private wfsService: WfsService
    ) {}

    ngOnInit() {
        this.formGroup.controls.AssessingNewArea.patchValue(false);
        // NPT-1123: homepage Field Actions deep-links here with the nearby assessment area pre-selected
        const queryParams = this.route.snapshot.queryParamMap;
        this.preselectedAreaID = Number(queryParams.get("ovtaAreaID")) || null;
        const preselectedJurisdictionID = Number(queryParams.get("jurisdictionID")) || null;
        this.stormwaterJurisdictions$ = this.stormwaterJurisdictionService.listViewableStormwaterJurisdiction().pipe(
            tap((x) => {
                const defaultJurisdiction = x.find((j) => j.StormwaterJurisdictionID === preselectedJurisdictionID) ?? x[0];
                if (this.preselectedAreaID && defaultJurisdiction.StormwaterJurisdictionID === preselectedJurisdictionID) {
                    this.formGroup.controls.OnlandVisualTrashAssessmentAreaID.patchValue(this.preselectedAreaID);
                } else {
                    this.preselectedAreaID = null;
                }
                this.formGroup.controls.StormwaterJurisdictionID.patchValue(defaultJurisdiction.StormwaterJurisdictionID);
                this.stormwaterJurisdictionSubject.next(defaultJurisdiction);
                this.getStormwaterJurisdictionBounds(defaultJurisdiction.StormwaterJurisdictionID);
            })
        );

        this.onlandVisualTrashAssessmentAreas$ = this.stormwaterJurisdiction$.pipe(
            tap((x) => {
                this.isLoading = true;
                this.addOVTAAreasToLayer(x.StormwaterJurisdictionID);
            }),
            switchMap((x) => {
                return this.onlandVisualTrashAssessmentAreaService.listByJurisdictionIDOnlandVisualTrashAssessmentArea(x.StormwaterJurisdictionID);
            }),
            tap(() => {
                this.isLoading = false;
            })
        );
    }

    public handleMapReady(event: NeptuneMapInitEvent): void {
        this.map = event.map;
        this.layerControl = event.layerControl;
        this.mapIsReady = true;
        this.layer.addTo(this.map);
    }

    public save(andContinue: boolean = false) {
        this.isLoadingSubmit = true;
        this.onlandVisualTrashAssessmentService.createOnlandVisualTrashAssessment(this.formGroup.value).subscribe((response) => {
            this.isLoadingSubmit = false;
            this.alertService.clearAlerts();
            this.alertService.pushAlert(new Alert("Your OVTA was successfully created.", AlertContext.Success));
            this.ovtaWorkflowProgressService.updateProgress(response.OnlandVisualTrashAssessmentID);
            if (andContinue) {
                this.router.navigate([`/trash/onland-visual-trash-assessments/edit/${response.OnlandVisualTrashAssessmentID}/record-observations`]);
            }
        });
    }

    private addOVTAAreasToLayer(jurisdictionID: number) {
        let cql_filter = `StormwaterJurisdictionID = ${jurisdictionID}`;

        this.wfsService
            .getGeoserverWFSLayerWithCQLFilter("OCStormwater:OnlandVisualTrashAssessmentAreas", cql_filter, "OnlandVisualTrashAssessmentAreaID")
            .subscribe((response) => {
                if (response.length == 0) {
                    this.abandonPreselection(jurisdictionID);
                    return;
                }
                this.layer = new L.GeoJSON(response as any, {
                    style: this.defaultStyle,
                    onEachFeature: (feature, layer: L.Path & { feature?: GeoJSON.Feature }) => {
                        layer.on("mouseover", (e) => {
                            layer.setStyle({ fillOpacity: 0.5 });
                        });
                        layer.on("mouseout", (e) => {
                            layer.setStyle({ fillOpacity: 0.1 });
                        });

                        layer.on("click", (e) => {
                            this.formGroup.controls.OnlandVisualTrashAssessmentAreaID.patchValue(feature.properties.OnlandVisualTrashAssessmentAreaID);
                            this.highlightSelectedOVTAArea();
                        });
                    },
                });
                this.layer.addTo(this.map);
                if (this.formGroup.controls.OnlandVisualTrashAssessmentAreaID.value && !this.highlightSelectedOVTAArea()) {
                    this.abandonPreselection(jurisdictionID);
                }
            });
    }

    // The deep-linked area isn't in this jurisdiction's layer, so the highlight will never zoom the map.
    // Drop the preselection and restore the jurisdiction view that getStormwaterJurisdictionBounds suppressed.
    private abandonPreselection(jurisdictionID: number) {
        if (!this.preselectedAreaID) {
            return;
        }
        this.preselectedAreaID = null;
        this.formGroup.controls.OnlandVisualTrashAssessmentAreaID.reset();
        this.getStormwaterJurisdictionBounds(jurisdictionID);
    }

    private getStormwaterJurisdictionBounds(jurisdictionID: number) {
        this.wfsService
            .getGeoserverWFSLayerWithCQLFilter("OCStormwater:Jurisdictions", `StormwaterJurisdictionID = ${jurisdictionID}`, "StormwaterJurisdictionID")
            .subscribe((response) => {
                if (this.preselectedAreaID) {
                    return;
                }
                this.map.fitBounds(L.geoJson(response as any).getBounds());
            });
    }

    public onJurisdictionSelected(event: StormwaterJurisdictionDisplayDto) {
        this.preselectedAreaID = null;
        this.stormwaterJurisdictionSubject.next(event);
        this.getStormwaterJurisdictionBounds(event.StormwaterJurisdictionID);
    }

    public onOVTAAreaDropdownChanged(event: any) {
        this.highlightSelectedOVTAArea();
    }

    // Returns whether a layer matched the selected area, so callers can tell a real highlight from a no-op
    private highlightSelectedOVTAArea(): boolean {
        let matchedArea = false;
        this.layer.eachLayer((layer: L.Polygon) => {
            if (layer.feature.properties.OnlandVisualTrashAssessmentAreaID == this.formGroup.controls.OnlandVisualTrashAssessmentAreaID.value) {
                matchedArea = true;
                layer.setStyle(this.highlightStyle);
                this.map.fitBounds(layer.getBounds());
            } else {
                layer.setStyle(this.defaultStyle);
            }
        });
        return matchedArea;
    }
}
