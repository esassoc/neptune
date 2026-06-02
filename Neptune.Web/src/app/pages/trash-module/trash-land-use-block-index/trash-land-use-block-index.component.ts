import { Component } from "@angular/core";
import { PageHeaderComponent } from "../../../shared/components/page-header/page-header.component";
import { AlertDisplayComponent } from "../../../shared/components/alert-display/alert-display.component";
import { Observable, tap } from "rxjs";
import { LandUseBlockService } from "../../../shared/generated/api/land-use-block.service";
import { UtilityFunctionsService } from "../../../services/utility-functions.service";
import { ColDef, RowNode } from "ag-grid-community";
import { AsyncPipe } from "@angular/common";
import { LandUseBlockGridDto } from "../../../shared/generated/model/land-use-block-grid-dto";
import { NeptunePageTypeEnum } from "src/app/shared/generated/enum/neptune-page-type-enum";
import { LoadingDirective } from "src/app/shared/directives/loading.directive";
import { IconComponent } from "../../../shared/components/icon/icon.component";
import { RouterLink } from "@angular/router";
import { AuthenticationService } from "src/app/services/authentication.service";
import { NeptuneMapInitEvent } from "src/app/shared/components/leaflet/neptune-map/neptune-map.component";
import { HybridMapGridComponent } from "src/app/shared/components/hybrid-map-grid/hybrid-map-grid.component";
import { Map, Control } from "leaflet";
import { BoundingBoxDto } from "src/app/shared/generated/model/bounding-box-dto";
import { StormwaterJurisdictionService } from "src/app/shared/generated/api/stormwater-jurisdiction.service";
import { SelectedLandUseBlockLayerComponent } from "src/app/shared/components/leaflet/layers/selected-land-use-block-layer/selected-land-use-block-layer.component";
import { DelineationsLayerComponent } from "src/app/shared/components/leaflet/layers/delineations-layer/delineations-layer.component";
import { JurisdictionsLayerComponent } from "src/app/shared/components/leaflet/layers/jurisdictions-layer/jurisdictions-layer.component";
import { WqmpsLayerComponent } from "src/app/shared/components/leaflet/layers/wqmps-layer/wqmps-layer.component";
import { TrashLandUseBlockAttributesEditModalComponent } from "src/app/shared/components/trash-land-use-blocks/modals/trash-land-use-block-attributes-edit-modal/trash-land-use-block-attributes-edit-modal.component";
import { DialogService } from "@ngneat/dialog";
import { PriorityLandUseTypeEnum } from "src/app/shared/generated/enum/priority-land-use-type-enum";
import { PermitTypeEnum } from "src/app/shared/generated/enum/permit-type-enum";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { AlertService } from "src/app/shared/services/alert.service";
import { OverlayMode } from "src/app/shared/components/leaflet/layers/generic-wms-wfs-layer/overlay-mode.enum";

@Component({
    selector: "trash-land-use-block-index",
    imports: [
        PageHeaderComponent,
        AlertDisplayComponent,
        AsyncPipe,
        LoadingDirective,
        IconComponent,
        HybridMapGridComponent,
        SelectedLandUseBlockLayerComponent,
        JurisdictionsLayerComponent,
        WqmpsLayerComponent,
        DelineationsLayerComponent,
        RouterLink,
    ],
    templateUrl: "./trash-land-use-block-index.component.html",
    styleUrl: "./trash-land-use-block-index.component.scss",
})
export class TrashLandUseBlockIndexComponent {
    public OverlayMode = OverlayMode;
    public landUseBlocks$: Observable<LandUseBlockGridDto[]>;
    public landUseBlockColumnDefs: ColDef[];
    public richTextID = NeptunePageTypeEnum.LandUseBlock;
    public isLoading: boolean = true;
    public landUseBlockID: number;
    /** Stable reference for the layer's [selectedLandUseBlockIDs] @Input. Built inline as
     * `landUseBlockID ? [landUseBlockID] : []` would allocate a new array every change-detection
     * pass and re-trigger ngOnChanges (and the layer's zoom-to-selection). */
    public selectedLandUseBlockIDs: number[] = [];
    public selectionFromMap: boolean;

    public map: Map;
    public layerControl: Control.Layers;
    public bounds: any;
    public mapIsReady: boolean = false;
    public boundingBox$: Observable<BoundingBoxDto>;

    public priorityLandUseTypes = PriorityLandUseTypeEnum;
    public permitTypes = PermitTypeEnum;

    constructor(
        private landUseBlockService: LandUseBlockService,
        private utilityFunctionsService: UtilityFunctionsService,
        private authenticationService: AuthenticationService,
        private stormwaterJurisdictionService: StormwaterJurisdictionService,
        private dialogService: DialogService
    ) {}

    ngOnInit() {
        this.landUseBlockColumnDefs = [
            this.utilityFunctionsService.createActionsColumnDef((params: any) => {
                return [
                    { ActionName: "Update Attributes", ActionIcon: "fas fa-info-circle", ActionHandler: () => this.updateAttributesModal(params.data.LandUseBlockID, params.node) },
                ];
            }),
            this.utilityFunctionsService.createBasicColumnDef("Block ID", "LandUseBlockID"),
            this.utilityFunctionsService.createBasicColumnDef("Land Use Type", "PriorityLandUseTypeName", {
                CustomDropdownFilterField: "PriorityLandUseTypeName",
            }),
            this.utilityFunctionsService.createBasicColumnDef("Jurisdiction", "StormwaterJurisdictionName", {
                CustomDropdownFilterField: "StormwaterJurisdictionName",
            }),
            this.utilityFunctionsService.createDecimalColumnDef("Block Area", "Area"),
            this.utilityFunctionsService.createDecimalColumnDef("Trash Generation Rate", "TrashGenerationRate", {
                CustomDropdownFilterField: "TrashGenerationRate",
            }),
            this.utilityFunctionsService.createBasicColumnDef("Land Use Description", "LandUseDescription", {
                CustomDropdownFilterField: "LandUseDescription",
            }),
            this.utilityFunctionsService.createDecimalColumnDef("Median Household Income Residential", "MedianHouseholdIncomeResidential"),
            this.utilityFunctionsService.createDecimalColumnDef("Median Household Income Retail", "MedianHouseholdIncomeRetail"),
            this.utilityFunctionsService.createDecimalColumnDef("Trash Results Area", "MedianHouseholdIncomeRetail"),
            this.utilityFunctionsService.createBasicColumnDef("Permit Type", "PermitTypeName"),
            this.utilityFunctionsService.createBasicColumnDef("Land Use for TGR", "LandUseForTGR"),
        ];
        this.landUseBlocks$ = this.landUseBlockService.listLandUseBlock().pipe(tap((x) => (this.isLoading = false)));
        this.boundingBox$ = this.stormwaterJurisdictionService.getBoundingBoxStormwaterJurisdiction();
    }

    public currentUserHasJurisdictionEditPermission(): boolean {
        return this.authenticationService.doesCurrentUserHaveJurisdictionEditPermission();
    }

    public handleMapReady(event: NeptuneMapInitEvent) {
        this.map = event.map;
        this.layerControl = event.layerControl;
        this.mapIsReady = true;
    }

    public onSelectedLandUseBlockChangedFromGrid(selectedLandUseBlockID) {
        if (this.landUseBlockID == selectedLandUseBlockID) return;

        this.landUseBlockID = selectedLandUseBlockID;
        this.selectedLandUseBlockIDs = selectedLandUseBlockID ? [selectedLandUseBlockID] : [];
        this.selectionFromMap = false;
        return this.landUseBlockID;
    }

    public onSelectedLandUseBlockChanged(selectedLandUseBlockID) {
        if (this.landUseBlockID == selectedLandUseBlockID) return;

        this.landUseBlockID = selectedLandUseBlockID;
        this.selectedLandUseBlockIDs = selectedLandUseBlockID ? [selectedLandUseBlockID] : [];
        this.selectionFromMap = true;
        return this.landUseBlockID;
    }

    public handleLayerBoundsCalculated(bounds: any) {
        this.bounds = bounds;
    }

    public updateAttributesModal(landUseBlockID: number, rowNode: RowNode) {
        const dialogRef = this.dialogService.open(TrashLandUseBlockAttributesEditModalComponent, {
            data: {
                LandUseBlockID: landUseBlockID,
                PriorityLandUseTypeID: rowNode.data.PriorityLandUseTypeID,
                TrashGenerationRate: rowNode.data.TrashGenerationRate,
                LandUseDescription: rowNode.data.LandUseDescription,
                MedianHouseholdIncomeResidential: rowNode.data.MedianHouseholdIncomeResidential,
                MedianHouseholdIncomeRetail: rowNode.data.MedianHouseholdIncomeRetail,
                PermitTypeID: rowNode.data.PermitTypeID,
            },
            size: "sm",
        });

        dialogRef.afterClosed$.subscribe((result) => {
            if (result) {
                let rowNodeUpdated = rowNode.data;
                rowNodeUpdated.PriorityLandUseTypeID = result.PriorityLandUseTypeID;
                rowNodeUpdated.PriorityLandUseTypeName = this.priorityLandUseTypes[result.PriorityLandUseTypeID];
                rowNodeUpdated.TrashGenerationRate = result.TrashGenerationRate;
                rowNodeUpdated.LandUseDescription = result.LandUseDescription;
                rowNodeUpdated.MedianHouseholdIncomeResidential = result.MedianHouseholdIncomeResidential;
                rowNodeUpdated.MedianHouseholdIncomeRetail = result.MedianHouseholdIncomeRetail;
                rowNodeUpdated.PermitTypeID = result.PermitTypeID;
                rowNodeUpdated.PermitTypeName = this.permitTypes[result.PermitTypeID];
                rowNode.setData(rowNodeUpdated);
            }
        });
    }
}
