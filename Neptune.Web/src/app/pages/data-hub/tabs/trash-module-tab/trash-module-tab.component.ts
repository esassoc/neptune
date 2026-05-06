import { Component } from "@angular/core";
import { DataHubActionButtonComponent } from "../../components/data-hub-action-button/data-hub-action-button.component";
import { DataHubQuickLinksComponent } from "../../components/data-hub-quick-links/data-hub-quick-links.component";

@Component({
    selector: "trash-module-tab",
    standalone: true,
    imports: [DataHubActionButtonComponent, DataHubQuickLinksComponent],
    templateUrl: "./trash-module-tab.component.html",
    styleUrl: "../../data-hub.component.scss",
})
export class TrashModuleTabComponent {
    public comingSoonTooltip = "Migration in progress.";
}
