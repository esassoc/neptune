import { Component, inject, OnInit } from "@angular/core";
import { AsyncPipe } from "@angular/common";
import { Router, RouterLink } from "@angular/router";
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
import { TreatmentBMPAssessmentObservationTypeService } from "src/app/shared/generated/api/treatment-bmp-assessment-observation-type.service";
import { TreatmentBMPAssessmentObservationTypeGridDto } from "src/app/shared/generated/model/treatment-bmp-assessment-observation-type-grid-dto";
import { NeptunePageTypeEnum } from "src/app/shared/generated/enum/neptune-page-type-enum";

@Component({
    selector: "observation-types-manage",
    standalone: true,
    imports: [PageHeaderComponent, AlertDisplayComponent, NeptuneGridComponent, AsyncPipe, RouterLink],
    template: `
        <page-header pageTitle="Observation Types" [templateRight]="addButton" [customRichTextTypeID]="NeptunePageTypeEnum.ManageObservationTypesList"></page-header>
        <ng-template #addButton>
            <a class="btn btn-primary" [routerLink]="['/manage/observation-types/new']">
                <i class="fa fa-plus"></i> Add Observation Type
            </a>
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
                        ActionHandler: () => this.router.navigate(["/manage/observation-types", id, "edit"]),
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

    private confirmDelete(row: TreatmentBMPAssessmentObservationTypeGridDto): void {
        // Pull the detail to surface the affected BMP type count in the cascade warning, mirroring
        // the legacy MVC delete dialog (TreatmentBMPAssessmentObservationTypeController.Delete:184).
        this.observationTypeService.getTreatmentBMPAssessmentObservationType(row.TreatmentBMPAssessmentObservationTypeID).subscribe({
            next: (detail) => this.promptDelete(row, detail.TreatmentBMPTypes?.length ?? 0),
            error: () => this.promptDelete(row, 0),
        });
    }

    private promptDelete(row: TreatmentBMPAssessmentObservationTypeGridDto, affectedBmpTypeCount: number): void {
        // Phrasing mirrors the legacy MVC ConfirmDialogFormViewData built in
        // TreatmentBMPAssessmentObservationTypeController.ViewDeleteObservationType. Use proper
        // <p> wrappers (not concatenated <br/>) so the confirm-modal's paragraph spacing applies.
        const name = this.escapeHtml(row.TreatmentBMPAssessmentObservationTypeName ?? "this observation type");
        const bmpTypeLabel = affectedBmpTypeCount === 1 ? "Treatment BMP Type" : "Treatment BMP Types";
        // Phrasing mirrors the legacy MVC ConfirmDialogFormViewData built in
        // TreatmentBMPAssessmentObservationTypeController.ViewDeleteObservationType — single
        // quotes around the name, no <strong>. Legacy also surfaces the count of historical
        // Observations; that requires a new field on TreatmentBMPAssessmentObservationTypeDetailDto
        // and is deferred to a follow-up backend tweak.
        this.confirmService
            .confirm({
                title: "Delete Observation Type",
                message:
                    `<p>Observation Type '${name}' is related to ${affectedBmpTypeCount} ${bmpTypeLabel}.</p>` +
                    `<p>Are you sure you want to delete this Observation Type?</p>`,
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
