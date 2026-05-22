import { Component, DestroyRef, inject, OnInit, signal } from "@angular/core";
import { ActivatedRoute, Router } from "@angular/router";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { BtnGroupRadioInputComponent, IBtnGroupRadioInputOption } from "src/app/shared/components/inputs/btn-group-radio-input/btn-group-radio-input.component";
import { BmpsAndDelineationsTabComponent } from "./tabs/bmps-and-delineations-tab/bmps-and-delineations-tab.component";
import { WqmpsTabComponent } from "./tabs/wqmps-tab/wqmps-tab.component";
import { TrashModuleTabComponent } from "./tabs/trash-module-tab/trash-module-tab.component";
import { CountyGisTabComponent } from "./tabs/county-gis-tab/county-gis-tab.component";
import { DataHubTabKey } from "./components/data-hub-quick-links/data-hub-quick-links.component";

@Component({
    selector: "data-hub",
    standalone: true,
    imports: [PageHeaderComponent, BtnGroupRadioInputComponent, BmpsAndDelineationsTabComponent, WqmpsTabComponent, TrashModuleTabComponent, CountyGisTabComponent],
    templateUrl: "./data-hub.component.html",
    styleUrl: "./data-hub.component.scss",
})
export class DataHubComponent implements OnInit {
    private route = inject(ActivatedRoute);
    private router = inject(Router);
    private destroyRef = inject(DestroyRef);

    public tabOptions: IBtnGroupRadioInputOption[] = [
        { label: "BMPs and Delineations", value: "bmps" },
        { label: "WQMPs", value: "wqmps" },
        { label: "Trash Module", value: "trash" },
        { label: "County GIS Integration", value: "county" },
    ];

    public activeTab = signal<DataHubTabKey>("bmps");

    // NPT-998: BtnGroupRadioInputComponent's `[default]` is matched against label (not value),
    // so map the active value back to its label for the radio group binding. Same pattern as
    // FieldRecordsComponent — see feedback memory.
    public get activeTabLabel(): string {
        return this.tabOptions.find((o) => o.value === this.activeTab())?.label ?? "";
    }

    ngOnInit(): void {
        this.route.queryParamMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params) => {
            const tab = params.get("tab") as DataHubTabKey | null;
            if (tab && this.tabOptions.some((o) => o.value === tab)) {
                this.activeTab.set(tab);
            }
        });
    }

    public selectTab(value: string): void {
        const key = value as DataHubTabKey;
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
