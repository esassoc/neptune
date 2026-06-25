import { ActivatedRouteSnapshot, RouterStateSnapshot } from "@angular/router";
import { Observable } from "rxjs";
import { Injectable } from "@angular/core";
import { AuthenticationService } from "src/app/services/authentication.service";
import { RoleEnum } from "src/app/shared/generated/enum/role-enum";

// NPT-1056: gates the Manager Dashboard route. Admin / SitkaAdmin / JurisdictionManager only.
// Mirrors the MVC [JurisdictionManageFeature] role list. Distinct from ManagerOnlyGuard
// (Admin-only) and JurisdictionManagerOrEditorOnlyGuard (also includes Editor).
@Injectable({
    providedIn: "root",
})
export class ManagerOrAdminOnlyGuard {
    constructor(private authenticationService: AuthenticationService) {}

    canActivate(next: ActivatedRouteSnapshot, state: RouterStateSnapshot): Observable<boolean> | Promise<boolean> | boolean {
        return this.authenticationService.guardWithRoleCheck(
            state.url,
            (user) => user?.RoleID === RoleEnum.Admin || user?.RoleID === RoleEnum.SitkaAdmin || user?.RoleID === RoleEnum.JurisdictionManager
        );
    }
}
