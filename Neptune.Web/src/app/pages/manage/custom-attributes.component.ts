import { Component, inject, OnInit } from "@angular/core";
import { AsyncPipe } from "@angular/common";
import { Router } from "@angular/router";
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
import { CustomAttributeTypeService } from "src/app/shared/generated/api/custom-attribute-type.service";
import { CustomAttributeTypeDto } from "src/app/shared/generated/model/custom-attribute-type-dto";
import { CustomAttributeTypePurposeEnum } from "src/app/shared/generated/enum/custom-attribute-type-purpose-enum";
import { NeptunePageTypeEnum } from "src/app/shared/generated/enum/neptune-page-type-enum";
import { escapeHtml } from "src/app/shared/helpers/html-escape";

@Component({
    selector: "custom-attributes",
    standalone: true,
    imports: [PageHeaderComponent, AlertDisplayComponent, NeptuneGridComponent, AsyncPipe],
    template: `
        <page-header pageTitle="Custom Attribute Types" [templateRight]="addButton" [customRichTextTypeID]="NeptunePageTypeEnum.ManageCustomAttributeTypesList"></page-header>
        <ng-template #addButton>
            <button class="btn btn-primary" (click)="openAdd()">
                <i class="fa fa-plus"></i> Add Custom Attribute Type
            </button>
        </ng-template>

        <app-alert-display></app-alert-display>

        <div class="page-body">
            <p class="system-text mb-2">
                <i class="fa fa-info-circle"></i>
                Modeling attributes are system-managed and cannot be deleted. The Delete action only appears for Other Design and Maintenance attributes.
            </p>
            @if (customAttributeTypes$ | async; as data) {
                <neptune-grid
                    [rowData]="data"
                    [columnDefs]="columnDefs"
                    [pagination]="true"
                    [height]="'600px'"
                    [downloadFileName]="'CustomAttributeTypes'">
                </neptune-grid>
            }
        </div>
    `,
})
export class CustomAttributesComponent implements OnInit {
    private customAttributeTypeService = inject(CustomAttributeTypeService);
    private utilityFunctionsService = inject(UtilityFunctionsService);
    private alertService = inject(AlertService);
    private confirmService = inject(ConfirmService);
    private router = inject(Router);

    // NPT-1038 rework: drives grid refresh after edits/deletes. Replaces the previous
    // shareReplay-on-reassign pattern, which left the grid bound to a stale subscription
    // after loadData() rebuilt the observable — edits only showed up after a manual
    // page reload.
    private reload$ = new BehaviorSubject<void>(undefined);
    public customAttributeTypes$: Observable<CustomAttributeTypeDto[]> = this.reload$.pipe(
        switchMap(() => this.customAttributeTypeService.listCustomAttributeType()),
        shareReplay(1),
    );
    public columnDefs: ColDef[];
    public NeptunePageTypeEnum = NeptunePageTypeEnum;

    ngOnInit(): void {
        this.buildColumnDefs();
    }

    private buildColumnDefs(): void {
        this.columnDefs = [
            // NPT-1038: Row actions (View / Edit / Delete). Delete is gated to
            // non-modeling attributes — modeling rows are system-managed and the
            // backend refuses to remove them.
            this.utilityFunctionsService.createActionsColumnDef((params: any) => {
                const row = params.data as CustomAttributeTypeDto;
                const id = row.CustomAttributeTypeID;
                const isModeling = row.CustomAttributeTypePurposeID === CustomAttributeTypePurposeEnum.Modeling;
                const actions: any[] = [
                    {
                        ActionName: "View",
                        ActionIcon: "fas fa-file-alt",
                        ActionHandler: () => this.router.navigate(["/manage/custom-attributes", id]),
                    },
                    {
                        ActionName: "Edit",
                        ActionIcon: "fas fa-edit",
                        ActionHandler: () => this.router.navigate(["/manage/custom-attributes", id, "edit"]),
                    },
                ];
                if (!isModeling) {
                    actions.push({
                        ActionName: "Delete",
                        ActionIcon: "fas fa-trash text-danger",
                        ActionHandler: () => this.confirmDelete(row),
                    });
                }
                return actions;
            }),
            // NPT-1038: Name column is now a router-link to the detail page so it's
            // visually obvious clicking it does something.
            this.utilityFunctionsService.createLinkColumnDef("Name", "CustomAttributeTypeName", "CustomAttributeTypeID", {
                InRouterLink: "/manage/custom-attributes/",
            }),
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

    openAdd(): void {
        this.router.navigate(["/manage/custom-attributes/new"]);
    }

    private confirmDelete(row: CustomAttributeTypeDto): void {
        const name = escapeHtml(row.CustomAttributeTypeName ?? "this attribute");
        this.confirmService
            .confirm({
                title: "Delete Custom Attribute Type",
                message: `<p>You are about to delete <strong>${name}</strong>.</p>` +
                    `<p>Any values stored on Treatment BMPs or Maintenance Records using this attribute, plus its associations to BMP types, will be removed too. This cannot be undone.</p>` +
                    `<p>Are you sure you wish to proceed?</p>`,
                buttonClassYes: "btn btn-danger",
                buttonTextYes: "Delete",
                buttonTextNo: "Cancel",
            })
            .then((confirmed) => {
                if (!confirmed) return;
                this.customAttributeTypeService.deleteCustomAttributeType(row.CustomAttributeTypeID!).subscribe({
                    next: () => {
                        this.alertService.pushAlert(new Alert("Custom attribute type deleted.", AlertContext.Success));
                        this.reload$.next();
                    },
                    error: (err) => {
                        // Surface the backend's ProblemDetails message when available so PO/dev
                        // can diagnose a failed delete (FK conflict, permission, etc.) instead
                        // of seeing the generic toast we used to show. AlertDisplayComponent
                        // renders via [innerHTML] + bypassSecurityTrustHtml, so escape the
                        // server-supplied text before interpolating.
                        const raw = err?.error?.detail ?? err?.error?.title
                            ?? "An error occurred while deleting the custom attribute type.";
                        this.alertService.pushAlert(new Alert(escapeHtml(raw), AlertContext.Danger));
                    },
                });
            })
            .catch(() => { /* dismissed via X/Escape — no-op */ });
    }
}
