import { Component, Input } from "@angular/core";
import { RouterLink } from "@angular/router";

@Component({
    selector: "data-hub-action-button",
    standalone: true,
    imports: [RouterLink],
    templateUrl: "./data-hub-action-button.component.html",
    styleUrl: "./data-hub-action-button.component.scss",
})
export class DataHubActionButtonComponent {
    @Input({ required: true }) label!: string;
    @Input({ required: true }) icon!: "upload" | "download" | "refresh";
    @Input() routerLink?: string | (string | number)[];
    @Input() disabled = false;
    @Input() disabledTooltip = "";

    get iconClass(): string {
        switch (this.icon) {
            case "upload":
                return "fa fa-upload";
            case "download":
                return "fa fa-download";
            case "refresh":
                return "fa fa-refresh";
        }
    }
}
