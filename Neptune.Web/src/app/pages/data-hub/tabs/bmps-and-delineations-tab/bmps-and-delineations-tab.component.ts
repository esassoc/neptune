import { Component, computed, inject, Signal } from "@angular/core";
import { toSignal } from "@angular/core/rxjs-interop";
import { map } from "rxjs";
import { AuthenticationService } from "src/app/services/authentication.service";
import { RoleEnum } from "src/app/shared/generated/enum/role-enum";
import { DataHubActionButtonComponent } from "../../components/data-hub-action-button/data-hub-action-button.component";
import { DataHubQuickLinksComponent } from "../../components/data-hub-quick-links/data-hub-quick-links.component";

@Component({
    selector: "bmps-and-delineations-tab",
    standalone: true,
    imports: [DataHubActionButtonComponent, DataHubQuickLinksComponent],
    templateUrl: "./bmps-and-delineations-tab.component.html",
    styleUrl: "../../data-hub.component.scss",
})
export class BmpsAndDelineationsTabComponent {
    private authenticationService = inject(AuthenticationService);

    private currentUser = toSignal(this.authenticationService.currentUserSetObservable.pipe(map((u) => u ?? null)), { initialValue: null });

    public isAdmin: Signal<boolean> = computed(() => {
        const u = this.currentUser();
        return !!u && (u.RoleID === RoleEnum.Admin || u.RoleID === RoleEnum.SitkaAdmin);
    });

    public isAdminOrEditor: Signal<boolean> = computed(() => {
        const u = this.currentUser();
        if (!u) return false;
        return [RoleEnum.Admin, RoleEnum.SitkaAdmin, RoleEnum.JurisdictionManager, RoleEnum.JurisdictionEditor].includes(u.RoleID);
    });

    public adminOnlyTooltip = "Only Administrators can perform this action.";
    public managerEditorTooltip = "Only Administrators, Jurisdiction Managers, and Jurisdiction Editors can perform this action.";
    public comingSoonTooltip = "Migration in progress.";
}
