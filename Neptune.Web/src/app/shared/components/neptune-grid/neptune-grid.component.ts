import { Component, ElementRef, EventEmitter, Input, OnChanges, OnInit, Output, SimpleChanges, ViewChild } from "@angular/core";
import { CommonModule } from "@angular/common";
import { AgGridAngular, AgGridModule } from "ag-grid-angular";
import {
    ColDef,
    FilterChangedEvent,
    FirstDataRenderedEvent,
    GetRowIdFunc,
    GridApi,
    GridColumnsChangedEvent,
    GridReadyEvent,
    GridSizeChangedEvent,
    SelectionChangedEvent,
} from "ag-grid-community";
import { AgGridHelper } from "src/app/shared/helpers/ag-grid-helper";
import { TooltipComponent } from "src/app/shared/components/ag-grid/tooltip/tooltip.component";
import { FormsModule } from "@angular/forms";
import { PaginationControlsComponent } from "src/app/shared/components/ag-grid/pagination-controls/pagination-controls.component";
import { CsvDownloadButtonComponent } from "../csv-download-button/csv-download-button.component";
import { NeptuneGridHeaderComponent } from "../neptune-grid-header/neptune-grid-header.component";
import { FullScreenButtonComponent } from "../full-screen-button/full-screen-button.component";

@Component({
    selector: "neptune-grid",
    imports: [CommonModule, AgGridModule, FormsModule, PaginationControlsComponent, CsvDownloadButtonComponent, NeptuneGridHeaderComponent, FullScreenButtonComponent],
    templateUrl: "./neptune-grid.component.html",
    styleUrls: ["./neptune-grid.component.scss"]
})
export class NeptuneGridComponent implements OnInit, OnChanges {
    @ViewChild(AgGridAngular) gridref: AgGridAngular;

    // ag grid stuff
    @Output() selectionChanged: EventEmitter<SelectionChangedEvent<any>> = new EventEmitter<SelectionChangedEvent<any>>();
    @Output() filterChanged: EventEmitter<FilterChangedEvent<any>> = new EventEmitter<FilterChangedEvent<any>>();
    @Output() gridReady: EventEmitter<GridReadyEvent> = new EventEmitter<GridReadyEvent>();
    @Output() gridRefReady: EventEmitter<AgGridAngular> = new EventEmitter<AgGridAngular>();

    @Input() rowData: any[];
    @Input() columnDefs: any[];
    @Input() defaultColDef: ColDef = {
        sortable: true,
        filter: true,
        resizable: true,
        minWidth: 50,
        tooltipComponent: TooltipComponent,
        tooltipValueGetter: (params) => params.value,
    };
    @Input() rowSelection: "single" | "multiple";
    @Input() suppressRowClickSelection: boolean = false;
    @Input() rowMultiSelectWithClick: boolean = false;
    @Input() pagination: boolean = false;
    @Input() paginationPageSize: number = 100;
    @Input() getRowId: GetRowIdFunc;
    @Input() pinnedBottomRowData: any[];

    // our stuff
    @Input() width: string = "100%";
    @Input() height: string = "720px";
    @Input() downloadFileName: string = "grid-data";
    @Input() colIDsToExclude: string[] = [];
    @Input() hideDownloadButton: boolean = false;
    @Input() hideFullscreenButton: boolean = false;
    @Input() hideTooltips: boolean = false;
    @Input() hideGlobalFilter: boolean = false;
    @Input() disableGlobalFilter: boolean = false;
    @Input() sizeColumnsToFitGrid: boolean = false;
    @Input() overrideDefaultGridHeader: boolean = false;

    private gridApi: GridApi;
    public gridLoaded: boolean = false;
    public agGridOverlay: string = AgGridHelper.gridSpinnerOverlay;
    public quickFilterText: string;
    public selectedRowsCount: number = 0;
    public allRowsSelected: boolean = false;
    public multiSelectEnabled: boolean;
    public anyFilterPresent: boolean = false;
    public filteredRowsCount: number;

    public autoSizeStrategy: { type: "fitCellContents" | "fitGridWidth" };

    public fullscreenTitleText = "Make grid full screen";

    constructor(private elementRef: ElementRef) {}

    ngOnInit(): void {
        this.autoSizeStrategy = { type: this.sizeColumnsToFitGrid ? "fitGridWidth" : "fitCellContents" };
        this.multiSelectEnabled = this.rowSelection == "multiple";

        if (this.hideTooltips) {
            this.defaultColDef.tooltipValueGetter = null;
        }
    }

    ngOnChanges(changes: SimpleChanges): void {
        if (changes.rowData) {
            this.gridApi?.updateGridOptions({ rowData: this.rowData });
            this.gridApi?.hideOverlay();
        }

        if (changes.columnDefs) {
            this.gridApi?.updateGridOptions({ columnDefs: this.columnDefs });
            this.gridApi?.hideOverlay();
        }

        if (changes.pinnedBottomRowData) {
            this.gridApi?.updateGridOptions({ pinnedBottomRowData: this.pinnedBottomRowData });
        }
    }

    public onGridReady(event: GridReadyEvent) {
        this.gridReady.emit(event);
        this.gridApi = event.api;
    }

    public onFirstDataRendered(event: FirstDataRenderedEvent) {
        // The initial fit is owned by [autoSizeStrategy] (fitGridWidth or fitCellContents, set in
        // ngOnInit). We no longer call sizeColumnsToFit()/autoSizeAllColumns() here — the old code
        // ran BOTH a width-fit (here) and a content-fit (onRowDataUpdated) on every load, and they
        // fought each other. Worse, when the container had no width yet (hybrid map grids, hidden
        // tabs) the width-fit collapsed every column to its min, hiding header and data until the
        // user dragged them. Responsive re-fitting now happens in the guarded onGridSizeChanged. (NPT-1079)
        this.gridLoaded = true;

        this.gridRefReady.emit(this.gridref);
    }

    public onGridColumnsChanged(event: GridColumnsChangedEvent) {
        // Re-fit when the column set changes (e.g. dynamic custom-attribute columns), but only when
        // the grid is actually laid out — fitting a zero-width container collapses the columns.
        if (!this.gridHasWidth()) return;
        if (this.sizeColumnsToFitGrid) {
            event.api.sizeColumnsToFit();
        } else {
            event.api.autoSizeAllColumns();
        }
    }

    public onGridSizeChanged(event: GridSizeChangedEvent) {
        // Fires on container resize and when a hidden grid becomes visible. Only fit-to-width grids
        // re-flow on resize, and only once the grid has real width — the clientWidth guard is what
        // prevents the "columns collapsed until manually resized" bug. (NPT-1079)
        if (this.sizeColumnsToFitGrid && event.clientWidth > 0) {
            event.api.sizeColumnsToFit();
        }
    }

    private gridHasWidth(): boolean {
        const wrapper = this.elementRef.nativeElement?.querySelector(".ag-root-wrapper");
        return !!wrapper && wrapper.clientWidth > 0;
    }

    public onSelectionChanged(event: SelectionChangedEvent) {
        this.selectionChanged.emit(event);

        if (this.multiSelectEnabled) {
            this.selectedRowsCount = this.gridApi.getSelectedNodes().length;
            this.allRowsSelected = this.selectedRowsCount == this.rowData.length;
        }
    }

    public onFilterChanged(event: FilterChangedEvent) {
        this.filterChanged.emit(event);

        this.anyFilterPresent = event.api.isAnyFilterPresent();

        let filteredRowsCount = 0;
        this.gridApi.forEachNodeAfterFilter(() => {
            filteredRowsCount++;
        });
        this.filteredRowsCount = filteredRowsCount;
    }

    onSelectAll() {
        this.gridApi.selectAllFiltered();
    }

    onDeselectAll() {
        this.gridApi.deselectAllFiltered();
    }

    public onFiltersCleared() {
        if (this.hideGlobalFilter) return;
        this.quickFilterText = "";
    }

    public handleScreenSizeChangedEvent() {
        // Toggling fullscreen changes the grid's available width. Re-fit using the grid's own
        // strategy (width-fit vs content-fit), guarded so we never fit a zero-width container.
        if (!this.gridApi || !this.gridHasWidth()) return;
        if (this.sizeColumnsToFitGrid) {
            this.gridApi.sizeColumnsToFit();
        } else {
            this.gridApi.autoSizeAllColumns();
        }
    }
}
