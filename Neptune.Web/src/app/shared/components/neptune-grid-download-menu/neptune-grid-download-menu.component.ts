import { Component, EventEmitter, Input, Output } from "@angular/core";
import { AgGridAngular } from "ag-grid-angular";
import { IconComponent } from "src/app/shared/components/icon/icon.component";
import { DropdownToggleDirective } from "src/app/shared/directives/dropdown-toggle.directive";
import { UtilityFunctionsService } from "src/app/services/utility-functions.service";

@Component({
    selector: "neptune-grid-download-menu",
    imports: [IconComponent, DropdownToggleDirective],
    templateUrl: "./neptune-grid-download-menu.component.html",
    styleUrls: ["./neptune-grid-download-menu.component.scss"],
})
export class GridDownloadMenuComponent {
    @Input() grid: AgGridAngular;
    @Input() fileName: string;
    @Input() colIDsToExclude: string[] = [];

    // When true the menu offers a "GIS (.gdb.zip)" option. The GDB export itself is owned by the host
    // page (WQMP is a filtered POST of the displayed IDs, BMP is a plain GET), so the menu only emits
    // the request and reflects the page's downloading state — it does not perform the download.
    @Input() showGdbOption: boolean = false;
    @Input() isDownloadingGdb: boolean = false;

    @Output() gdbDownloadRequested: EventEmitter<void> = new EventEmitter<void>();

    constructor(private utilityFunctionsService: UtilityFunctionsService) {}

    public exportCsv(): void {
        if (!this.grid) return;
        const columnIDs = this.grid.api
            .getAllDisplayedColumns()
            .map((column) => column.getColId())
            .filter((id) => this.colIDsToExclude.indexOf(id) < 0);
        this.utilityFunctionsService.exportGridToCsv(this.grid, this.fileName + ".csv", columnIDs);
    }

    public onGdbClick(): void {
        if (this.isDownloadingGdb) return;
        this.gdbDownloadRequested.emit();
    }
}
