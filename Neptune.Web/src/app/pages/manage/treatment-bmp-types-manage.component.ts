import { Component, inject, OnInit } from "@angular/core";
import { Router } from "@angular/router";
import { AsyncPipe } from "@angular/common";
import { ColDef } from "ag-grid-community";
import { Observable, shareReplay } from "rxjs";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { NeptuneGridComponent } from "src/app/shared/components/neptune-grid/neptune-grid.component";
import { UtilityFunctionsService } from "src/app/services/utility-functions.service";
import { AlertService } from "src/app/shared/services/alert.service";
import { TreatmentBMPTypeService } from "src/app/shared/generated/api/treatment-bmp-type.service";
import { TreatmentBMPTypeGridDto } from "src/app/shared/generated/model/treatment-bmp-type-grid-dto";

@Component({
    selector: "treatment-bmp-types-manage",
    standalone: true,
    imports: [PageHeaderComponent, AlertDisplayComponent, NeptuneGridComponent, AsyncPipe],
    template: `
        <page-header pageTitle="Treatment BMP Types" [templateRight]="addButton"></page-header>
        <ng-template #addButton>
            <button class="btn btn-primary" (click)="navigateToNew()">
                <i class="fa fa-plus"></i> Add Treatment BMP Type
            </button>
        </ng-template>

        <app-alert-display></app-alert-display>

        <div class="page-body">
            @if (bmpTypes$ | async; as data) {
                <neptune-grid
                    [rowData]="data"
                    [columnDefs]="columnDefs"
                    [pagination]="true"
                    [height]="'600px'"
                    [downloadFileName]="'TreatmentBMPTypes'"
                    rowSelection="single"
                    (selectionChanged)="onRowSelected($event)">
                </neptune-grid>
            }
        </div>
    `,
})
export class TreatmentBmpTypesManageComponent implements OnInit {
    private bmpTypeService = inject(TreatmentBMPTypeService);
    private utilityFunctionsService = inject(UtilityFunctionsService);
    private alertService = inject(AlertService);
    private router = inject(Router);

    public bmpTypes$: Observable<TreatmentBMPTypeGridDto[]>;
    public columnDefs: ColDef[];

    ngOnInit(): void {
        this.buildColumnDefs();
        this.loadData();
    }

    private loadData(): void {
        this.bmpTypes$ = this.bmpTypeService.listAsGridDtoTreatmentBMPType().pipe(shareReplay(1));
    }

    private buildColumnDefs(): void {
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
    }

    navigateToNew(): void {
        this.router.navigate(["/manage/treatment-bmp-types/new"]);
    }

    onRowSelected(event: any): void {
        const selectedRows = event.api.getSelectedRows();
        if (!selectedRows?.length) return;
        const selected = selectedRows[0] as TreatmentBMPTypeGridDto;
        this.router.navigate(["/manage/treatment-bmp-types", selected.TreatmentBMPTypeID, "edit"]);
    }
}
