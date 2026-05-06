import { Component } from "@angular/core";
import { DataHubActionButtonComponent } from "../../components/data-hub-action-button/data-hub-action-button.component";
import { DataHubQuickLinksComponent } from "../../components/data-hub-quick-links/data-hub-quick-links.component";

@Component({
    selector: "wqmps-tab",
    standalone: true,
    imports: [DataHubActionButtonComponent, DataHubQuickLinksComponent],
    templateUrl: "./wqmps-tab.component.html",
    styleUrl: "../../data-hub.component.scss",
})
export class WqmpsTabComponent {
    public comingSoonTooltip = "Migration in progress.";
}
