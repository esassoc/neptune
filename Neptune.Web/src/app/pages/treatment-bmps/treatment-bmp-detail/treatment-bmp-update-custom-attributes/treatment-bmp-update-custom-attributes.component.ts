import { Component, inject, Input, OnInit } from "@angular/core";
import { CustomAttributeTypePurposes } from "src/app/shared/generated/enum/custom-attribute-type-purpose-enum";
import { LoadingDirective } from "src/app/shared/directives/loading.directive";
import { FormGroup, ReactiveFormsModule } from "@angular/forms";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { AsyncPipe } from "@angular/common";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { Router, RouterModule } from "@angular/router";
import { Observable, shareReplay, switchMap, tap } from "rxjs";
import { TreatmentBMPService } from "src/app/shared/generated/api/treatment-bmp.service";
import { TreatmentBMPTypeCustomAttributeTypeDto } from "src/app/shared/generated/model/treatment-bmp-type-custom-attribute-type-dto";
import { TreatmentBMPTypeService } from "src/app/shared/generated/api/treatment-bmp-type.service";
import { CustomAttributeDto, CustomAttributeUpsertDto, TreatmentBMPDto } from "src/app/shared/generated/model/models";
import { CustomAttributesEditorComponent } from "src/app/shared/components/custom-attributes-editor/custom-attributes-editor.component";
import { IDeactivateComponent } from "src/app/shared/guards/unsaved-changes.guard";
import { AlertService } from "src/app/shared/services/alert.service";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";

@Component({
    selector: "treatment-bmp-update-custom-attributes",
    imports: [LoadingDirective, ReactiveFormsModule, PageHeaderComponent, AsyncPipe, AlertDisplayComponent, RouterModule, CustomAttributesEditorComponent],
    templateUrl: "./treatment-bmp-update-custom-attributes.component.html",
    styleUrl: "./treatment-bmp-update-custom-attributes.component.scss",
})
export class TreatmentBmpUpdateCustomAttributesComponent implements OnInit, IDeactivateComponent {
    @Input() treatmentBMPID?: number;
    @Input() customAttributePurposeID?: number;

    private treatmentBMPService = inject(TreatmentBMPService);
    private treatmentBMPTypeService = inject(TreatmentBMPTypeService);
    private router = inject(Router);
    private alertService = inject(AlertService);

    public customAttributePurposeName?: string;
    public treatmentBMP$: Observable<TreatmentBMPDto>;
    public treatmentBMPTypeCustomAttributeTypes$: Observable<TreatmentBMPTypeCustomAttributeTypeDto[]>;
    public customAttributes$: Observable<CustomAttributeDto[]>;

    public hasAttributes = false;

    public formGroup: FormGroup = new FormGroup({});

    public isLoadingSubmit = false;

    ngOnInit(): void {
        this.customAttributePurposeName = CustomAttributeTypePurposes.find((x) => x.Value == this.customAttributePurposeID!)?.DisplayName;
        this.treatmentBMP$ = this.treatmentBMPService.getByIDTreatmentBMP(this.treatmentBMPID!).pipe(shareReplay(1));

        this.treatmentBMPTypeCustomAttributeTypes$ = this.treatmentBMP$.pipe(
            switchMap((bmp) =>
                this.treatmentBMPTypeService.listCustomAttributeTypesTreatmentBMPType(bmp.TreatmentBMPTypeID).pipe(
                    tap((attributes) => {
                        this.hasAttributes =
                            Array.isArray(attributes) && attributes.some((attr) => attr.CustomAttributeType?.CustomAttributeTypePurposeID === this.customAttributePurposeID);
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

        let customAttributeUpsertDtos: CustomAttributeUpsertDto[] = [];
        for (const controlName of Object.keys(this.formGroup.controls)) {
            const controlValue = this.formGroup.controls[controlName].value;
            const customAttributeTypeID = Number(controlName.replace("CustomAttributeType_", ""));
            if (controlValue != null) {
                let values: string[] = [];
                if (Array.isArray(controlValue)) {
                    values = controlValue;
                } else {
                    values = [controlValue];
                }

                let treatmentBMPTypeCustomAttributeType = treatmentBMPTypeCustomAttributeTypes.find((x) => x.CustomAttributeTypeID == customAttributeTypeID);
                customAttributeUpsertDtos.push({
                    CustomAttributeTypeID: customAttributeTypeID,
                    TreatmentBMPTypeCustomAttributeTypeID: treatmentBMPTypeCustomAttributeType?.TreatmentBMPTypeCustomAttributeTypeID,
                    CustomAttributeValues: values,
                } as CustomAttributeUpsertDto);
            }
        }

        this.treatmentBMPService.updateCustomAttributesTreatmentBMP(this.treatmentBMPID!, this.customAttributePurposeID!, customAttributeUpsertDtos).subscribe({
            next: () => {
                this.isLoadingSubmit = false;
                this.formGroup.markAsPristine();
                this.router.navigate(["/treatment-bmps", this.treatmentBMPID]).then(() => {
                    this.alertService.pushAlert(new Alert(`Treatment BMP ${this.customAttributePurposeName} custom attributes updated successfully.`, AlertContext.Success));
                });
            },
            error: () => {
                this.isLoadingSubmit = false;
            },
        });
    }

    public cancel(): void {
        this.router.navigate(["/treatment-bmps", this.treatmentBMPID]);
    }
}
