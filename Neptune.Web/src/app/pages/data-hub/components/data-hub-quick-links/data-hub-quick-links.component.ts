import { Component, Input } from "@angular/core";
import { RouterLink } from "@angular/router";

export type DataHubTabKey = "bmps" | "wqmps" | "trash" | "county" | "web-services";

interface QuickLink {
    label: string;
    routerLink: string | (string | number)[];
}

@Component({
    selector: "data-hub-quick-links",
    standalone: true,
    imports: [RouterLink],
    templateUrl: "./data-hub-quick-links.component.html",
    styleUrl: "./data-hub-quick-links.component.scss",
})
export class DataHubQuickLinksComponent {
    @Input({ required: true }) tab!: DataHubTabKey;

    private linkSets: Record<DataHubTabKey, QuickLink[]> = {
        bmps: [
            { label: "BMP List", routerLink: "/treatment-bmps" },
            { label: "Field Visits", routerLink: "/field-records" },
            { label: "BMP Map", routerLink: "/treatment-bmps" },
            { label: "BMP Types", routerLink: "/program-info/treatment-bmp-types" },
        ],
        wqmps: [{ label: "WQMP List", routerLink: "/water-quality-management-plans" }],
        trash: [
            { label: "OVTA List", routerLink: "/trash/onland-visual-trash-assessments" },
            { label: "Trash Analysis Areas", routerLink: "/trash/trash-analysis-areas" },
            { label: "Land Use Blocks", routerLink: "/trash/land-use-blocks" },
        ],
        county: [
            { label: "Parcels", routerLink: "/parcels" },
            { label: "Regional Subbasins", routerLink: "/regional-subbasins" },
            { label: "HRU Characteristics List", routerLink: "/hru-characteristics" },
        ],
        "web-services": [],
    };

    get links(): QuickLink[] {
        return this.linkSets[this.tab];
    }

    get tipText(): string {
        switch (this.tab) {
            case "trash":
                return "Go to the BMP and WQMP tabs to update asset inventories and set trash capture status.";
            default:
                return "";
        }
    }
}
