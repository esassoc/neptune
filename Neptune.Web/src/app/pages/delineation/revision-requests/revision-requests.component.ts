import { Component, OnInit } from "@angular/core";
import { finalize, Observable } from "rxjs";
import { ColDef } from "ag-grid-community";
import { AsyncPipe } from "@angular/common";
import { NeptuneGridComponent } from "src/app/shared/components/neptune-grid/neptune-grid.component";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { LoadingDirective } from "src/app/shared/directives/loading.directive";
import { LinkRendererComponent } from "src/app/shared/components/ag-grid/link-renderer/link-renderer.component";
import { UtilityFunctionsService } from "src/app/services/utility-functions.service";
import { RegionalSubbasinRevisionRequestService } from "src/app/shared/generated/api/regional-subbasin-revision-request.service";
import { RegionalSubbasinRevisionRequestDto } from "src/app/shared/generated/model/regional-subbasin-revision-request-dto";

@Component({
    selector: "revision-requests",
    imports: [NeptuneGridComponent, PageHeaderComponent, AlertDisplayComponent, AsyncPipe, LoadingDirective],
    templateUrl: "./revision-requests.component.html",
    styleUrl: "./revision-requests.component.scss",
})
export class RevisionRequestsComponent implements OnInit {
    public revisionRequests$: Observable<RegionalSubbasinRevisionRequestDto[]>;
    public columnDefs: ColDef[];
    public isLoading = true;

    constructor(
        private regionalSubbasinRevisionRequestService: RegionalSubbasinRevisionRequestService,
        private utilityFunctionsService: UtilityFunctionsService
    ) {}

    ngOnInit(): void {
        this.columnDefs = [
            // Render "View" as a real anchor (routerLink) so Ctrl/middle-click opens the detail page in a new tab.
            {
                headerName: "",
                field: "RegionalSubbasinRevisionRequestID",
                valueGetter: (params) => ({ LinkValue: params.data.RegionalSubbasinRevisionRequestID, LinkDisplay: "View" }),
                cellRenderer: LinkRendererComponent,
                cellRendererParams: { inRouterLink: "/delineation/revision-requests/" },
                pinned: true,
                sortable: false,
                filter: false,
                suppressSizeToFit: true,
                suppressAutoSize: true,
                width: 80,
                maxWidth: 80,
            },
            this.utilityFunctionsService.createLinkColumnDef("BMP Name", "TreatmentBMPName", "TreatmentBMPID", {
                InRouterLink: "/treatment-bmps/",
            }),
            this.utilityFunctionsService.createDateColumnDef("Date Submitted", "RequestDate", "shortDate"),
            this.utilityFunctionsService.createBasicColumnDef("Requested By", "RequestPersonName"),
            this.utilityFunctionsService.createBasicColumnDef("Status", "RegionalSubbasinRevisionRequestStatusDisplayName", {
                CustomDropdownFilterField: "RegionalSubbasinRevisionRequestStatusDisplayName",
            }),
            this.utilityFunctionsService.createDateColumnDef("Date Closed", "ClosedDate", "shortDate"),
            this.utilityFunctionsService.createBasicColumnDef("Closed By", "ClosedByPersonName"),
            this.utilityFunctionsService.createBasicColumnDef("Notes", "Notes"),
            this.utilityFunctionsService.createBasicColumnDef("Close Notes", "CloseNotes"),
        ];

        this.revisionRequests$ = this.regionalSubbasinRevisionRequestService
            .listRegionalSubbasinRevisionRequest()
            .pipe(finalize(() => (this.isLoading = false)));
    }
}
