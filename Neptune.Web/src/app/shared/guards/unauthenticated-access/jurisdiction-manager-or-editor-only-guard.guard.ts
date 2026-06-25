import { Injectable } from "@angular/core";
import { ActivatedRouteSnapshot, RouterStateSnapshot } from "@angular/router";
import { Observable } from "rxjs";
import { AuthenticationService } from "src/app/services/authentication.service";
import { RoleEnum } from "src/app/shared/generated/enum/role-enum";

@Injectable({
    providedIn: "root",
})
export class JurisdictionManagerOrEditorOnlyGuard {
    constructor(private authenticationService: AuthenticationService) {}

    canActivate(next: ActivatedRouteSnapshot, state: RouterStateSnapshot): Observable<boolean> | Promise<boolean> | boolean {
        return this.authenticationService.guardWithRoleCheck(
            state.url,
            (user) =>
                user?.RoleID === RoleEnum.Admin ||
                user?.RoleID === RoleEnum.SitkaAdmin ||
                user?.RoleID === RoleEnum.JurisdictionManager ||
                user?.RoleID === RoleEnum.JurisdictionEditor
        );
    }
}
