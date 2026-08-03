import { Component, inject, OnInit } from "@angular/core";
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from "@angular/forms";
import { DialogRef } from "@ngneat/dialog";
import { FormFieldComponent, FormFieldType, FormInputOption } from "src/app/shared/components/forms/form-field/form-field.component";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { OnlandVisualTrashAssessmentAreaService } from "src/app/shared/generated/api/onland-visual-trash-assessment-area.service";
import { OnlandVisualTrashAssessmentAreaMoveAssessmentsDto } from "src/app/shared/generated/model/onland-visual-trash-assessment-area-move-assessments-dto";
import { AlertService } from "src/app/shared/services/alert.service";

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
            OnlandVisualTrashAssessmentIDs: this.ref.data.SelectedAssessmentIDs,
        };
        this.onlandVisualTrashAssessmentAreaService.moveAssessmentsOnlandVisualTrashAssessmentArea(this.ref.data.SourceOnlandVisualTrashAssessmentAreaID, dto).subscribe({
            next: () => {
                // Success alert is pushed by the caller after this modal closes — the modal's own
                // <app-alert-display> clears alerts on destroy, so pushing it here would not survive.
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
    SelectedAssessmentIDs: number[];
    SourceAssessmentCount: number;
}
