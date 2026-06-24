import { ActivatedRouteSnapshot, RouterStateSnapshot, Router } from "@angular/router";
import { Observable } from "rxjs";
import { map } from "rxjs/operators";
import { Injectable } from "@angular/core";
import { AlertService } from "../../services/alert.service";
import { AuthenticationService } from "src/app/services/authentication.service";
import { RoleEnum } from "src/app/shared/generated/enum/role-enum";

// Admin/SitkaAdmin-only route guard — matches the backend [AdminFeature] attribute. Use for routes whose API
// is admin-gated (e.g. Funding Sources, whose list endpoint is [AdminFeature]) so anonymous/non-admin users are
// blocked at the route instead of loading the page and hitting a 403.
@Injectable({
    providedIn: "root",
})
export class AdminOnlyGuard {
    constructor(private router: Router, private alertService: AlertService, private authenticationService: AuthenticationService) {}

    canActivate(next: ActivatedRouteSnapshot, state: RouterStateSnapshot): Observable<boolean> | Promise<boolean> | boolean {
        if (!this.authenticationService.isCurrentUserNullOrUndefined()) {
            if (this.authenticationService.isCurrentUserAnAdministrator()) {
                return true;
            } else {
                return this.returnUnauthorized();
            }
        }

        return this.authenticationService.currentUserSetObservable.pipe(
            map((x) => {
                if (x.RoleID == RoleEnum.Admin || x.RoleID == RoleEnum.SitkaAdmin) {
                    return true;
                } else {
                    return this.returnUnauthorized();
                }
            })
        );
    }

    private returnUnauthorized() {
        this.router.navigate(["/"]).then(() => {
            this.alertService.pushNotFoundUnauthorizedAlert();
        });
        return false;
    }
}
