import { Component, inject, OnInit } from "@angular/core";
import { AsyncPipe } from "@angular/common";
import { ColDef } from "ag-grid-community";
import { Observable, shareReplay, tap } from "rxjs";
import { DialogService } from "@ngneat/dialog";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { NeptuneGridComponent } from "src/app/shared/components/neptune-grid/neptune-grid.component";
import { UtilityFunctionsService } from "src/app/services/utility-functions.service";
import { AlertService } from "src/app/shared/services/alert.service";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { CustomAttributeTypeService } from "src/app/shared/generated/api/custom-attribute-type.service";
import { CustomAttributeTypeDto } from "src/app/shared/generated/model/custom-attribute-type-dto";
import { CustomAttributeTypeModalComponent } from "src/app/pages/manage/custom-attribute-type-modal/custom-attribute-type-modal.component";

@Component({
    selector: "custom-attributes",
    standalone: true,
    imports: [PageHeaderComponent, AlertDisplayComponent, NeptuneGridComponent, AsyncPipe],
    template: `
        <page-header pageTitle="Custom Attribute Types" [templateRight]="addButton"></page-header>
        <ng-template #addButton>
            <button class="btn btn-primary" (click)="openAddModal()">
                <i class="fa fa-plus"></i> Add Custom Attribute Type
            </button>
        </ng-template>

        <app-alert-display></app-alert-display>

        <div class="page-body">
            @if (customAttributeTypes$ | async; as data) {
                <neptune-grid
                    [rowData]="data"
                    [columnDefs]="columnDefs"
                    [pagination]="true"
                    [height]="'600px'"
                    [downloadFileName]="'CustomAttributeTypes'"
                    rowSelection="single"
                    (selectionChanged)="onRowSelected($event)">
                </neptune-grid>
            }
        </div>
    `,
})
export class CustomAttributesComponent implements OnInit {
    private customAttributeTypeService = inject(CustomAttributeTypeService);
    private utilityFunctionsService = inject(UtilityFunctionsService);
    private dialogService = inject(DialogService);
    private alertService = inject(AlertService);

    public customAttributeTypes$: Observable<CustomAttributeTypeDto[]>;
    public columnDefs: ColDef[];

    ngOnInit(): void {
        this.buildColumnDefs();
        this.loadData();
    }

    private loadData(): void {
        this.customAttributeTypes$ = this.customAttributeTypeService.listCustomAttributeType().pipe(shareReplay(1));
    }

    private buildColumnDefs(): void {
        this.columnDefs = [
            this.utilityFunctionsService.createBasicColumnDef("Name", "CustomAttributeTypeName"),
            this.utilityFunctionsService.createBasicColumnDef("Data Type", "DataTypeDisplayName", { UseCustomDropdownFilter: true }),
            this.utilityFunctionsService.createBasicColumnDef("Purpose", "Purpose", { UseCustomDropdownFilter: true }),
            this.utilityFunctionsService.createBasicColumnDef("Is Required", "IsRequired", {
                UseCustomDropdownFilter: true,
                ValueGetter: (params) => params.data?.IsRequired ? "Yes" : "No",
            }),
            this.utilityFunctionsService.createBasicColumnDef("Measurement Unit", "MeasurementUnitDisplayName"),
            this.utilityFunctionsService.createBasicColumnDef("Description", "CustomAttributeTypeDescription", { MaxWidth: 300 }),
        ];
    }

    openAddModal(): void {
        const dialogRef = this.dialogService.open(CustomAttributeTypeModalComponent, {
            data: { mode: "add" },
            width: "700px",
        });
        dialogRef.afterClosed$.subscribe((result) => {
            if (result) {
                this.alertService.pushAlert(new Alert("Custom attribute type created.", AlertContext.Success));
                this.loadData();
            }
        });
    }

    onRowSelected(event: any): void {
        const selectedRows = event.api.getSelectedRows();
        if (!selectedRows?.length) return;
        const selected = selectedRows[0] as CustomAttributeTypeDto;

        const dialogRef = this.dialogService.open(CustomAttributeTypeModalComponent, {
            data: { mode: "edit", customAttributeType: selected },
            width: "700px",
        });
        dialogRef.afterClosed$.subscribe((result) => {
            if (result) {
                this.alertService.pushAlert(new Alert("Custom attribute type updated.", AlertContext.Success));
                this.loadData();
            }
        });
    }
}
