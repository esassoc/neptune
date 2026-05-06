import { Component, OnInit } from "@angular/core";
import { finalize, Observable } from "rxjs";
import { ColDef } from "ag-grid-community";
import { AsyncPipe } from "@angular/common";
import { Router } from "@angular/router";
import { NeptuneGridComponent } from "src/app/shared/components/neptune-grid/neptune-grid.component";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { LoadingDirective } from "src/app/shared/directives/loading.directive";
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
        private utilityFunctionsService: UtilityFunctionsService,
        private router: Router
    ) {}

    ngOnInit(): void {
        this.columnDefs = [
            this.utilityFunctionsService.createActionsColumnDef((params: any) => [
                {
                    ActionName: "View",
                    ActionHandler: () =>
                        this.router.navigate(["delineation", "revision-requests", params.data.RegionalSubbasinRevisionRequestID]),
                },
            ]),
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
