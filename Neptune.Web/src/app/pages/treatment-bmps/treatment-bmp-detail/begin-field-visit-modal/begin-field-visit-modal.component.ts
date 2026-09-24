import { Component, DestroyRef, inject, OnInit, signal } from "@angular/core";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { AsyncPipe } from "@angular/common";
import { HttpErrorResponse } from "@angular/common/http";
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from "@angular/forms";
import { DialogRef } from "@ngneat/dialog";
import { NgSelectModule } from "@ng-select/ng-select";
import { catchError, map, Observable, of, shareReplay, switchMap, tap } from "rxjs";
import { escapeHtml } from "src/app/shared/helpers/html-escape";
import { todayLocalDateString } from "src/app/shared/helpers/local-date";

import { FormFieldComponent, FormFieldType } from "src/app/shared/components/forms/form-field/form-field.component";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { AlertService } from "src/app/shared/services/alert.service";

import { FieldVisitService } from "src/app/shared/generated/api/field-visit.service";
import { TreatmentBMPService } from "src/app/shared/generated/api/treatment-bmp.service";
import { FieldVisitCreateDto, FieldVisitCreateDtoForm, FieldVisitCreateDtoFormControls } from "src/app/shared/generated/model/field-visit-create-dto";
import { FieldVisitDto } from "src/app/shared/generated/model/field-visit-dto";
import { TreatmentBMPMinimalDto } from "src/app/shared/generated/model/treatment-bmp-minimal-dto";
import { FieldVisitTypesAsSelectDropdownOptions, FieldVisitTypeEnum } from "src/app/shared/generated/enum/field-visit-type-enum";

import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";

export interface BeginFieldVisitModalContext {
    // NPT-1122: omit treatmentBMPID to open in picker mode (Field Records "Start Field Visit"); the
    // in-progress visit is then looked up reactively when a BMP is selected.
    treatmentBMPID?: number | null;
    inProgressFieldVisit?: FieldVisitDto | null;
}

@Component({
    selector: "begin-field-visit-modal",
    standalone: true,
    imports: [ReactiveFormsModule, FormFieldComponent, AlertDisplayComponent, NgSelectModule, AsyncPipe],
    templateUrl: "./begin-field-visit-modal.component.html",
    styleUrl: "./begin-field-visit-modal.component.scss",
})
export class BeginFieldVisitModalComponent implements OnInit {
    public ref: DialogRef<BeginFieldVisitModalContext, FieldVisitDto | null> = inject(DialogRef);
    private treatmentBMPService = inject(TreatmentBMPService);
    private destroyRef = inject(DestroyRef);
    public FormFieldType = FormFieldType;
    public fieldVisitTypeOptions = FieldVisitTypesAsSelectDropdownOptions;

    public continueOptions = [
        { Label: "Continue the in-progress visit", Value: true, disabled: false },
        { Label: "Start a new visit (the in-progress visit will be marked Unresolved)", Value: false, disabled: false },
    ];

    public formGroup = new FormGroup<FieldVisitCreateDtoForm>({
        VisitDate: FieldVisitCreateDtoFormControls.VisitDate(todayLocalDateString(), { validators: [Validators.required] }),
        FieldVisitTypeID: FieldVisitCreateDtoFormControls.FieldVisitTypeID(FieldVisitTypeEnum.DryWeather, { validators: [Validators.required] }),
        ContinueExistingInProgress: FieldVisitCreateDtoFormControls.ContinueExistingInProgress(),
    });

    // Picker mode only. Kept outside formGroup since it isn't part of FieldVisitCreateDto (it's the route param).
    public showBMPPicker = false;
    public treatmentBMPControl = new FormControl<number | null>(null, { validators: [Validators.required] });
    public treatmentBMPs$: Observable<TreatmentBMPMinimalDto[]>;

    // Signals: in picker mode these change after async lookups, which zoneless CD won't pick up from plain fields.
    public hasInProgressVisit = signal(false);
    public isCheckingInProgress = signal(false);
    public inProgressCheckFailed = signal(false);
    public selectedTreatmentBMPID = signal<number | null>(null);
    private inProgressFieldVisit: FieldVisitDto | null = null;

    public isSaving = false;

    constructor(private fieldVisitService: FieldVisitService, private alertService: AlertService) {}

    // Name-only, case-insensitive, match-anywhere search (NPT-1122 AC 4). bindLabel alone isn't enough:
    // without a searchFn, ng-select matches String(item) (see NPT-1109 in edit-treatment-bmps-modal).
    public searchTreatmentBMPs = (term: string, item: TreatmentBMPMinimalDto): boolean => (item.TreatmentBMPName ?? "").toLowerCase().includes(term.toLowerCase());

    ngOnInit(): void {
        this.alertService.clearAlerts();
        const ctx = this.ref.data;
        this.showBMPPicker = !ctx?.treatmentBMPID;

        // When the user flips to "Start a new visit", reset Date + Type back to fresh defaults so
        // they don't accidentally clone the in-progress visit's metadata. Flip the other way and
        // we restore the in-progress values.
        this.formGroup.controls.ContinueExistingInProgress.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((value) => {
            if (!this.inProgressFieldVisit) return;
            if (value === false) {
                this.resetDateAndType();
            } else if (value === true) {
                this.fillDateAndTypeFrom(this.inProgressFieldVisit);
            }
        });

        if (this.showBMPPicker) {
            this.treatmentBMPs$ = this.treatmentBMPService.listForPickerTreatmentBMP().pipe(shareReplay(1));
            this.treatmentBMPControl.valueChanges
                .pipe(
                    tap((treatmentBMPID) => {
                        this.selectedTreatmentBMPID.set(treatmentBMPID ?? null);
                        this.isCheckingInProgress.set(treatmentBMPID != null);
                        if (this.inProgressCheckFailed()) {
                            this.inProgressCheckFailed.set(false);
                            this.alertService.clearAlerts();
                        }
                    }),
                    switchMap((treatmentBMPID) =>
                        treatmentBMPID == null
                            ? of({ failed: false, inProgress: null as FieldVisitDto | null })
                            : this.fieldVisitService.getInProgressForTreatmentBMPFieldVisit(treatmentBMPID).pipe(
                                  // "No in-progress visit" is a successful null/204, not an error — only a failed
                                  // request lands here. Treating a failure as "none" would send
                                  // ContinueExistingInProgress: null for a BMP that may have one, which trips the
                                  // one-in-progress-per-BMP unique index server-side as a raw 500.
                                  map((inProgress) => ({ failed: false, inProgress: inProgress ?? null })),
                                  catchError(() => of({ failed: true, inProgress: null as FieldVisitDto | null }))
                              )
                    ),
                    takeUntilDestroyed(this.destroyRef)
                )
                .subscribe(({ failed, inProgress }) => {
                    this.isCheckingInProgress.set(false);
                    this.applyInProgressVisit(inProgress);
                    if (failed) {
                        // Block Start until a check succeeds; reselecting the BMP retries.
                        this.inProgressCheckFailed.set(true);
                        this.alertService.pushAlert(
                            new Alert("Couldn't check whether this BMP has an in-progress visit. Please reselect it or try again.", AlertContext.Danger)
                        );
                    }
                });
        } else {
            this.applyInProgressVisit(ctx.inProgressFieldVisit ?? null);
        }
    }

    public get canStart(): boolean {
        return !this.isSaving && !this.isCheckingInProgress() && !this.inProgressCheckFailed() && (!this.showBMPPicker || this.selectedTreatmentBMPID() != null);
    }

    save(): void {
        // Surface the invalid state so required-field errors light up (Kathleen's "click does
        // nothing" report). Per NPT-1029, modals rely on field-level highlights rather than
        // pushing a global danger alert, so just markAllAsTouched and return.
        if (this.formGroup.invalid || (this.showBMPPicker && this.treatmentBMPControl.invalid)) {
            this.formGroup.markAllAsTouched();
            this.treatmentBMPControl.markAsTouched();
            return;
        }

        this.alertService.clearAlerts();
        this.isSaving = true;

        // Strict boolean: the radio form control should hold true/false, but defend against string
        // coercion from older ng versions or odd ValueAccessor wiring by normalizing here.
        const continueRaw = this.formGroup.controls.ContinueExistingInProgress.value;
        const continueExistingInProgress = this.hasInProgressVisit() ? continueRaw === true || (continueRaw as unknown) === "true" : null;

        const dto = new FieldVisitCreateDto({
            VisitDate: this.formGroup.controls.VisitDate.value,
            FieldVisitTypeID: this.formGroup.controls.FieldVisitTypeID.value,
            ContinueExistingInProgress: continueExistingInProgress,
        });

        const treatmentBMPID = this.ref.data?.treatmentBMPID ?? this.treatmentBMPControl.value;
        this.fieldVisitService.createFieldVisit(treatmentBMPID, dto).subscribe({
            next: (result) => {
                this.isSaving = false;
                this.alertService.pushAlert(new Alert("Field Visit started.", AlertContext.Success));
                this.ref.close(result);
            },
            error: (err: HttpErrorResponse) => {
                this.isSaving = false;
                // NPT-984: server failures (DB constraint hits, 4xx from the API) were being
                // swallowed as a generic "Failed to start" message — Kathleen's 5/14 retest
                // couldn't see why the "Start new field visit" path was failing. Prefer the
                // structured ProblemDetails / message fields, then string error bodies, then
                // the generic fallback. Alerts render via [innerHTML] so escape any server text.
                const raw = (err?.error?.detail as string | undefined)
                    ?? (err?.error?.title as string | undefined)
                    ?? (err?.error?.message as string | undefined)
                    ?? (typeof err?.error === "string" ? (err.error as string) : null)
                    ?? "Failed to start the Field Visit. Please try again.";
                this.alertService.pushAlert(new Alert(escapeHtml(raw), AlertContext.Danger));
            },
        });
    }

    cancel(): void {
        this.ref.close(null);
    }

    private applyInProgressVisit(inProgress: FieldVisitDto | null): void {
        const hadInProgress = this.inProgressFieldVisit != null;
        this.inProgressFieldVisit = inProgress;
        this.hasInProgressVisit.set(!!inProgress);
        if (inProgress) {
            // Default to continuing the existing visit; pre-fill date and type from the in-progress
            // visit so the user can see what they'd be continuing.
            this.formGroup.controls.ContinueExistingInProgress.setValue(true, { emitEvent: false });
            this.fillDateAndTypeFrom(inProgress);
        } else {
            // Picker mode: switching away from a BMP with an in-progress visit hides the choice and drops the
            // in-progress date/type (AC 12); a date the user picked between two BMPs without one is kept.
            this.formGroup.controls.ContinueExistingInProgress.setValue(null, { emitEvent: false });
            if (hadInProgress) this.resetDateAndType();
        }
    }

    private fillDateAndTypeFrom(visit: FieldVisitDto): void {
        this.formGroup.controls.VisitDate.setValue(this.formatDateInputValue(visit.VisitDate));
        this.formGroup.controls.FieldVisitTypeID.setValue(visit.FieldVisitTypeID);
    }

    private resetDateAndType(): void {
        this.formGroup.controls.VisitDate.setValue(todayLocalDateString());
        this.formGroup.controls.FieldVisitTypeID.setValue(FieldVisitTypeEnum.DryWeather);
    }

    private formatDateInputValue(value: string | Date): string {
        if (!value) return todayLocalDateString();
        const d = typeof value === "string" ? new Date(value) : value;
        if (Number.isNaN(d.getTime())) return todayLocalDateString();
        return d.toISOString().slice(0, 10);
    }
}
