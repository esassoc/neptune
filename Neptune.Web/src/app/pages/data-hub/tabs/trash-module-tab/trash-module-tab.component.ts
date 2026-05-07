import { Component, computed, inject, Signal } from "@angular/core";
import { toSignal } from "@angular/core/rxjs-interop";
import { map } from "rxjs";
import { AuthenticationService } from "src/app/services/authentication.service";
import { RoleEnum } from "src/app/shared/generated/enum/role-enum";
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
    private authenticationService = inject(AuthenticationService);

    private currentUser = toSignal(this.authenticationService.currentUserSetObservable.pipe(map((u) => u ?? null)), { initialValue: null });

    // Mirrors the legacy [JurisdictionManageFeature] envelope: Admin / SitkaAdmin / JurisdictionManager only.
    public isManager: Signal<boolean> = computed(() => {
        const u = this.currentUser();
        if (!u) return false;
        return [RoleEnum.Admin, RoleEnum.SitkaAdmin, RoleEnum.JurisdictionManager].includes(u.RoleID);
    });

    public isAdminOrEditor: Signal<boolean> = computed(() => {
        const u = this.currentUser();
        if (!u) return false;
        return [RoleEnum.Admin, RoleEnum.SitkaAdmin, RoleEnum.JurisdictionManager, RoleEnum.JurisdictionEditor].includes(u.RoleID);
    });

    public managerOnlyTooltip = "Only Administrators and Jurisdiction Managers can perform this action.";
    public managerEditorTooltip = "Only Administrators, Jurisdiction Managers, and Jurisdiction Editors can perform this action.";
}
