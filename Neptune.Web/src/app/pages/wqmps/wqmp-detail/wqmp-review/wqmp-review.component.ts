import { Component, inject, Input, OnInit, signal } from "@angular/core";
import { DomSanitizer, SafeResourceUrl } from "@angular/platform-browser";
import { Router, RouterLink } from "@angular/router";
import { AsyncPipe } from "@angular/common";
import { Observable, tap, shareReplay, map as rxMap } from "rxjs";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { AlertService } from "src/app/shared/services/alert.service";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { WaterQualityManagementPlanService } from "src/app/shared/generated/api/water-quality-management-plan.service";
import { WaterQualityManagementPlanExtractionResultDto } from "src/app/shared/generated/model/water-quality-management-plan-extraction-result-dto";
import { WaterQualityManagementPlanUpsertDto } from "src/app/shared/generated/model/water-quality-management-plan-upsert-dto";
import { FieldCardComponent } from "src/app/pages/wqmps/wqmp-detail/wqmp-review/field-card/field-card.component";
import { environment } from "src/environments/environment";

export interface ExtractedField {
    key: string;
    label: string;
    value: string | null;
    evidence: string | null;
    source: string | null;
    step: number;
    acceptedValue?: string | null;
    state: "pending" | "accepted" | "edited" | "rejected";
}

@Component({
    selector: "wqmp-review",
    standalone: true,
    imports: [PageHeaderComponent, AlertDisplayComponent, FieldCardComponent, RouterLink, AsyncPipe],
    templateUrl: "./wqmp-review.component.html",
    styleUrl: "./wqmp-review.component.scss",
})
export class WqmpReviewComponent implements OnInit {
    @Input() waterQualityManagementPlanID!: number;

    private router = inject(Router);
    private wqmpService = inject(WaterQualityManagementPlanService);
    private alertService = inject(AlertService);
    private sanitizer = inject(DomSanitizer);

    public extractionResult$: Observable<WaterQualityManagementPlanExtractionResultDto>;
    public currentStep = signal(1);
    public fields = signal<ExtractedField[]>([]);
    public pdfUrl: SafeResourceUrl = "";
    public isApplying = false;

    public steps = [
        { number: 1, title: "Location", desc: "Jurisdiction & parcels" },
        { number: 2, title: "Basics", desc: "Project details & contacts" },
        { number: 3, title: "BMPs", desc: "Treatment controls", disabled: true },
        { number: 4, title: "Review", desc: "Final review & submit", disabled: true },
    ];

    // Fields per step
    private locationFields = ["Jurisdiction", "HydrologicSubarea", "RecordedWQMPAreaInAcres"];
    private basicsFields = [
        "WaterQualityManagementPlanName", "WaterQualityManagementPlanPriority", "WaterQualityManagementPlanDevelopmentType",
        "WaterQualityManagementPlanLandUse", "WaterQualityManagementPlanPermitTerm", "ApprovalDate", "DateOfConstruction",
        "RecordNumber", "TrashCaptureStatusType",
        "MaintenanceContactName", "MaintenanceContactOrganization", "MaintenanceContactPhone",
        "MaintenanceContactAddress1", "MaintenanceContactAddress2", "MaintenanceContactCity", "MaintenanceContactState", "MaintenanceContactZip",
    ];

    private fieldLabels: Record<string, string> = {
        WaterQualityManagementPlanName: "WQMP Name",
        Jurisdiction: "Jurisdiction",
        HydrologicSubarea: "Hydrologic Subarea",
        RecordedWQMPAreaInAcres: "Recorded WQMP Area (Acres)",
        WaterQualityManagementPlanPriority: "Priority",
        WaterQualityManagementPlanDevelopmentType: "Development Type",
        WaterQualityManagementPlanLandUse: "Land Use",
        WaterQualityManagementPlanPermitTerm: "Permit Term",
        ApprovalDate: "Approval Date",
        DateOfConstruction: "Date of Construction",
        RecordNumber: "Record Number",
        TrashCaptureStatusType: "Trash Capture Status",
        MaintenanceContactName: "Maintenance Contact Name",
        MaintenanceContactOrganization: "Maintenance Contact Organization",
        MaintenanceContactPhone: "Maintenance Contact Phone",
        MaintenanceContactAddress1: "Maintenance Contact Address 1",
        MaintenanceContactAddress2: "Maintenance Contact Address 2",
        MaintenanceContactCity: "Maintenance Contact City",
        MaintenanceContactState: "Maintenance Contact State",
        MaintenanceContactZip: "Maintenance Contact ZIP",
    };

    ngOnInit(): void {
        this.extractionResult$ = this.wqmpService
            .getExtractionResultWaterQualityManagementPlan(this.waterQualityManagementPlanID)
            .pipe(
                tap((result) => {
                    this.pdfUrl = this.sanitizer.bypassSecurityTrustResourceUrl(`${environment.mainAppApiUrl}/file-resources/${result.FileResourceGuid}`);
                    this.parseExtractionResult(result.ExtractionResultJson);
                }),
                shareReplay(1)
            );
    }

    goToStep(step: number): void {
        if (this.steps[step - 1]?.disabled) return;
        this.currentStep.set(step);
    }

    get fieldsForCurrentStep(): ExtractedField[] {
        return this.fields().filter((f) => f.step === this.currentStep());
    }

    get confirmedCount(): number {
        return this.fields().filter((f) => f.state === "accepted" || f.state === "edited").length;
    }

    get totalFieldCount(): number {
        return this.fields().length;
    }

    onFieldAccepted(field: ExtractedField, value: string | null): void {
        field.state = "accepted";
        field.acceptedValue = value;
        this.fields.update((f) => [...f]);
    }

    onFieldEdited(field: ExtractedField, value: string): void {
        field.state = "edited";
        field.acceptedValue = value;
        this.fields.update((f) => [...f]);
    }

    onFieldRejected(field: ExtractedField): void {
        field.state = "rejected";
        field.acceptedValue = null;
        this.fields.update((f) => [...f]);
    }

    applyToWqmp(): void {
        this.isApplying = true;
        this.alertService.clearAlerts();

        // Build UpsertDto from accepted/edited fields
        // Map extraction field keys to UpsertDto property names
        const fieldToDto: Record<string, string> = {
            WaterQualityManagementPlanName: "WaterQualityManagementPlanName",
            RecordNumber: "RecordNumber",
            RecordedWQMPAreaInAcres: "RecordedWQMPAreaInAcres",
            MaintenanceContactName: "MaintenanceContactName",
            MaintenanceContactOrganization: "MaintenanceContactOrganization",
            MaintenanceContactPhone: "MaintenanceContactPhone",
            MaintenanceContactAddress1: "MaintenanceContactAddress1",
            MaintenanceContactAddress2: "MaintenanceContactAddress2",
            MaintenanceContactCity: "MaintenanceContactCity",
            MaintenanceContactState: "MaintenanceContactState",
            MaintenanceContactZip: "MaintenanceContactZip",
            // TODO: Lookup fields (Jurisdiction, HydrologicSubarea, Priority, etc.) need
            // name-to-ID resolution before they can be mapped to the UpsertDto.
            // For now, only direct text fields are mapped.
        };
        const dto: any = {};
        for (const field of this.fields()) {
            if ((field.state === "accepted" || field.state === "edited") && fieldToDto[field.key]) {
                dto[fieldToDto[field.key]] = field.acceptedValue;
            }
        }

        this.wqmpService.updateWaterQualityManagementPlan(this.waterQualityManagementPlanID, dto).subscribe({
            next: () => {
                this.isApplying = false;
                this.alertService.pushAlert(new Alert("WQMP updated with extracted data.", AlertContext.Success));
                this.router.navigate(["/water-quality-management-plans", this.waterQualityManagementPlanID]);
            },
            error: () => {
                this.isApplying = false;
                this.alertService.pushAlert(new Alert("An error occurred while applying changes.", AlertContext.Danger));
            },
        });
    }

    cancel(): void {
        this.router.navigate(["/water-quality-management-plans", this.waterQualityManagementPlanID]);
    }

    private parseExtractionResult(json: string): void {
        try {
            const parsed = JSON.parse(json);
            const wqmp = parsed.WQMP || {};
            const allFields: ExtractedField[] = [];

            for (const key of this.locationFields) {
                allFields.push(this.makeField(key, wqmp[key], 1));
            }
            for (const key of this.basicsFields) {
                allFields.push(this.makeField(key, wqmp[key], 2));
            }

            this.fields.set(allFields);
        } catch {
            this.fields.set([]);
        }
    }

    private makeField(key: string, extracted: any, step: number): ExtractedField {
        return {
            key,
            label: this.fieldLabels[key] || key,
            value: extracted?.Value ?? null,
            evidence: extracted?.ExtractionEvidence ?? null,
            source: extracted?.DocumentSource ?? null,
            step,
            state: "pending",
        };
    }
}
