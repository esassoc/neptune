import { Component, inject, OnInit } from "@angular/core";
import { AsyncPipe } from "@angular/common";
import { Router } from "@angular/router";
import { ColDef } from "ag-grid-community";
import { BehaviorSubject, Observable, shareReplay, switchMap } from "rxjs";
import { DialogService } from "@ngneat/dialog";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { NeptuneGridComponent } from "src/app/shared/components/neptune-grid/neptune-grid.component";
import { UtilityFunctionsService } from "src/app/services/utility-functions.service";
import { AlertService } from "src/app/shared/services/alert.service";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { ConfirmService } from "src/app/shared/services/confirm/confirm.service";
import { TreatmentBMPAssessmentObservationTypeService } from "src/app/shared/generated/api/treatment-bmp-assessment-observation-type.service";
import { TreatmentBMPAssessmentObservationTypeGridDto } from "src/app/shared/generated/model/treatment-bmp-assessment-observation-type-grid-dto";
import { NeptunePageTypeEnum } from "src/app/shared/generated/enum/neptune-page-type-enum";
import { ObservationTypeModalComponent } from "src/app/pages/manage/observation-type-modal/observation-type-modal.component";

@Component({
    selector: "observation-types-manage",
    standalone: true,
    imports: [PageHeaderComponent, AlertDisplayComponent, NeptuneGridComponent, AsyncPipe],
    template: `
        <page-header pageTitle="Observation Types" [templateRight]="addButton" [customRichTextTypeID]="NeptunePageTypeEnum.ManageObservationTypesList"></page-header>
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
                    [downloadFileName]="'ObservationTypes'">
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
    private confirmService = inject(ConfirmService);
    private router = inject(Router);

    private reload$ = new BehaviorSubject<void>(undefined);
    public observationTypes$: Observable<TreatmentBMPAssessmentObservationTypeGridDto[]> = this.reload$.pipe(
        switchMap(() => this.observationTypeService.listTreatmentBMPAssessmentObservationType()),
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
                const row = params.data as TreatmentBMPAssessmentObservationTypeGridDto;
                const id = row.TreatmentBMPAssessmentObservationTypeID;
                return [
                    {
                        ActionName: "View",
                        ActionHandler: () => this.router.navigate(["/program-info/observation-types", id]),
                    },
                    {
                        ActionName: "Edit",
                        ActionIcon: "fas fa-edit",
                        ActionHandler: () => this.openEditModal(row),
                    },
                    {
                        ActionName: "Delete",
                        ActionIcon: "fa fa-trash text-danger",
                        ActionHandler: () => this.confirmDelete(row),
                    },
                ];
            }),
            this.utilityFunctionsService.createLinkColumnDef("Name", "TreatmentBMPAssessmentObservationTypeName", "TreatmentBMPAssessmentObservationTypeID", {
                InRouterLink: "/program-info/observation-types/",
            }),
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
                this.reload$.next();
            }
        });
    }

    private openEditModal(row: TreatmentBMPAssessmentObservationTypeGridDto): void {
        const dialogRef = this.dialogService.open(ObservationTypeModalComponent, {
            data: { mode: "edit", observationTypeID: row.TreatmentBMPAssessmentObservationTypeID },
            width: "800px",
        });
        dialogRef.afterClosed$.subscribe((result) => {
            if (result) {
                this.alertService.pushAlert(new Alert("Observation type updated.", AlertContext.Success));
                this.reload$.next();
            }
        });
    }

    private confirmDelete(row: TreatmentBMPAssessmentObservationTypeGridDto): void {
        // Pull the detail to surface the affected BMP types in the cascade warning, mirroring
        // the Custom Attribute Type delete flow. Falls back to a plain warning if the fetch fails.
        this.observationTypeService.getTreatmentBMPAssessmentObservationType(row.TreatmentBMPAssessmentObservationTypeID).subscribe({
            next: (detail) => this.promptDelete(row, (detail.TreatmentBMPTypes ?? [])
                .map((t) => t.TreatmentBMPTypeName)
                .filter((n): n is string => !!n)),
            error: () => this.promptDelete(row, []),
        });
    }

    private promptDelete(row: TreatmentBMPAssessmentObservationTypeGridDto, affectedBmpTypes: string[]): void {
        const name = this.escapeHtml(row.TreatmentBMPAssessmentObservationTypeName ?? "this observation type");
        const cascadeBlock = affectedBmpTypes.length > 0
            ? `<p>This will remove this observation type from the following Treatment BMP Types:</p><ul>${affectedBmpTypes.map((n) => `<li>${this.escapeHtml(n)}</li>`).join("")}</ul>`
            : `<p>This observation type is not currently assigned to any Treatment BMP Types.</p>`;
        this.confirmService
            .confirm({
                title: "Delete Observation Type",
                message: `<p>You are about to delete <strong>${name}</strong>.</p>` +
                    cascadeBlock +
                    `<p>Any saved observations of this type, and its associations to BMP types, will be removed too. This cannot be undone.</p>` +
                    `<p>Are you sure you wish to proceed?</p>`,
                buttonClassYes: "btn btn-danger",
                buttonTextYes: "Delete",
                buttonTextNo: "Cancel",
            })
            .then((confirmed) => {
                if (!confirmed) return;
                this.observationTypeService.deleteTreatmentBMPAssessmentObservationType(row.TreatmentBMPAssessmentObservationTypeID).subscribe({
                    next: () => {
                        this.alertService.pushAlert(new Alert("Observation type deleted.", AlertContext.Success));
                        this.reload$.next();
                    },
                    error: () => {
                        this.alertService.pushAlert(new Alert("An error occurred while deleting the observation type.", AlertContext.Danger));
                    },
                });
            });
    }

    // ConfirmModalComponent renders message via [innerHtml] + bypassSecurityTrustHtml,
    // so any user-controlled string interpolated into the template must be HTML-escaped.
    private escapeHtml(s: string): string {
        return s
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#39;");
    }
}
