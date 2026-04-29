import { Component, inject, Input, OnInit } from "@angular/core";
import { AsyncPipe } from "@angular/common";
import { Router, RouterLink } from "@angular/router";
import { BehaviorSubject, catchError, EMPTY, Observable, shareReplay, switchMap, tap } from "rxjs";
import { DialogService } from "@ngneat/dialog";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { LoadingDirective } from "src/app/shared/directives/loading.directive";
import { AlertService } from "src/app/shared/services/alert.service";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { ConfirmService } from "src/app/shared/services/confirm/confirm.service";
import { CustomAttributeTypeService } from "src/app/shared/generated/api/custom-attribute-type.service";
import { CustomAttributeTypeDto } from "src/app/shared/generated/model/custom-attribute-type-dto";
import { CustomAttributeTypePurposeEnum } from "src/app/shared/generated/enum/custom-attribute-type-purpose-enum";
import { CustomAttributeDataTypeEnum } from "src/app/shared/generated/enum/custom-attribute-data-type-enum";
import { CustomAttributeTypeModalComponent } from "src/app/pages/manage/custom-attribute-type-modal/custom-attribute-type-modal.component";

/**
 * NPT-1038 rework: read-only detail page for a single Custom Attribute Type.
 * Reachable from the Name link / View action on the index grid. Renders the
 * same fields the edit modal shows (Name, Description, Data Type, Purpose,
 * Unit, Required, Default Value, Options) plus the "Used by Treatment BMP
 * Types" list that previously cluttered the edit modal. Edit and Delete
 * buttons live at the bottom (Delete gated to non-modeling).
 */
@Component({
    selector: "custom-attribute-type-detail",
    standalone: true,
    imports: [AsyncPipe, RouterLink, PageHeaderComponent, AlertDisplayComponent, LoadingDirective],
    templateUrl: "./custom-attribute-type-detail.component.html",
    styleUrl: "./custom-attribute-type-detail.component.scss",
})
export class CustomAttributeTypeDetailComponent implements OnInit {
    @Input() customAttributeTypeID!: number;

    private customAttributeTypeService = inject(CustomAttributeTypeService);
    private alertService = inject(AlertService);
    private confirmService = inject(ConfirmService);
    private dialogService = inject(DialogService);
    private router = inject(Router);

    private reload$ = new BehaviorSubject<void>(undefined);
    public attribute$: Observable<CustomAttributeTypeDto>;
    public isLoading = true;

    public CustomAttributeTypePurposeEnum = CustomAttributeTypePurposeEnum;

    ngOnInit(): void {
        // Single subscription via the async pipe — isLoading and error handling are
        // baked into the pipeline so no second subscribe is needed (a separate
        // .subscribe would have fired a duplicate HTTP call on every reload).
        // shareReplay caches the latest emission for any future subscriber.
        this.attribute$ = this.reload$.pipe(
            tap(() => (this.isLoading = true)),
            switchMap(() => this.customAttributeTypeService.getCustomAttributeType(this.customAttributeTypeID).pipe(
                tap(() => (this.isLoading = false)),
                catchError(() => {
                    this.isLoading = false;
                    this.alertService.pushAlert(new Alert("Failed to load custom attribute type.", AlertContext.Danger));
                    return EMPTY;
                }),
            )),
            shareReplay(1),
        );
    }

    isOptionsType(dataTypeID: number | undefined): boolean {
        return dataTypeID === CustomAttributeDataTypeEnum.PickFromList || dataTypeID === CustomAttributeDataTypeEnum.MultiSelect;
    }

    parseOptions(schema: string | null | undefined): string[] {
        if (!schema) return [];
        try {
            const parsed = JSON.parse(schema);
            return Array.isArray(parsed) ? parsed : [];
        } catch {
            return [];
        }
    }

    openEditModal(attribute: CustomAttributeTypeDto): void {
        const dialogRef = this.dialogService.open(CustomAttributeTypeModalComponent, {
            data: { mode: "edit", customAttributeType: attribute },
            width: "700px",
        });
        dialogRef.afterClosed$.subscribe((result) => {
            if (result) {
                this.alertService.pushAlert(new Alert("Custom attribute type updated.", AlertContext.Success));
                this.reload$.next();
            }
        });
    }

    confirmDelete(attribute: CustomAttributeTypeDto): void {
        const name = this.escapeHtml(attribute.CustomAttributeTypeName ?? "this attribute");
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
                this.customAttributeTypeService.deleteCustomAttributeType(attribute.CustomAttributeTypeID!).subscribe({
                    next: () => {
                        this.alertService.pushAlert(new Alert("Custom attribute type deleted.", AlertContext.Success));
                        this.router.navigate(["/manage/custom-attributes"]);
                    },
                    error: () => {
                        this.alertService.pushAlert(new Alert("An error occurred while deleting the custom attribute type.", AlertContext.Danger));
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
