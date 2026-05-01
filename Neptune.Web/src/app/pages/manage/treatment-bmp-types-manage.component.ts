import { Component, inject, OnInit } from "@angular/core";
import { Router } from "@angular/router";
import { AsyncPipe } from "@angular/common";
import { ColDef } from "ag-grid-community";
import { BehaviorSubject, Observable, shareReplay, switchMap } from "rxjs";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { NeptuneGridComponent } from "src/app/shared/components/neptune-grid/neptune-grid.component";
import { UtilityFunctionsService } from "src/app/services/utility-functions.service";
import { AlertService } from "src/app/shared/services/alert.service";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { ConfirmService } from "src/app/shared/services/confirm/confirm.service";
import { TreatmentBMPTypeService } from "src/app/shared/generated/api/treatment-bmp-type.service";
import { TreatmentBMPTypeGridDto } from "src/app/shared/generated/model/treatment-bmp-type-grid-dto";
import { NeptunePageTypeEnum } from "src/app/shared/generated/enum/neptune-page-type-enum";

@Component({
    selector: "treatment-bmp-types-manage",
    standalone: true,
    imports: [PageHeaderComponent, AlertDisplayComponent, NeptuneGridComponent, AsyncPipe],
    template: `
        <page-header pageTitle="Treatment BMP Types" [templateRight]="addButton" [customRichTextTypeID]="NeptunePageTypeEnum.ManageTreatmentBMPTypesList"></page-header>
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
                    [downloadFileName]="'TreatmentBMPTypes'">
                </neptune-grid>
            }
        </div>
    `,
})
export class TreatmentBmpTypesManageComponent implements OnInit {
    private bmpTypeService = inject(TreatmentBMPTypeService);
    private utilityFunctionsService = inject(UtilityFunctionsService);
    private alertService = inject(AlertService);
    private confirmService = inject(ConfirmService);
    private router = inject(Router);

    private reload$ = new BehaviorSubject<void>(undefined);
    public bmpTypes$: Observable<TreatmentBMPTypeGridDto[]> = this.reload$.pipe(
        switchMap(() => this.bmpTypeService.listAsGridDtoTreatmentBMPType()),
        shareReplay(1),
    );
    public columnDefs: ColDef[];
    public NeptunePageTypeEnum = NeptunePageTypeEnum;

    ngOnInit(): void {
        this.buildColumnDefs();
    }

    private buildColumnDefs(): void {
        this.columnDefs = [
            this.utilityFunctionsService.createActionsColumnDef((params: any) => {
                const row = params.data as TreatmentBMPTypeGridDto;
                const id = row.TreatmentBMPTypeID;
                return [
                    {
                        ActionName: "View",
                        ActionHandler: () => this.router.navigate(["/program-info/treatment-bmp-types", id]),
                    },
                    {
                        ActionName: "Edit",
                        ActionIcon: "fas fa-edit",
                        ActionHandler: () => this.router.navigate(["/manage/treatment-bmp-types", id, "edit"]),
                    },
                    {
                        ActionName: "Delete",
                        ActionIcon: "fa fa-trash text-danger",
                        ActionHandler: () => this.confirmDelete(row),
                    },
                ];
            }),
            this.utilityFunctionsService.createLinkColumnDef("Name", "TreatmentBMPTypeName", "TreatmentBMPTypeID", {
                InRouterLink: "/program-info/treatment-bmp-types/",
            }),
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

    private confirmDelete(row: TreatmentBMPTypeGridDto): void {
        const name = this.escapeHtml(row.TreatmentBMPTypeName ?? "this BMP type");
        // Cascade summary mirrors the data we have on the row + the back-end DeleteFull cascade
        // (BMPs, Quick BMPs, maintenance records, observations, custom attribute values, join tables).
        this.confirmService
            .confirm({
                title: "Delete Treatment BMP Type",
                message:
                    `<p>You are about to delete <strong>${name}</strong>.</p>` +
                    `<p>This will permanently remove:</p>` +
                    `<ul>` +
                    `<li>${row.TreatmentBMPCount ?? 0} Treatment BMP${(row.TreatmentBMPCount ?? 0) === 1 ? "" : "s"} of this type and all of their assessments, observations, photos, delineations, and custom attribute values</li>` +
                    `<li>Any Quick BMPs of this type and their WQMP verification associations</li>` +
                    `<li>Any maintenance records for BMPs of this type and their observations</li>` +
                    `<li>This BMP type's associations to ${row.ObservationTypeCount ?? 0} Observation Type${(row.ObservationTypeCount ?? 0) === 1 ? "" : "s"} and ${row.CustomAttributeTypeCount ?? 0} Custom Attribute${(row.CustomAttributeTypeCount ?? 0) === 1 ? "" : "s"}</li>` +
                    `</ul>` +
                    `<p>This cannot be undone. Are you sure you wish to proceed?</p>`,
                buttonClassYes: "btn btn-danger",
                buttonTextYes: "Delete",
                buttonTextNo: "Cancel",
            })
            .then((confirmed) => {
                if (!confirmed) return;
                this.bmpTypeService.deleteTreatmentBMPType(row.TreatmentBMPTypeID!).subscribe({
                    next: () => {
                        this.alertService.pushAlert(new Alert("Treatment BMP type deleted.", AlertContext.Success));
                        this.reload$.next();
                    },
                    error: () => {
                        this.alertService.pushAlert(new Alert("An error occurred while deleting the Treatment BMP type.", AlertContext.Danger));
                    },
                });
            });
    }

    private escapeHtml(s: string): string {
        return s
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#39;");
    }
}
