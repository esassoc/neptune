import { Component, inject, OnInit } from "@angular/core";
import { AsyncPipe } from "@angular/common";
import { ColDef } from "ag-grid-community";
import { Observable, shareReplay } from "rxjs";
import { DialogService } from "@ngneat/dialog";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { NeptuneGridComponent } from "src/app/shared/components/neptune-grid/neptune-grid.component";
import { UtilityFunctionsService } from "src/app/services/utility-functions.service";
import { AlertService } from "src/app/shared/services/alert.service";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { TreatmentBMPAssessmentObservationTypeService } from "src/app/shared/generated/api/treatment-bmp-assessment-observation-type.service";
import { TreatmentBMPAssessmentObservationTypeGridDto } from "src/app/shared/generated/model/treatment-bmp-assessment-observation-type-grid-dto";
import { ObservationTypeModalComponent } from "src/app/pages/manage/observation-type-modal/observation-type-modal.component";

@Component({
    selector: "observation-types-manage",
    standalone: true,
    imports: [PageHeaderComponent, AlertDisplayComponent, NeptuneGridComponent, AsyncPipe],
    template: `
        <page-header pageTitle="Observation Types" [templateRight]="addButton"></page-header>
        <ng-template #addButton>
            <button class="btn btn-primary" (click)="openAddModal()">
                <i class="fa fa-plus"></i> Add Observation Type
            </button>
        </ng-template>

        <app-alert-display></app-alert-display>

        <div class="page-body">
            @if (observationTypes$ | async; as data) {
                <neptune-grid
                    [rowData]="data"
                    [columnDefs]="columnDefs"
                    [pagination]="true"
                    [height]="'600px'"
                    [downloadFileName]="'ObservationTypes'"
                    rowSelection="single"
                    (selectionChanged)="onRowSelected($event)">
                </neptune-grid>
            }
        </div>
    `,
})
export class ObservationTypesManageComponent implements OnInit {
    private observationTypeService = inject(TreatmentBMPAssessmentObservationTypeService);
    private utilityFunctionsService = inject(UtilityFunctionsService);
    private dialogService = inject(DialogService);
    private alertService = inject(AlertService);

    public observationTypes$: Observable<TreatmentBMPAssessmentObservationTypeGridDto[]>;
    public columnDefs: ColDef[];

    ngOnInit(): void {
        this.buildColumnDefs();
        this.loadData();
    }

    private loadData(): void {
        this.observationTypes$ = this.observationTypeService.listTreatmentBMPAssessmentObservationType().pipe(shareReplay(1));
    }

    private buildColumnDefs(): void {
        this.columnDefs = [
            this.utilityFunctionsService.createBasicColumnDef("Name", "TreatmentBMPAssessmentObservationTypeName"),
            this.utilityFunctionsService.createBasicColumnDef("Collection Method", "ObservationTypeCollectionMethodDisplayName", { UseCustomDropdownFilter: true }),
            this.utilityFunctionsService.createBasicColumnDef("Target Type", "ObservationTargetTypeDisplayName", { UseCustomDropdownFilter: true }),
            this.utilityFunctionsService.createBasicColumnDef("Threshold Type", "ObservationThresholdTypeDisplayName", { UseCustomDropdownFilter: true }),
            this.utilityFunctionsService.createDecimalColumnDef("# BMP Types", "TreatmentBMPTypeCount", { DecimalPlacesToDisplay: 0 }),
        ];
    }

    openAddModal(): void {
        const dialogRef = this.dialogService.open(ObservationTypeModalComponent, {
            data: { mode: "add" },
            width: "800px",
        });
        dialogRef.afterClosed$.subscribe((result) => {
            if (result) {
                this.alertService.pushAlert(new Alert("Observation type created.", AlertContext.Success));
                this.loadData();
            }
        });
    }

    onRowSelected(event: any): void {
        const selectedRows = event.api.getSelectedRows();
        if (!selectedRows?.length) return;
        const selected = selectedRows[0] as TreatmentBMPAssessmentObservationTypeGridDto;

        const dialogRef = this.dialogService.open(ObservationTypeModalComponent, {
            data: { mode: "edit", observationTypeID: selected.TreatmentBMPAssessmentObservationTypeID },
            width: "800px",
        });
        dialogRef.afterClosed$.subscribe((result) => {
            if (result) {
                this.alertService.pushAlert(new Alert("Observation type updated.", AlertContext.Success));
                this.loadData();
            }
        });
    }
}
