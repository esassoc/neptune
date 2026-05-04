import { Component, OnInit } from "@angular/core";
import { AsyncPipe } from "@angular/common";
import { ColDef } from "ag-grid-community";
import { Observable } from "rxjs";

import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { NeptuneGridComponent } from "src/app/shared/components/neptune-grid/neptune-grid.component";

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
            this.utility.createLinkColumnDef("BMP Name", "TreatmentBMPName", "TreatmentBMPID", {
                InRouterLink: "/treatment-bmps/",
                FieldDefinitionType: "TreatmentBMP",
            }),
            this.utility.createBasicColumnDef("BMP Type", "TreatmentBMPTypeName"),
            this.utility.createDateColumnDef("Date", "VisitDate", "MM/dd/yyyy"),
            this.utility.createBasicColumnDef("Jurisdiction", "StormwaterJurisdictionName"),
            this.utility.createBasicColumnDef("WQMP", "WaterQualityManagementPlanName"),
            this.utility.createBasicColumnDef("Performed By", "PerformedByPersonName"),
            this.utility.createBasicColumnDef("Field Visit Type", "FieldVisitTypeDisplayName"),
            this.utility.createBasicColumnDef("Assessment Type", "TreatmentBMPAssessmentTypeDisplayName"),
            this.utility.createBasicColumnDef("Status", "Status"),
            this.utility.createDecimalColumnDef("Score", "AssessmentScore"),
        ];
        this.assessments$ = this.assessmentService.listTreatmentBMPAssessment();
    }
}
