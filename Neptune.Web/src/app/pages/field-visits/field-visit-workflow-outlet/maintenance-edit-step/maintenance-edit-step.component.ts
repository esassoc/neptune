import { Component, OnInit, signal } from "@angular/core";
import { Router } from "@angular/router";
import { AsyncPipe } from "@angular/common";
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from "@angular/forms";
import { combineLatest, finalize, map, Observable, switchMap, take } from "rxjs";

import { FormFieldComponent, FormFieldType, SelectDropdownOption } from "src/app/shared/components/forms/form-field/form-field.component";
import { LoadingDirective } from "src/app/shared/directives/loading.directive";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";

import { FieldVisitWorkflowDto } from "src/app/shared/generated/model/field-visit-workflow-dto";
import { MaintenanceRecordService } from "src/app/shared/generated/api/maintenance-record.service";
import { MaintenanceRecordDetailDto } from "src/app/shared/generated/model/maintenance-record-detail-dto";
import { MaintenanceRecordUpsertDto } from "src/app/shared/generated/model/maintenance-record-upsert-dto";
import { MaintenanceRecordObservationUpsertDto } from "src/app/shared/generated/model/maintenance-record-observation-upsert-dto";
import { TreatmentBMPTypeService } from "src/app/shared/generated/api/treatment-bmp-type.service";
import { TreatmentBMPTypeCustomAttributeTypeDto } from "src/app/shared/generated/model/treatment-bmp-type-custom-attribute-type-dto";
import { CustomAttributeTypePurposeEnum } from "src/app/shared/generated/enum/custom-attribute-type-purpose-enum";
import { CustomAttributeDataTypeEnum } from "src/app/shared/generated/enum/custom-attribute-data-type-enum";
import { MaintenanceRecordTypes, MaintenanceRecordTypesAsSelectDropdownOptions } from "src/app/shared/generated/enum/maintenance-record-type-enum";

import { FieldVisitWorkflowService } from "../../services/field-visit-workflow.service";
import { AlertService } from "src/app/shared/services/alert.service";
import { AuthenticationService } from "src/app/services/authentication.service";
import { ConfirmService } from "src/app/shared/services/confirm/confirm.service";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";

interface ObservationField {
    customAttributeTypeID: number;
    name: string;
    description?: string;
    isRequired: boolean;
    sortOrder: number;
    dataTypeID: number;
    /** Configured unit label (e.g. "sq ft", "cu yd") shown to the right of numeric inputs. */
    unitLabel: string;
    /** Per-data-type display info — drives which input the template renders. */
    dataTypeKind: "text" | "integer" | "decimal" | "date" | "pick-one" | "multi-select";
    options: SelectDropdownOption[];
    /** Single-value input control (text / integer / decimal / date / pick-one). */
    control: FormControl<string | null>;
    /** Multi-select input control (string list). */
    multiControl: FormControl<string[] | null>;
}

@Component({
    selector: "field-visit-maintenance-edit-step",
    standalone: true,
    imports: [AsyncPipe, ReactiveFormsModule, FormFieldComponent, LoadingDirective, PageHeaderComponent],
    templateUrl: "./maintenance-edit-step.component.html",
    styleUrl: "./maintenance-edit-step.component.scss",
})
export class FieldVisitMaintenanceEditStepComponent implements OnInit {
    public workflow$: Observable<FieldVisitWorkflowDto | null>;
    // Signals — plain fields don't reliably trigger CD when mutated inside subscribe callbacks
    // under zoneless behavior, leaving the spinner stuck until a stray click forces a render.
    public maintenanceRecord = signal<MaintenanceRecordDetailDto | null>(null);
    public observationFields = signal<ObservationField[]>([]);
    public isLoading = signal(true);
    public isReadOnly = signal(false);
    public isSaving = signal(false);
    public FormFieldType = FormFieldType;
    public maintenanceRecordTypeOptions: SelectDropdownOption[] = MaintenanceRecordTypesAsSelectDropdownOptions;

    public formGroup = new FormGroup({
        MaintenanceRecordTypeID: new FormControl<number | null>(null, { validators: [Validators.required] }),
        MaintenanceRecordDescription: new FormControl<string | null>(""),
        // Per-observation controls are added dynamically once the BMP type's attribute schema loads;
        // see buildObservationField. Keeping them under a nested group means formGroup.invalid +
        // [disabled] on the Save button correctly reflect required observation inputs.
        Observations: new FormGroup<{ [key: string]: FormControl<any> }>({}),
    });

    constructor(
        private workflowService: FieldVisitWorkflowService,
        private maintenanceRecordService: MaintenanceRecordService,
        private treatmentBMPTypeService: TreatmentBMPTypeService,
        private alertService: AlertService,
        private authenticationService: AuthenticationService,
        private confirmService: ConfirmService,
        private router: Router
    ) {}

    // NPT-984: Delete Maintenance Record is Manager-only (backend tightened to
    // JurisdictionManageFeature). Editor performs the maintenance; only Manager can delete it.
    public get canManage(): boolean {
        return this.authenticationService.doesCurrentUserHaveJurisdictionManagePermission();
    }

    ngOnInit(): void {
        this.workflow$ = this.workflowService.workflow$;
        this.workflowService.clearStepAlerts();
        this.workflowService.workflow$
            .pipe(
                take(1),
                switchMap((workflow) => {
                    if (!workflow) throw new Error("Field Visit not loaded");
                    return combineLatest({
                        record: this.maintenanceRecordService.getByFieldVisitMaintenanceRecord(workflow.FieldVisitID),
                        attributeTypes: this.treatmentBMPTypeService.listCustomAttributeTypesTreatmentBMPType(workflow.TreatmentBMPTypeID),
                    }).pipe(map((r) => ({ workflow, ...r })));
                })
            )
            .subscribe(({ workflow, record, attributeTypes }) => {
                this.maintenanceRecord.set(record);
                this.isReadOnly.set(this.workflowService.isReadOnly(workflow));
                this.formGroup.patchValue({
                    MaintenanceRecordTypeID: record?.MaintenanceRecordTypeID ?? null,
                    MaintenanceRecordDescription: record?.MaintenanceRecordDescription ?? "",
                });

                const maintenanceTypes = (attributeTypes ?? []).filter(
                    (t) => t.CustomAttributeType?.CustomAttributeTypePurposeID === CustomAttributeTypePurposeEnum.Maintenance
                );
                // Reset the dynamic Observations sub-group before re-populating in case the load fires twice.
                const observationsGroup = this.formGroup.controls.Observations;
                Object.keys(observationsGroup.controls).forEach((k) => observationsGroup.removeControl(k));

                const fields = maintenanceTypes
                    .map((t) => this.buildObservationField(t, record))
                    .sort((a, b) => a.sortOrder - b.sortOrder);
                this.observationFields.set(fields);

                // Register each field's active control so formGroup.invalid reflects required observations.
                for (const field of fields) {
                    const active = field.dataTypeKind === "multi-select" ? field.multiControl : field.control;
                    observationsGroup.addControl(`${field.customAttributeTypeID}`, active);
                }
                if (this.isReadOnly()) {
                    this.formGroup.disable();
                }
                this.isLoading.set(false);
            });
    }

    private buildObservationField(typeAttr: TreatmentBMPTypeCustomAttributeTypeDto, record: MaintenanceRecordDetailDto | null): ObservationField {
        const attr = typeAttr.CustomAttributeType;
        const existing = record?.Observations?.find((o) => o.CustomAttributeTypeID === typeAttr.CustomAttributeTypeID);
        const existingValues = (existing?.Values ?? []).map((v) => v.ObservationValue ?? "").filter((v) => v.length > 0);
        const dataTypeID = attr?.CustomAttributeDataTypeID ?? CustomAttributeDataTypeEnum.String;
        const dataTypeKind = this.mapDataTypeKind(dataTypeID);
        const options = this.parseOptions(attr?.CustomAttributeTypeOptionsSchema ?? null);

        const isRequired = attr?.IsRequired ?? false;
        const required = isRequired ? [Validators.required] : [];

        const initialSingle = existingValues[0] ?? "";

        return {
            customAttributeTypeID: typeAttr.CustomAttributeTypeID,
            name: attr?.CustomAttributeTypeName ?? "Observation",
            description: attr?.CustomAttributeTypeDescription,
            isRequired,
            sortOrder: typeAttr.SortOrder ?? 0,
            dataTypeID,
            unitLabel: attr?.MeasurementUnitDisplayName ?? "",
            dataTypeKind,
            options,
            control: new FormControl<string | null>(initialSingle, { validators: required }),
            multiControl: new FormControl<string[] | null>(existingValues, { validators: required }),
        };
    }

    private mapDataTypeKind(dataTypeID: number): ObservationField["dataTypeKind"] {
        switch (dataTypeID) {
            case CustomAttributeDataTypeEnum.Integer:
                return "integer";
            case CustomAttributeDataTypeEnum.Decimal:
                return "decimal";
            case CustomAttributeDataTypeEnum.DateTime:
                return "date";
            case CustomAttributeDataTypeEnum.PickFromList:
                return "pick-one";
            case CustomAttributeDataTypeEnum.MultiSelect:
                return "multi-select";
            case CustomAttributeDataTypeEnum.String:
            default:
                return "text";
        }
    }

    private parseOptions(schema: string | null): SelectDropdownOption[] {
        if (!schema) return [];
        try {
            const parsed = JSON.parse(schema);
            if (!Array.isArray(parsed)) return [];
            return parsed.map((opt: string) => ({ Value: opt, Label: opt, disabled: false }));
        } catch {
            return [];
        }
    }

    save(workflow: FieldVisitWorkflowDto, nextAction: "stay" | "continue" | "wrap-up" = "continue"): void {
        // Defensive double-submit guard. The previous version had no in-flight check, so a fast
        // second click during the (visible) Save & Continue → navigate window could push the
        // success alert twice — Kathleen flagged this as the duplicate-alert symptom.
        if (this.isSaving()) return;
        const record = this.maintenanceRecord();
        if (this.formGroup.invalid || !record) return;

        this.workflowService.clearStepAlerts();
        this.isSaving.set(true);

        const observations: MaintenanceRecordObservationUpsertDto[] = this.observationFields()
            .map((field) => ({
                CustomAttributeTypeID: field.customAttributeTypeID,
                Values: this.collectFieldValues(field),
            }))
            .filter((o) => o.Values.length > 0);

        const dto = new MaintenanceRecordUpsertDto({
            MaintenanceRecordTypeID: this.formGroup.controls.MaintenanceRecordTypeID.value!,
            MaintenanceRecordDescription: this.formGroup.controls.MaintenanceRecordDescription.value ?? "",
            Observations: observations,
        });

        // Chain update -> refresh and use finalize so isSaving always resets, regardless of whether
        // the refresh leg errors out (transient network / 401 / etc.). Previously isSaving was only
        // cleared in the success branch of refresh().subscribe(), so a failed refresh after a
        // successful save left the footer permanently disabled.
        this.maintenanceRecordService
            .updateMaintenanceRecord(record.MaintenanceRecordID, dto)
            .pipe(
                switchMap(() => {
                    this.alertService.pushAlert(new Alert("Maintenance Record saved.", AlertContext.Success));
                    return this.workflowService.refresh();
                }),
                finalize(() => this.isSaving.set(false))
            )
            .subscribe({
                next: () => {
                    if (nextAction === "continue") {
                        this.router.navigate(["/field-visits", workflow.FieldVisitID, "post-maintenance-assessment"]);
                    } else if (nextAction === "wrap-up") {
                        this.workflowService.wrapUpVisit(workflow.FieldVisitID);
                    }
                },
            });
    }

    onMultiSelectToggle(field: ObservationField, value: string, checked: boolean): void {
        const current = field.multiControl.value ?? [];
        const next = checked ? [...new Set([...current, value])] : current.filter((v) => v !== value);
        field.multiControl.setValue(next);
        field.multiControl.markAsDirty();
    }

    private collectFieldValues(field: ObservationField): string[] {
        if (field.dataTypeKind === "multi-select") {
            const values = field.multiControl.value ?? [];
            return values.filter((v) => v && v.trim().length > 0);
        }
        const single = field.control.value ?? "";
        return single.trim().length > 0 ? [single.trim()] : [];
    }

    deleteRecord(workflow: FieldVisitWorkflowDto): void {
        const record = this.maintenanceRecord();
        if (!record) return;
        const recordID = record.MaintenanceRecordID;
        this.confirmService
            .confirm({
                title: "Delete Maintenance Record",
                message: "Are you sure you want to delete the Maintenance Record for this Field Visit? This will remove all entered observations.",
                buttonClassYes: "btn btn-danger",
                buttonTextYes: "Delete",
                buttonTextNo: "Cancel",
            })
            .then((confirmed) => {
                if (!confirmed) return;
                this.maintenanceRecordService.deleteMaintenanceRecord(recordID).subscribe(() => {
                    this.alertService.pushAlert(new Alert("Maintenance Record deleted.", AlertContext.Success));
                    this.workflowService.refresh().subscribe(() => {
                        this.router.navigate(["/field-visits", workflow.FieldVisitID, "maintenance"]);
                    });
                });
            });
    }
}
