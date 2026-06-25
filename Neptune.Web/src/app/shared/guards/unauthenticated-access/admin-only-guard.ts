import { ActivatedRouteSnapshot, RouterStateSnapshot } from "@angular/router";
import { Observable } from "rxjs";
import { Injectable } from "@angular/core";
import { AuthenticationService } from "src/app/services/authentication.service";
import { RoleEnum } from "src/app/shared/generated/enum/role-enum";

// Admin/SitkaAdmin-only route guard — matches the backend [AdminFeature] attribute. Use for routes whose API
// is admin-gated (e.g. Funding Sources, whose list endpoint is [AdminFeature]) so anonymous/non-admin users are
// blocked at the route instead of loading the page and hitting a 403. Anonymous users are sent to login (and
// returned here afterwards); logged-in non-admins get the home redirect + unauthorized alert.
@Injectable({
    providedIn: "root",
})
export class AdminOnlyGuard {
    constructor(private authenticationService: AuthenticationService) {}

    canActivate(next: ActivatedRouteSnapshot, state: RouterStateSnapshot): Observable<boolean> | Promise<boolean> | boolean {
        return this.authenticationService.guardWithRoleCheck(
            state.url,
            (user) => user?.RoleID === RoleEnum.Admin || user?.RoleID === RoleEnum.SitkaAdmin
        );
    }
}
