import { Component, Input } from "@angular/core";
import { AsyncPipe, DatePipe } from "@angular/common";
import { RouterLink } from "@angular/router";
import { forkJoin, Observable } from "rxjs";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { AuthenticationService } from "src/app/services/authentication.service";
import { WaterQualityManagementPlanService } from "src/app/shared/generated/api/water-quality-management-plan.service";
import { WaterQualityManagementPlanDto } from "src/app/shared/generated/model/water-quality-management-plan-dto";
import { WaterQualityManagementPlanVerifyDetailDto } from "src/app/shared/generated/model/water-quality-management-plan-verify-detail-dto";
import { environment } from "src/environments/environment";

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

    // NPT-995 rework: page title is the WQMP name with status as an italic subtitle.
    // The verify-detail DTO doesn't carry the WQMP name, so the page fetches both
    // resources in parallel and binds them together.
    public pageData$: Observable<{ wqmp: WaterQualityManagementPlanDto; verification: WaterQualityManagementPlanVerifyDetailDto }>;

    constructor(private wqmpService: WaterQualityManagementPlanService, private authenticationService: AuthenticationService) {}

    ngOnInit(): void {
        this.pageData$ = forkJoin({
            wqmp: this.wqmpService.getWaterQualityManagementPlan(this.waterQualityManagementPlanID),
            verification: this.wqmpService.getVerificationWaterQualityManagementPlan(this.waterQualityManagementPlanID, this.waterQualityManagementPlanVerifyID),
        });
    }

    public get currentPersonCanEdit(): boolean {
        return this.authenticationService.doesCurrentUserHaveJurisdictionEditPermission();
    }

    // NPT-995 Round 5: FileResource download URL — same pattern as the supporting-documentation step.
    public getDownloadUrl(guid: string): string {
        return `${environment.mainAppApiUrl}/FileResource/DisplayResource/${guid}`;
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
