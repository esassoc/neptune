import { Component, DestroyRef, inject, OnInit, signal } from "@angular/core";
import { ActivatedRoute, Router } from "@angular/router";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";

import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { BtnGroupRadioInputComponent, IBtnGroupRadioInputOption } from "src/app/shared/components/inputs/btn-group-radio-input/btn-group-radio-input.component";

import { FieldVisitsTabComponent } from "./tabs/field-visits-tab/field-visits-tab.component";
import { BmpRecordsTabComponent } from "./tabs/bmp-records-tab/bmp-records-tab.component";
import { DelineationsTabComponent } from "./tabs/delineations-tab/delineations-tab.component";

type DashboardTabKey = "field-visits" | "bmp-records" | "delineations";

@Component({
    selector: "dashboard",
    standalone: true,
    imports: [PageHeaderComponent, AlertDisplayComponent, BtnGroupRadioInputComponent, FieldVisitsTabComponent, BmpRecordsTabComponent, DelineationsTabComponent],
    templateUrl: "./dashboard.component.html",
    styleUrl: "./dashboard.component.scss",
})
export class DashboardComponent implements OnInit {
    private route = inject(ActivatedRoute);
    private router = inject(Router);
    private destroyRef = inject(DestroyRef);

    // Labels match the legacy MVC dashboard panel headers (Index.cshtml:36-91) so muscle-memory
    // and any external "go to the Provisional X tab" references still resolve. Tab `value` keys
    // stay short — they're the URL query-param values + the @switch keys in the template.
    // The route guard (ManagerOrAdminOnlyGuard) already restricts /dashboard to roles that all
    // qualify to see the Delineations tab, so no per-tab visibility gate is needed here.
    public readonly tabOptions: IBtnGroupRadioInputOption[] = [
        { label: "Provisional Assessment and Maintenance Records", value: "field-visits" },
        { label: "Provisional BMP Records", value: "bmp-records" },
        { label: "Provisional BMP Delineations", value: "delineations" },
    ];

    public activeTab = signal<DashboardTabKey>("field-visits");

    public get activeTabLabel(): string {
        return this.tabOptions.find((o) => o.value === this.activeTab())?.label ?? "";
    }

    ngOnInit(): void {
        this.route.queryParamMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params) => {
            const tab = params.get("tab") as DashboardTabKey | null;
            if (tab && this.tabOptions.some((o) => o.value === tab)) {
                this.activeTab.set(tab);
            }
        });
    }

    public selectTab(value: string): void {
        const key = value as DashboardTabKey;
        if (this.activeTab() === key) return;
        this.activeTab.set(key);
        this.router.navigate([], {
            relativeTo: this.route,
            queryParams: { tab: key },
            queryParamsHandling: "merge",
            replaceUrl: true,
        });
    }
}
