import { Component, EventEmitter, inject, Input, OnInit, Output } from "@angular/core";
import { FormGroup, ReactiveFormsModule } from "@angular/forms";
import { AsyncPipe } from "@angular/common";
import { Observable, shareReplay, switchMap, tap } from "rxjs";

import { CustomAttributesEditorComponent } from "src/app/shared/components/custom-attributes-editor/custom-attributes-editor.component";
import { LoadingDirective } from "src/app/shared/directives/loading.directive";
import { CustomAttributeTypePurposes } from "src/app/shared/generated/enum/custom-attribute-type-purpose-enum";
import { TreatmentBMPService } from "src/app/shared/generated/api/treatment-bmp.service";
import { TreatmentBMPTypeService } from "src/app/shared/generated/api/treatment-bmp-type.service";
import { TreatmentBMPTypeCustomAttributeTypeDto } from "src/app/shared/generated/model/treatment-bmp-type-custom-attribute-type-dto";
import { CustomAttributeDto } from "src/app/shared/generated/model/custom-attribute-dto";
import { CustomAttributeUpsertDto } from "src/app/shared/generated/model/custom-attribute-upsert-dto";
import { TreatmentBMPDto } from "src/app/shared/generated/model/treatment-bmp-dto";
import { AlertService } from "src/app/shared/services/alert.service";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";

/**
 * Reusable embedded editor for a Treatment BMP's custom attributes scoped to a
 * single CustomAttributeTypePurpose. Wraps the generic &lt;custom-attributes-editor&gt;
 * with the BMP-specific data load + save logic. Used by the BMP detail routed
 * edit page and by the Field Visit Inventory workflow step.
 */
@Component({
    selector: "treatment-bmp-custom-attributes-form",
    standalone: true,
    imports: [ReactiveFormsModule, AsyncPipe, LoadingDirective, CustomAttributesEditorComponent],
    templateUrl: "./treatment-bmp-custom-attributes-form.component.html",
})
export class TreatmentBmpCustomAttributesFormComponent implements OnInit {
    private treatmentBMPService = inject(TreatmentBMPService);
    private treatmentBMPTypeService = inject(TreatmentBMPTypeService);
    private alertService = inject(AlertService);

    @Input() treatmentBMPID!: number;
    @Input() customAttributePurposeID!: number;
    /** When true, suppresses the built-in Save/Cancel footer so a host (e.g. the field-visit
     * workflow) can render its own button row and drive saves via @ViewChild + saveFromHost(). */
    @Input() hideFooter = false;

    @Output() saved = new EventEmitter<void>();
    @Output() cancelled = new EventEmitter<void>();
    /** Emitted when a save attempt fails or is rejected before it can fire (e.g. attribute types
     * not yet loaded). Hosts driving saves via @ViewChild should listen here to clear their
     * own in-flight UI state on error/no-op paths. */
    @Output() saveError = new EventEmitter<void>();

    public customAttributePurposeName?: string;
    public treatmentBMP$!: Observable<TreatmentBMPDto>;
    public treatmentBMPTypeCustomAttributeTypes$!: Observable<TreatmentBMPTypeCustomAttributeTypeDto[]>;
    public customAttributes$!: Observable<CustomAttributeDto[]>;
    public hasAttributes = false;
    public formGroup = new FormGroup({});
    public isLoadingSubmit = false;

    /** Cached attribute-types last emitted by the observable, populated via tap(). The host calls
     * saveFromHost() without needing to thread the types in; saveFromHost() uses this cache. */
    private cachedAttributeTypes: TreatmentBMPTypeCustomAttributeTypeDto[] = [];

    ngOnInit(): void {
        this.customAttributePurposeName = CustomAttributeTypePurposes.find((x) => x.Value == this.customAttributePurposeID)?.DisplayName;
        this.treatmentBMP$ = this.treatmentBMPService.getByIDTreatmentBMP(this.treatmentBMPID).pipe(shareReplay(1));

        this.treatmentBMPTypeCustomAttributeTypes$ = this.treatmentBMP$.pipe(
            switchMap((bmp) =>
                this.treatmentBMPTypeService.listCustomAttributeTypesTreatmentBMPType(bmp.TreatmentBMPTypeID).pipe(
                    tap((attributes) => {
                        this.cachedAttributeTypes = attributes ?? [];
                        this.hasAttributes =
                            Array.isArray(attributes) &&
                            attributes.some((attr) => attr.CustomAttributeType?.CustomAttributeTypePurposeID === this.customAttributePurposeID);
                    })
                )
            )
        );

        this.customAttributes$ = this.treatmentBMP$.pipe(switchMap((bmp) => this.treatmentBMPService.listCustomAttributesTreatmentBMP(bmp.TreatmentBMPID)));
    }

    public canExit(): boolean {
        return this.formGroup.pristine;
    }

    public save(treatmentBMPTypeCustomAttributeTypes: TreatmentBMPTypeCustomAttributeTypeDto[]): void {
        this.isLoadingSubmit = true;

        const customAttributeUpsertDtos: CustomAttributeUpsertDto[] = [];
        for (const controlName of Object.keys(this.formGroup.controls)) {
            const controlValue = (this.formGroup.controls as Record<string, { value: unknown }>)[controlName].value;
            const customAttributeTypeID = Number(controlName.replace("CustomAttributeType_", ""));
            if (controlValue != null) {
                const values: string[] = Array.isArray(controlValue) ? (controlValue as string[]) : [controlValue as string];
                const treatmentBMPTypeCustomAttributeType = treatmentBMPTypeCustomAttributeTypes.find((x) => x.CustomAttributeTypeID == customAttributeTypeID);
                customAttributeUpsertDtos.push({
                    CustomAttributeTypeID: customAttributeTypeID,
                    TreatmentBMPTypeCustomAttributeTypeID: treatmentBMPTypeCustomAttributeType?.TreatmentBMPTypeCustomAttributeTypeID,
                    CustomAttributeValues: values,
                } as CustomAttributeUpsertDto);
            }
        }

        this.treatmentBMPService
            .updateCustomAttributesTreatmentBMP(this.treatmentBMPID, this.customAttributePurposeID, customAttributeUpsertDtos)
            .subscribe({
                next: () => {
                    this.isLoadingSubmit = false;
                    this.formGroup.markAsPristine();
                    this.alertService.pushAlert(new Alert(`Treatment BMP ${this.customAttributePurposeName} attributes updated successfully.`, AlertContext.Success));
                    this.saved.emit();
                },
                error: () => {
                    this.isLoadingSubmit = false;
                    this.saveError.emit();
                },
            });
    }

    public cancel(): void {
        this.cancelled.emit();
    }

    /** Imperative save trigger for hosts using `[hideFooter]`. Uses the cached attribute-types
     * captured during the load pipeline so the host doesn't need to pass them in. When the BMP
     * type has no custom attributes (legitimate empty case) we still want to give the host a
     * `(saved)` signal so its 3-button footer can advance — there's nothing to persist, so the
     * empty case is effectively a no-op success. */
    public saveFromHost(): void {
        if (this.cachedAttributeTypes.length === 0) {
            this.saved.emit();
            return;
        }
        this.save(this.cachedAttributeTypes);
    }
}
