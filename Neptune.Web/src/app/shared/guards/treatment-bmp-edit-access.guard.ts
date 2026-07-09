import { Injectable } from "@angular/core";
import { ActivatedRouteSnapshot, Router } from "@angular/router";
import { Observable, of } from "rxjs";
import { catchError, map } from "rxjs/operators";
import { TreatmentBMPService } from "src/app/shared/generated/api/treatment-bmp.service";
import { AlertService } from "src/app/shared/services/alert.service";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";

/**
 * NPT-1104: the routed Treatment BMP editors (edit-basic-info / edit-location /
 * edit-custom-attributes / edit-images) had no canActivate guard, so a user without edit
 * rights (e.g. a JurisdictionEditor for a different jurisdiction, arriving via bookmark or
 * direct URL — the detail page hides the buttons) could open the editor, fill in the form,
 * and only discover on save that they lack permission. Check the server-computed
 * CurrentPersonCanEdit up front and bounce back to the detail page with a clear alert.
 *
 * On an API error the guard lets navigation proceed — the server-side
 * [TreatmentBMPEditFeature] gates remain the real enforcement on save.
 */
@Injectable({
    providedIn: "root",
})
export class TreatmentBmpEditAccessGuard {
    constructor(
        private treatmentBMPService: TreatmentBMPService,
        private router: Router,
        private alertService: AlertService
    ) {}

    canActivate(next: ActivatedRouteSnapshot): Observable<boolean> | boolean {
        const treatmentBMPID = Number(next.paramMap.get("treatmentBMPID"));
        if (!Number.isFinite(treatmentBMPID)) {
            return true; // malformed URL: let routing/component handle it
        }

        return this.treatmentBMPService.getByIDTreatmentBMP(treatmentBMPID).pipe(
            map((treatmentBMP) => {
                if (treatmentBMP?.CurrentPersonCanEdit) {
                    return true;
                }
                // Post-navigation alert pattern: push after the redirect completes so the
                // destination's AlertDisplayComponent renders it (matches returnUnauthorized).
                this.router.navigate(["/treatment-bmps", treatmentBMPID]).then(() => {
                    this.alertService.pushAlert(
                        new Alert("You do not have permission to edit this Treatment BMP.", AlertContext.Warning)
                    );
                });
                return false;
            }),
            catchError(() => of(true))
        );
    }
}
