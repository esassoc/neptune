import { Component, inject, OnInit } from "@angular/core";
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from "@angular/forms";
import { DialogRef } from "@ngneat/dialog";
import { FormFieldComponent, FormFieldType, FormInputOption } from "src/app/shared/components/forms/form-field/form-field.component";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { OnlandVisualTrashAssessmentAreaService } from "src/app/shared/generated/api/onland-visual-trash-assessment-area.service";
import { OnlandVisualTrashAssessmentAreaMoveAssessmentsDto } from "src/app/shared/generated/model/onland-visual-trash-assessment-area-move-assessments-dto";
import { AlertService } from "src/app/shared/services/alert.service";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";

@Component({
    selector: "move-ovta-assessments-modal",
    imports: [ReactiveFormsModule, FormFieldComponent, AlertDisplayComponent],
    templateUrl: "./move-ovta-assessments-modal.component.html",
    styleUrl: "./move-ovta-assessments-modal.component.scss",
})
export class MoveOvtaAssessmentsModalComponent implements OnInit {
    public ref: DialogRef<MoveOvtaAssessmentsModalContext, boolean> = inject(DialogRef);
    public FormFieldType = FormFieldType;
    public targetAreaOptions: FormInputOption[] = [];
    public isLoadingOptions = true;

    public formGroup = new FormGroup({
        TargetOnlandVisualTrashAssessmentAreaID: new FormControl<number | null>(null, [Validators.required]),
    });

    constructor(private onlandVisualTrashAssessmentAreaService: OnlandVisualTrashAssessmentAreaService, private alertService: AlertService) {}

    ngOnInit(): void {
        this.alertService.clearAlerts();
        this.onlandVisualTrashAssessmentAreaService.listByJurisdictionIDOnlandVisualTrashAssessmentArea(this.ref.data.SourceStormwaterJurisdictionID).subscribe({
            next: (areas) => {
                this.targetAreaOptions = areas
                    .filter((x) => x.OnlandVisualTrashAssessmentAreaID !== this.ref.data.SourceOnlandVisualTrashAssessmentAreaID)
                    .map((x) => ({
                        Value: x.OnlandVisualTrashAssessmentAreaID,
                        Label: x.OnlandVisualTrashAssessmentAreaName,
                        disabled: false,
                    }));
                this.isLoadingOptions = false;
            },
            error: () => {
                // httpErrorInterceptor surfaces the failure; close so the user can retry.
                this.ref.close(null);
            },
        });
    }

    save(): void {
        const dto: OnlandVisualTrashAssessmentAreaMoveAssessmentsDto = {
            TargetOnlandVisualTrashAssessmentAreaID: this.formGroup.controls.TargetOnlandVisualTrashAssessmentAreaID.value!,
        };
        this.onlandVisualTrashAssessmentAreaService.moveAssessmentsOnlandVisualTrashAssessmentArea(this.ref.data.SourceOnlandVisualTrashAssessmentAreaID, dto).subscribe({
            next: () => {
                this.alertService.clearAlerts();
                this.alertService.pushAlert(new Alert("Successfully moved assessments to the selected OVTA Area.", AlertContext.Success));
                this.ref.close(true);
            },
            // httpErrorInterceptor surfaces the failure alert; modal stays open so the user can retry.
            error: () => {},
        });
    }

    cancel(): void {
        this.ref.close(null);
    }
}

export class MoveOvtaAssessmentsModalContext {
    SourceOnlandVisualTrashAssessmentAreaID: number;
    SourceOnlandVisualTrashAssessmentAreaName: string;
    SourceStormwaterJurisdictionID: number;
    SourceAssessmentCount: number;
}
