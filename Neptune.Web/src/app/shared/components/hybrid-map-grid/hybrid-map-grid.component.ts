import { Component, EventEmitter, Input, Output } from "@angular/core";
import { ColDef, GridApi, GridReadyEvent } from "ag-grid-community";
import { AgGridAngular } from "ag-grid-angular";
import { Map, Control } from "leaflet";
import { LoadingDirective } from "../../directives/loading.directive";
import { IconComponent } from "../icon/icon.component";

import { NeptuneGridHeaderComponent } from "../neptune-grid-header/neptune-grid-header.component";
import { NeptuneGridComponent } from "../neptune-grid/neptune-grid.component";
import { NeptuneMapComponent, NeptuneMapInitEvent } from "../leaflet/neptune-map/neptune-map.component";
import { BoundingBoxDto } from "../../generated/model/bounding-box-dto";

@Component({
    selector: "hybrid-map-grid",
    imports: [LoadingDirective, IconComponent, NeptuneGridHeaderComponent, NeptuneGridComponent, NeptuneMapComponent],
    templateUrl: "./hybrid-map-grid.component.html",
    styleUrl: "./hybrid-map-grid.component.scss",
})
export class HybridMapGridComponent {
    @Input() rowData: any[];
    @Input() columnDefs: ColDef[];
    @Input() downloadFileName: string;
    @Input() selectedValue: number = null;
    @Input() selectionFromMap: boolean;
    @Input() entityIDField: string = "";
    @Input() gridHeight: string = "675px";
    @Input() boundingBox: BoundingBoxDto;

    @Output() onMapLoad: EventEmitter<NeptuneMapInitEvent> = new EventEmitter();
    @Output() selectedValueChange: EventEmitter<number> = new EventEmitter<number>();
    public gridApi: GridApi;
    public gridRef: AgGridAngular;

    public selectedPanel: "Grid" | "Hybrid" | "Map" = "Hybrid";

    public map: Map;
    public layerControl: L.Control.Layers;
    public bounds: any;
    public mapIsReady: boolean = false;

    public isLoading: boolean = true;
    public firstLoad: boolean = true;

    ngOnChanges(changes: any): void {
        if (changes.selectedValue) {
            if (changes.selectedValue.previousValue == changes.selectedValue.currentValue) return;
            this.selectedValue = changes.selectedValue.currentValue;

            // only want to call onMapSelectionChanged if the change to selectedValue originated from the Map.
            // This will find the row in the grid for the selectedValue and scroll it to the top of the grid
            if (changes.selectionFromMap) {
                this.selectionFromMap = changes.selectionFromMap.currentValue;
            }
            if (this.selectionFromMap) {
                this.onMapSelectionChanged(this.selectedValue);
            }
        }
    }

    public toggleSelectedPanel(selectedPanel: "Grid" | "Hybrid" | "Map") {
        this.selectedPanel = selectedPanel;

        // resizing map to fit new container width; timeout needed to ensure new width has registered before running invalidtaeSize()
        setTimeout(() => {
            this.map.invalidateSize(true);

            if (this.layerControl && this.bounds) {
                this.map.fitBounds(this.bounds);
            }
        }, 300);

        // if no map is visible, turn of grid selection
        if (selectedPanel == "Grid") {
            this.gridApi.setGridOption("rowSelection", null);
            this.selectedValue = undefined;
        } else {
            this.gridApi.setGridOption("rowSelection", "single");
        }
    }

    public handleMapReady(event: NeptuneMapInitEvent) {
        this.map = event.map;
        this.mapIsReady = true;

        this.onMapLoad.emit(event);
    }

    public onGridReady(event: GridReadyEvent) {
        this.gridApi = event.api;
    }

    public onGridRefReady(gridRef: AgGridAngular) {
        this.gridRef = gridRef;
    }

    public onGridSelectionChanged() {
        const selectedNodes = this.gridApi.getSelectedNodes();

        this.selectedValue = selectedNodes.length > 0 ? selectedNodes[0].data[this.entityIDField] : null;
        // do not scroll the row for the selected value to the top. Since the selection origniated from clicking the grid, it is already in view
        this.selectedValueChange.emit(this.selectedValue);
    }

    public onMapSelectionChanged(selectedWaterAccountID: number) {
        this.selectedValue = selectedWaterAccountID;

        this.gridApi.forEachNode((node, index) => {
            if (node.data[this.entityIDField] == selectedWaterAccountID) {
                node.setSelected(true, true);
                this.gridApi.ensureIndexVisible(index, "top");
            }
        });
        return this.selectedValue;
    }
}
