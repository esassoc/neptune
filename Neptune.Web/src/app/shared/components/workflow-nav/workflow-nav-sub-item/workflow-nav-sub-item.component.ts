import { Component, Input } from "@angular/core";
import { RouterLink, RouterLinkActive } from "@angular/router";

import { IconComponent } from "../../icon/icon.component";

@Component({
    selector: "workflow-nav-sub-item",
    standalone: true,
    imports: [IconComponent, RouterLink, RouterLinkActive],
    templateUrl: "./workflow-nav-sub-item.component.html",
    styleUrl: "./workflow-nav-sub-item.component.scss",
})
export class WorkflowNavSubItemComponent {
    @Input() navRouterLink: string | string[];
    @Input() complete: boolean = false;
    @Input() disabled: boolean = false;
}
