import { Component, inject, OnInit } from "@angular/core";
import { AsyncPipe } from "@angular/common";
import { ColDef } from "ag-grid-community";
import { Observable, shareReplay } from "rxjs";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { NeptuneGridComponent } from "src/app/shared/components/neptune-grid/neptune-grid.component";
import { UtilityFunctionsService } from "src/app/services/utility-functions.service";
import { TreatmentBMPAssessmentObservationTypeService } from "src/app/shared/generated/api/treatment-bmp-assessment-observation-type.service";
import { TreatmentBMPAssessmentObservationTypeGridDto } from "src/app/shared/generated/model/treatment-bmp-assessment-observation-type-grid-dto";
import { NeptunePageTypeEnum } from "src/app/shared/generated/enum/neptune-page-type-enum";

@Component({
    selector: "observation-types",
    standalone: true,
    imports: [PageHeaderComponent, NeptuneGridComponent, AsyncPipe],
    template: `
        <page-header pageTitle="Observation Types" [customRichTextTypeID]="NeptunePageTypeEnum.ManageObservationTypesList"></page-header>
        <div class="page-body">
            @if (observationTypes$ | async; as data) {
                <neptune-grid
                    [rowData]="data"
                    [columnDefs]="columnDefs"
                    [pagination]="true"
                    [height]="'600px'"
                    [downloadFileName]="'ObservationTypes'">
                </neptune-grid>
            }
        </div>
    `,
})
export class ObservationTypesComponent implements OnInit {
    private observationTypeService = inject(TreatmentBMPAssessmentObservationTypeService);
    private utilityFunctionsService = inject(UtilityFunctionsService);

    public observationTypes$: Observable<TreatmentBMPAssessmentObservationTypeGridDto[]>;
    public columnDefs: ColDef[];
    public NeptunePageTypeEnum = NeptunePageTypeEnum;

    ngOnInit(): void {
        this.columnDefs = [
            this.utilityFunctionsService.createLinkColumnDef("Name", "TreatmentBMPAssessmentObservationTypeName", "TreatmentBMPAssessmentObservationTypeID", {
                InRouterLink: "/program-info/observation-types/",
            }),
            this.utilityFunctionsService.createBasicColumnDef("Collection Method", "ObservationTypeCollectionMethodDisplayName", { UseCustomDropdownFilter: true }),
            this.utilityFunctionsService.createBasicColumnDef("Target Type", "ObservationTargetTypeDisplayName", { UseCustomDropdownFilter: true }),
            this.utilityFunctionsService.createBasicColumnDef("Threshold Type", "ObservationThresholdTypeDisplayName", { UseCustomDropdownFilter: true }),
            this.utilityFunctionsService.createDecimalColumnDef("# BMP Types", "TreatmentBMPTypeCount", { DecimalPlacesToDisplay: 0 }),
        ];
        this.observationTypes$ = this.observationTypeService.listTreatmentBMPAssessmentObservationType().pipe(shareReplay(1));
    }
}
