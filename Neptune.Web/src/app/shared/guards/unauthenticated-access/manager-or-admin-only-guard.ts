import { ActivatedRouteSnapshot, RouterStateSnapshot, Router } from "@angular/router";
import { Observable } from "rxjs";
import { map } from "rxjs/operators";
import { Injectable } from "@angular/core";
import { AlertService } from "../../services/alert.service";
import { AuthenticationService } from "src/app/services/authentication.service";
import { RoleEnum } from "src/app/shared/generated/enum/role-enum";

// NPT-1056: gates the Manager Dashboard route. Admin / SitkaAdmin / JurisdictionManager only.
// Mirrors the MVC [JurisdictionManageFeature] role list. Distinct from ManagerOnlyGuard
// (Admin-only) and JurisdictionManagerOrEditorOnlyGuard (also includes Editor).
@Injectable({
    providedIn: "root",
})
export class ManagerOrAdminOnlyGuard {
    constructor(private router: Router, private alertService: AlertService, private authenticationService: AuthenticationService) {}

    canActivate(next: ActivatedRouteSnapshot, state: RouterStateSnapshot): Observable<boolean> | Promise<boolean> | boolean {
        const isAllowed = (roleID: number | null | undefined) =>
            roleID === RoleEnum.Admin || roleID === RoleEnum.SitkaAdmin || roleID === RoleEnum.JurisdictionManager;

        return this.authenticationService.currentUserSetObservable.pipe(
            map((x) => (isAllowed(x?.RoleID) ? true : this.returnUnauthorized()))
        );
    }

    private returnUnauthorized() {
        this.router.navigate(["/"]).then(() => {
            this.alertService.pushNotFoundUnauthorizedAlert();
        });
        return false;
    }
}
