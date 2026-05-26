import { Component, computed, DestroyRef, inject, OnInit, signal, Signal } from "@angular/core";
import { ActivatedRoute, Router } from "@angular/router";
import { takeUntilDestroyed, toSignal } from "@angular/core/rxjs-interop";
import { map } from "rxjs";

import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { BtnGroupRadioInputComponent, IBtnGroupRadioInputOption } from "src/app/shared/components/inputs/btn-group-radio-input/btn-group-radio-input.component";
import { AuthenticationService } from "src/app/services/authentication.service";

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
    private authenticationService = inject(AuthenticationService);

    // AC item 10: Delineations tab is conditional on BMP delineation view permission. Editor+
    // role gates it; Admin/SitkaAdmin and Managers-with-assigned-jurisdiction qualify. Sourced
    // from currentUserSetObservable so the tab list reacts to impersonation (NPT-998 / -1064
    // zoneless pattern — service method calls don't trigger CD on their own).
    private currentUserSignal = toSignal(this.authenticationService.currentUserSetObservable.pipe(map((u) => u ?? null)), { initialValue: null });
    public canViewDelineationsTab: Signal<boolean> = computed(() => {
        const u = this.currentUserSignal();
        if (!u) return false;
        return this.authenticationService.doesCurrentUserHaveJurisdictionEditPermission();
    });

    // Labels match the legacy MVC dashboard panel headers (Index.cshtml:36-91) so muscle-memory
     // and any external "go to the Provisional X tab" references still resolve. Tab `value` keys
     // stay short — they're the URL query-param values + the @switch keys in the template.
    public tabOptions: Signal<IBtnGroupRadioInputOption[]> = computed(() => {
        const base: IBtnGroupRadioInputOption[] = [
            { label: "Provisional Assessment and Maintenance Records", value: "field-visits" },
            { label: "Provisional BMP Records", value: "bmp-records" },
        ];
        if (this.canViewDelineationsTab()) {
            base.push({ label: "Provisional BMP Delineations", value: "delineations" });
        }
        return base;
    });

    public activeTab = signal<DashboardTabKey>("field-visits");

    public get activeTabLabel(): string {
        return this.tabOptions().find((o) => o.value === this.activeTab())?.label ?? "";
    }

    ngOnInit(): void {
        this.route.queryParamMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params) => {
            const tab = params.get("tab") as DashboardTabKey | null;
            if (tab && this.tabOptions().some((o) => o.value === tab)) {
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
