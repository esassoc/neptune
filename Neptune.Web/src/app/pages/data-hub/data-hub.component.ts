import { Component, DestroyRef, inject, OnInit, signal } from "@angular/core";
import { ActivatedRoute, Router } from "@angular/router";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { BmpsAndDelineationsTabComponent } from "./tabs/bmps-and-delineations-tab/bmps-and-delineations-tab.component";
import { WqmpsTabComponent } from "./tabs/wqmps-tab/wqmps-tab.component";
import { TrashModuleTabComponent } from "./tabs/trash-module-tab/trash-module-tab.component";
import { CountyGisTabComponent } from "./tabs/county-gis-tab/county-gis-tab.component";
import { DataHubTabKey } from "./components/data-hub-quick-links/data-hub-quick-links.component";

interface TabDef {
    key: DataHubTabKey;
    label: string;
}

@Component({
    selector: "data-hub",
    standalone: true,
    imports: [PageHeaderComponent, BmpsAndDelineationsTabComponent, WqmpsTabComponent, TrashModuleTabComponent, CountyGisTabComponent],
    templateUrl: "./data-hub.component.html",
    styleUrl: "./data-hub.component.scss",
})
export class DataHubComponent implements OnInit {
    private route = inject(ActivatedRoute);
    private router = inject(Router);
    private destroyRef = inject(DestroyRef);

    public tabs: TabDef[] = [
        { key: "bmps", label: "BMPs and Delineations" },
        { key: "wqmps", label: "WQMPs" },
        { key: "trash", label: "Trash Module" },
        { key: "county", label: "County GIS Integration" },
    ];

    public activeTab = signal<DataHubTabKey>("bmps");

    ngOnInit(): void {
        this.route.queryParamMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params) => {
            const tab = params.get("tab") as DataHubTabKey | null;
            if (tab && this.tabs.some((t) => t.key === tab)) {
                this.activeTab.set(tab);
            }
        });
    }

    public selectTab(key: DataHubTabKey): void {
        this.activeTab.set(key);
        this.router.navigate([], {
            relativeTo: this.route,
            queryParams: { tab: key },
            queryParamsHandling: "merge",
        });
    }
}
