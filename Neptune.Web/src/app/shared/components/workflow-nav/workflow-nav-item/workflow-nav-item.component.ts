import { Component, Input } from "@angular/core";

import { IconComponent } from "../../icon/icon.component";
import { RouterLink, RouterLinkActive } from "@angular/router";

@Component({
    selector: "workflow-nav-item",
    standalone: true,
    imports: [IconComponent, RouterLink, RouterLinkActive],
    templateUrl: "./workflow-nav-item.component.html",
    styleUrls: ["./workflow-nav-item.component.scss"]
})
export class WorkflowNavItemComponent {
    @Input() navRouterLink: string | string[];
    @Input() complete: boolean = false;
    @Input() disabled: boolean = false;
    @Input() required: boolean = true;

    constructor() {}
}
