import { Component, inject, OnInit } from "@angular/core";
import { Router, ActivatedRoute, RouterLink } from "@angular/router";
import { ReactiveFormsModule, FormGroup, FormArray, FormControl, Validators } from "@angular/forms";
import { AsyncPipe } from "@angular/common";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { FormFieldComponent, FormFieldType, SelectDropdownOption } from "src/app/shared/components/forms/form-field/form-field.component";
import { AlertService } from "src/app/shared/services/alert.service";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { forkJoin, map, Observable, shareReplay, tap } from "rxjs";
import { WaterQualityManagementPlanService } from "src/app/shared/generated/api/water-quality-management-plan.service";
import { SourceControlBMPUpsertDto } from "src/app/shared/generated/model/source-control-bmp-upsert-dto";
import { routeParams } from "src/app/app.routes";

@Component({
    selector: "edit-source-control-bmps",
    imports: [AlertDisplayComponent, PageHeaderComponent, RouterLink, ReactiveFormsModule, FormFieldComponent, AsyncPipe],
    templateUrl: "./edit-source-control-bmps.component.html",
    styleUrls: ["./edit-source-control-bmps.component.scss"],
})
export class EditSourceControlBMPsComponent implements OnInit {
    private router = inject(Router);
    private route = inject(ActivatedRoute);
    private wqmpService = inject(WaterQualityManagementPlanService);
    private alertService = inject(AlertService);

    public FormFieldType = FormFieldType;
    public waterQualityManagementPlanID: number;
    public sourceControlRows = new FormArray<FormGroup>([]);
    public groupedAttributes: { category: string; rows: FormGroup[]; collapsed: boolean }[] = [];
    public isPresentOptions: SelectDropdownOption[] = [
        { Value: true, Label: "Yes", disabled: false },
        { Value: false, Label: "No", disabled: false },
    ];
    public validationErrors: string[] = [];
    public loaded$: Observable<boolean>;

    ngOnInit(): void {
        this.alertService.clearAlerts();
        this.waterQualityManagementPlanID = +this.route.snapshot.paramMap.get(routeParams.waterQualityManagementPlanID);

        this.loaded$ = forkJoin({
            attributes: this.wqmpService.listSourceControlBMPAttributesWaterQualityManagementPlan(),
            existing: this.wqmpService.listSourceControlBMPsWaterQualityManagementPlan(this.waterQualityManagementPlanID),
        }).pipe(
            tap(({ attributes, existing }) => {
                const existingByAttrID = new Map(existing.map((e) => [e.SourceControlBMPAttributeID, e]));

                for (const attr of attributes) {
                    const existingData = existingByAttrID.get(attr.SourceControlBMPAttributeID);
                    const row = new FormGroup({
                        SourceControlBMPAttributeID: new FormControl<number>(attr.SourceControlBMPAttributeID),
                        SourceControlBMPAttributeName: new FormControl<string>(attr.SourceControlBMPAttributeName),
                        SourceControlBMPAttributeCategoryName: new FormControl<string>(attr.SourceControlBMPAttributeCategoryName),
                        SourceControlBMPAttributeCategoryID: new FormControl<number>(attr.SourceControlBMPAttributeCategoryID),
                        IsPresent: new FormControl<boolean>(existingData?.IsPresent ?? null),
                        SourceControlBMPNote: new FormControl<string>(existingData?.SourceControlBMPNote ?? "", { validators: [Validators.maxLength(200)] }),
                    });
                    this.sourceControlRows.push(row);
                }

                this.buildGroups();
            }),
            map(() => true),
            shareReplay(1)
        );
    }

    private buildGroups(): void {
        const groupMap = new Map<string, FormGroup[]>();
        for (const row of this.sourceControlRows.controls) {
            const category = row.get("SourceControlBMPAttributeCategoryName").value;
            if (!groupMap.has(category)) {
                groupMap.set(category, []);
            }
            groupMap.get(category).push(row);
        }
        this.groupedAttributes = Array.from(groupMap.entries()).map(([category, rows]) => ({ category, rows, collapsed: true }));
    }

    public toggleCategory(group: { collapsed: boolean }): void {
        group.collapsed = !group.collapsed;
    }

    public save(): void {
        if (this.sourceControlRows.invalid) {
            this.sourceControlRows.markAllAsTouched();
            this.alertService.pushAlert(new Alert("Please complete the highlighted required fields before saving.", AlertContext.Danger));
            return;
        }
        this.validationErrors = [];
        const noteMaxLength = 200;

        const dtos: SourceControlBMPUpsertDto[] = this.sourceControlRows.controls
            .filter((row) => {
                const isPresent = row.get("IsPresent").value;
                const note = row.get("SourceControlBMPNote").value;
                return isPresent != null || (note && note.trim());
            })
            .map((row) => {
                return new SourceControlBMPUpsertDto({
                    SourceControlBMPAttributeID: row.get("SourceControlBMPAttributeID").value,
                    SourceControlBMPAttributeCategoryID: row.get("SourceControlBMPAttributeCategoryID").value,
                    SourceControlBMPAttributeCategoryName: row.get("SourceControlBMPAttributeCategoryName").value,
                    SourceControlBMPAttributeName: row.get("SourceControlBMPAttributeName").value,
                    IsPresent: row.get("IsPresent").value ?? null,
                    SourceControlBMPNote: row.get("SourceControlBMPNote").value || null,
                });
            });

        for (const dto of dtos) {
            if (dto.SourceControlBMPNote && dto.SourceControlBMPNote.length > noteMaxLength) {
                this.validationErrors.push(`"${dto.SourceControlBMPAttributeName}"'s note exceeds the maximum of ${noteMaxLength} characters.`);
            }
        }

        if (this.validationErrors.length > 0) {
            return;
        }

        this.wqmpService.mergeSourceControlBMPsWaterQualityManagementPlan(this.waterQualityManagementPlanID, dtos).subscribe(() => {
            this.alertService.pushAlert(new Alert("Source control BMPs updated successfully.", AlertContext.Success));
            this.router.navigate(["/water-quality-management-plans", this.waterQualityManagementPlanID]);
        });
    }

    public cancel(): void {
        this.router.navigate(["/water-quality-management-plans", this.waterQualityManagementPlanID]);
    }
}
