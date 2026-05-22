import { Component, computed, OnInit, Signal } from "@angular/core";
import { toSignal } from "@angular/core/rxjs-interop";
import { AuthenticationService } from "src/app/services/authentication.service";
import { environment } from "src/environments/environment";
import { PersonDto } from "src/app/shared/generated/model/person-dto";
import { RouterLink, RouterLinkActive, RouterOutlet } from "@angular/router";
import { DropdownToggleDirective } from "src/app/shared/directives/dropdown-toggle.directive";
import { IconComponent } from "src/app/shared/components/icon/icon.component";
import { HeaderNavComponent } from "../../shared/components/header-nav/header-nav.component";
import { map, Observable } from "rxjs";
import { RoleEnum } from "src/app/shared/generated/enum/role-enum";

@Component({
    selector: "site-layout",
    templateUrl: "./site-layout.component.html",
    styleUrls: ["./site-layout.component.scss"],
    imports: [RouterLink, RouterLinkActive, RouterOutlet, DropdownToggleDirective, IconComponent, HeaderNavComponent],
})
export class SiteLayoutComponent implements OnInit {
    public currentUser$: Observable<PersonDto>;
    public siteUrl = environment.ocStormwaterToolsBaseUrl;

    constructor(private authenticationService: AuthenticationService) {}

    ngOnInit() {
        this.currentUser$ = this.authenticationService.getCurrentUser();
    }

    public isAuthenticated(): boolean {
        return this.authenticationService.isAuthenticated();
    }

    public isAdministrator(currentUser: PersonDto): boolean {
        return this.authenticationService.isUserAnAdministrator(currentUser);
    }

    public isJurisdicionManager(currentUser: PersonDto): boolean {
        return this.authenticationService.isUserAJurisdictionManager(currentUser);
    }

    public isNotUnassigned(currentUser: PersonDto): boolean {
        if (!currentUser) {
            return false;
        }
        return !this.authenticationService.isUserUnassigned(currentUser);
    }

    // Signal-backed gate for nav items so the template re-evaluates when the user changes
    // (impersonate/stop-impersonate). A plain method call on the auth service would not
    // trigger CD in zoneless mode after a user mutation — site-layout has no other live
    // observable binding to drive a tick, so we derive a signal off the same ReplaySubject
    // that backs all user updates.
    //
    // Anonymous and Unassigned users see only the public-facing list pages (WQMP List + BMP
    // List + WQMP Map + Find a BMP); everything else in the top nav routes to pages they
    // don't have permission to access. Mirrors the MVC behavior where each nav item is gated
    // by its controller's feature attribute. NPT-1064.
    private currentUserSignal = toSignal(this.authenticationService.currentUserSetObservable.pipe(map((u) => u ?? null)), { initialValue: null });

    public isCurrentUserAnonymousOrUnassigned: Signal<boolean> = computed(() => {
        const u = this.currentUserSignal();
        return !u || u.RoleID === RoleEnum.Unassigned;
    });

    // Role-derived nav gates, mirroring MVC's per-controller [Feature] attributes:
    //   Manage menu items → NeptuneAdminFeature (Admin/SitkaAdmin only)
    //   Dashboard         → currentPerson.IsManagerOrAdmin()
    //   Data Hub          → currentPerson.IsJurisdictionEditorOrManagerOrAdmin()
    //   Delineation Reconciliation Report → JurisdictionEditFeature
    public isCurrentUserAdmin: Signal<boolean> = computed(() => {
        const u = this.currentUserSignal();
        return !!u && (u.RoleID === RoleEnum.Admin || u.RoleID === RoleEnum.SitkaAdmin);
    });

    public isCurrentUserManagerOrAdmin: Signal<boolean> = computed(() => {
        const u = this.currentUserSignal();
        return !!u && (u.RoleID === RoleEnum.Admin || u.RoleID === RoleEnum.SitkaAdmin || u.RoleID === RoleEnum.JurisdictionManager);
    });

    public isCurrentUserEditorOrHigher: Signal<boolean> = computed(() => {
        const u = this.currentUserSignal();
        return !!u && (u.RoleID === RoleEnum.Admin || u.RoleID === RoleEnum.SitkaAdmin || u.RoleID === RoleEnum.JurisdictionManager || u.RoleID === RoleEnum.JurisdictionEditor);
    });

    public isOCTAGrantReviewer(): boolean {
        return this.authenticationService.isCurrentUserAnOCTAGrantReviewer();
    }

    public usersListUrl(): string {
        return `${environment.ocStormwaterToolsBaseUrl}/User/Index`;
    }

    public organizationsIndexUrl(): string {
        return `${environment.ocStormwaterToolsBaseUrl}/Organization/Index`;
    }
}
