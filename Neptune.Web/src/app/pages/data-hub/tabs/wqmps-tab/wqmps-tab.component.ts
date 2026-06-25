import { Component, computed, inject, Signal } from "@angular/core";
import { toSignal } from "@angular/core/rxjs-interop";
import { map } from "rxjs";
import { AuthenticationService } from "src/app/services/authentication.service";
import { RoleEnum } from "src/app/shared/generated/enum/role-enum";
import { NeptunePageTypeEnum } from "src/app/shared/generated/enum/neptune-page-type-enum";
import { CustomRichTextComponent } from "src/app/shared/components/custom-rich-text/custom-rich-text.component";
import { DataHubActionButtonComponent } from "../../components/data-hub-action-button/data-hub-action-button.component";
import { DataHubQuickLinksComponent } from "../../components/data-hub-quick-links/data-hub-quick-links.component";

@Component({
    selector: "wqmps-tab",
    standalone: true,
    imports: [CustomRichTextComponent, DataHubActionButtonComponent, DataHubQuickLinksComponent],
    templateUrl: "./wqmps-tab.component.html",
    styleUrl: "../../data-hub.component.scss",
})
export class WqmpsTabComponent {
    private authenticationService = inject(AuthenticationService);

    public NeptunePageTypeEnum = NeptunePageTypeEnum;

    private currentUser = toSignal(this.authenticationService.currentUserSetObservable.pipe(map((u) => u ?? null)), { initialValue: null });

    public isAdminOrEditor: Signal<boolean> = computed(() => {
        const u = this.currentUser();
        if (!u) return false;
        return [RoleEnum.Admin, RoleEnum.SitkaAdmin, RoleEnum.JurisdictionManager, RoleEnum.JurisdictionEditor].includes(u.RoleID);
    });

    public managerEditorTooltip = "Only Administrators, Jurisdiction Managers, and Jurisdiction Editors can perform this action.";
}
