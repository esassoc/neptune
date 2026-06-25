import { ActivatedRouteSnapshot, RouterStateSnapshot } from "@angular/router";
import { Observable } from "rxjs";
import { Injectable } from "@angular/core";
import { AuthenticationService } from "src/app/services/authentication.service";

@Injectable({
    providedIn: "root",
})
export class OCTAGrantReviewerOnlyGuard {
    constructor(private authenticationService: AuthenticationService) {}

    canActivate(next: ActivatedRouteSnapshot, state: RouterStateSnapshot): Observable<boolean> | Promise<boolean> | boolean {
        return this.authenticationService.guardWithRoleCheck(state.url, (user) => !!user?.IsOCTAGrantReviewer);
    }
}
