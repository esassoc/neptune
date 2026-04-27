import { Component, Input } from "@angular/core";
import { AsyncPipe, DatePipe } from "@angular/common";
import { RouterLink } from "@angular/router";
import { Observable } from "rxjs";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { AuthenticationService } from "src/app/services/authentication.service";
import { WaterQualityManagementPlanService } from "src/app/shared/generated/api/water-quality-management-plan.service";
import { WaterQualityManagementPlanVerifyDetailDto } from "src/app/shared/generated/model/water-quality-management-plan-verify-detail-dto";

@Component({
    selector: "verification-detail",
    standalone: true,
    imports: [AsyncPipe, DatePipe, RouterLink, PageHeaderComponent, AlertDisplayComponent],
    templateUrl: "./verification-detail.component.html",
    styleUrl: "./verification-detail.component.scss",
})
export class VerificationDetailComponent {
    @Input() waterQualityManagementPlanID!: number;
    @Input() waterQualityManagementPlanVerifyID!: number;

    public verification$: Observable<WaterQualityManagementPlanVerifyDetailDto>;

    constructor(private wqmpService: WaterQualityManagementPlanService, private authenticationService: AuthenticationService) {}

    ngOnInit(): void {
        this.verification$ = this.wqmpService.getVerificationWaterQualityManagementPlan(this.waterQualityManagementPlanID, this.waterQualityManagementPlanVerifyID);
    }

    public get currentPersonCanEdit(): boolean {
        return this.authenticationService.doesCurrentUserHaveJurisdictionEditPermission();
    }

    public groupSourceControlsByCategory(rows: WaterQualityManagementPlanVerifyDetailDto["SourceControlBMPs"]): { category: string; items: WaterQualityManagementPlanVerifyDetailDto["SourceControlBMPs"] }[] {
        const map = new Map<string, WaterQualityManagementPlanVerifyDetailDto["SourceControlBMPs"]>();
        for (const row of rows ?? []) {
            const key = row.SourceControlBMPAttributeCategoryName ?? "Other";
            if (!map.has(key)) map.set(key, []);
            map.get(key)!.push(row);
        }
        return Array.from(map.entries()).map(([category, items]) => ({ category, items }));
    }
}
