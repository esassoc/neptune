import { Component, inject, OnInit } from "@angular/core";
import { AsyncPipe } from "@angular/common";
import { ColDef } from "ag-grid-community";
import { Observable, shareReplay } from "rxjs";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { NeptuneGridComponent } from "src/app/shared/components/neptune-grid/neptune-grid.component";
import { UtilityFunctionsService } from "src/app/services/utility-functions.service";
import { TreatmentBMPTypeService } from "src/app/shared/generated/api/treatment-bmp-type.service";
import { TreatmentBMPTypeGridDto } from "src/app/shared/generated/model/treatment-bmp-type-grid-dto";

@Component({
    selector: "treatment-bmp-types",
    standalone: true,
    imports: [PageHeaderComponent, NeptuneGridComponent, AsyncPipe],
    template: `
        <page-header pageTitle="Treatment BMP Types"></page-header>
        <div class="page-body">
            @if (bmpTypes$ | async; as data) {
                <neptune-grid
                    [rowData]="data"
                    [columnDefs]="columnDefs"
                    [pagination]="true"
                    [height]="'600px'"
                    [downloadFileName]="'TreatmentBMPTypes'">
                </neptune-grid>
            }
        </div>
    `,
})
export class TreatmentBmpTypesComponent implements OnInit {
    private bmpTypeService = inject(TreatmentBMPTypeService);
    private utilityFunctionsService = inject(UtilityFunctionsService);

    public bmpTypes$: Observable<TreatmentBMPTypeGridDto[]>;
    public columnDefs: ColDef[];

    ngOnInit(): void {
        this.columnDefs = [
            this.utilityFunctionsService.createBasicColumnDef("Name", "TreatmentBMPTypeName"),
            this.utilityFunctionsService.createBasicColumnDef("Description", "TreatmentBMPTypeDescription", { MaxWidth: 400 }),
            this.utilityFunctionsService.createDecimalColumnDef("# Observation Types", "ObservationTypeCount", { DecimalPlacesToDisplay: 0 }),
            this.utilityFunctionsService.createDecimalColumnDef("# Custom Attributes", "CustomAttributeTypeCount", { DecimalPlacesToDisplay: 0 }),
            this.utilityFunctionsService.createDecimalColumnDef("# BMPs", "TreatmentBMPCount", { DecimalPlacesToDisplay: 0 }),
            this.utilityFunctionsService.createBasicColumnDef("In Modeling", "IsAnalyzedInModelingModule", {
                UseCustomDropdownFilter: true,
                ValueGetter: (params) => params.data?.IsAnalyzedInModelingModule ? "Yes" : "No",
            }),
        ];
        this.bmpTypes$ = this.bmpTypeService.listAsGridDtoTreatmentBMPType().pipe(shareReplay(1));
    }
}
