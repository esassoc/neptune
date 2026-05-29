import { Component, OnInit } from "@angular/core";
import { AsyncPipe } from "@angular/common";
import { ColDef } from "ag-grid-community";
import { Observable } from "rxjs";

import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { NeptuneGridComponent } from "src/app/shared/components/neptune-grid/neptune-grid.component";
import { LinkRendererComponent } from "src/app/shared/components/ag-grid/link-renderer/link-renderer.component";

import { TreatmentBMPAssessmentService } from "src/app/shared/generated/api/treatment-bmp-assessment.service";
import { TreatmentBMPAssessmentGridDto } from "src/app/shared/generated/model/treatment-bmp-assessment-grid-dto";

import { UtilityFunctionsService } from "src/app/services/utility-functions.service";

@Component({
    selector: "latest-bmp-assessments",
    standalone: true,
    imports: [PageHeaderComponent, AlertDisplayComponent, NeptuneGridComponent, AsyncPipe],
    templateUrl: "./latest-bmp-assessments.component.html",
    styleUrl: "./latest-bmp-assessments.component.scss",
})
export class LatestBmpAssessmentsComponent implements OnInit {
    public assessments$: Observable<TreatmentBMPAssessmentGridDto[]>;
    public columnDefs: ColDef[];

    constructor(
        private assessmentService: TreatmentBMPAssessmentService,
        private utility: UtilityFunctionsService
    ) {}

    ngOnInit(): void {
        this.columnDefs = [
            {
                headerName: "",
                valueGetter: (params: any) => ({
                    LinkValue: params.data.TreatmentBMPAssessmentID,
                    LinkDisplay: "View",
                }),
                cellRenderer: LinkRendererComponent,
                cellRendererParams: { inRouterLink: "/treatment-bmp-assessments/" },
                sortable: false,
                filter: false,
                resizable: false,
                width: 80,
            },
            this.utility.createLinkColumnDef("BMP Name", "TreatmentBMPName", "TreatmentBMPID", {
                InRouterLink: "/treatment-bmps/",
                FieldDefinitionType: "TreatmentBMP",
            }),
            this.utility.createBasicColumnDef("BMP Type", "TreatmentBMPTypeName", { UseCustomDropdownFilter: true }),
            this.utility.createDateColumnDef("Date", "VisitDate", "MM/dd/yyyy"),
            this.utility.createBasicColumnDef("Jurisdiction", "StormwaterJurisdictionName", { UseCustomDropdownFilter: true }),
            this.utility.createBasicColumnDef("WQMP", "WaterQualityManagementPlanName"),
            this.utility.createBasicColumnDef("Performed By", "PerformedByPersonName"),
            this.utility.createBasicColumnDef("Field Visit Type", "FieldVisitTypeDisplayName", { UseCustomDropdownFilter: true }),
            this.utility.createBasicColumnDef("Assessment Type", "TreatmentBMPAssessmentTypeDisplayName", { UseCustomDropdownFilter: true }),
            this.utility.createBasicColumnDef("Status", "Status", { UseCustomDropdownFilter: true }),
            this.utility.createDecimalColumnDef("Score", "AssessmentScore"),
            // NPT-984: comma-separated failure notes from the assessment's PassFail observations.
            // Mirrors the legacy MVC "Failure Notes" column on the same page.
            this.utility.createBasicColumnDef("Failure Notes", "FailureNotes"),
        ];
        // NPT-984 round 6: latest-by-BMP is its own endpoint now. The shared list endpoint
        // returns every assessment ever recorded (used by the Field Records "Assessments" tab);
        // this page wants one row per BMP (most-recent wrapped-up assessment), so it calls the
        // dedicated `listLatestByBMP...` route. Splitting the endpoints unwound a silent
        // regression where the Field Records tab was inadvertently filtered to most-recent.
        this.assessments$ = this.assessmentService.listLatestByBMPTreatmentBMPAssessment();
    }
}
