import { Injectable } from "@angular/core";
import { HttpInterceptor, HttpRequest, HttpErrorResponse, HttpHandler, HttpEvent, HttpResponse } from "@angular/common/http";

import { Observable, EMPTY, throwError, of } from "rxjs";
import { catchError } from "rxjs/operators";
import { Router } from "@angular/router";
import { AlertService } from "../services/alert.service";
import { AlertContext } from "../models/enums/alert-context.enum";
import { Alert } from "../models/alert";

@Injectable()
export class HttpErrorInterceptor implements HttpInterceptor {
    constructor(
        private router: Router,
        private alertService: AlertService
    ) {}
    intercept(request: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
        return next.handle(request).pipe(
            catchError((error: HttpErrorResponse) => {
                if (error.error instanceof Error) {
                    // A client-side or network error occurred. Handle it accordingly.
                    console.error("An error occurred:", error.error.message);
                } else {
                    // The backend returned an unsuccessful response code.
                    // The response body may contain clues as to what went wrong,
                    console.error(`Backend returned code ${error.status}, body was: ${error.error}`);

                    // If the backend indicates a missing refresh token, log which request triggered it.
                    try {
                        if (typeof error.error === "string" && error.error.indexOf("missing_refresh_token") !== -1) {
                            // eslint-disable-next-line no-console
                            console.error("[HttpErrorInterceptor] Request triggered missing_refresh_token", {
                                method: request.method,
                                url: request.urlWithParams,
                            });
                            // eslint-disable-next-line no-console
                            console.error("[HttpErrorInterceptor] Full HttpErrorResponse:", error);
                        }
                    } catch (e) {}

                    if (error instanceof HttpErrorResponse) {
                        if (error.status == 400) {
                            if (!error.error) {
                                return throwError(() => error);
                            }

                            const formatValue = (val: unknown): string =>
                                Array.isArray(val) ? (val as string[]).join("<br/>") : String(val);

                            if (error.error.errors) {
                                for (const key of Object.keys(error.error.errors)) {
                                    this.alertService.pushAlert(new Alert(formatValue(error.error.errors[key]), AlertContext.Danger));
                                }
                            } else if (typeof error.error === "string") {
                                this.alertService.pushAlert(new Alert(error.error, AlertContext.Danger));
                            } else {
                                // Dictionary of messages — each value may be a string or a string[]
                                for (const key of Object.keys(error.error)) {
                                    this.alertService.pushAlert(new Alert(formatValue(error.error[key]), AlertContext.Danger));
                                }
                            }
                        }
                        if (error.status == 401) {
                            this.router.navigateByUrl("/unauthenticated", { replaceUrl: false }).then((x) => {
                                if (typeof error.error === "string") {
                                    this.alertService.pushAlert(new Alert(error.error, AlertContext.Danger));
                                }
                            });
                        }
                        if (error.status == 403) {
                            this.router.navigateByUrl("/subscription-insufficient", { replaceUrl: false }).then((x) => {
                                if (typeof error.error === "string") {
                                    this.alertService.pushAlert(new Alert(error.error, AlertContext.Danger));
                                }
                            });
                        }
                        if (error.status == 404) {
                            if (error.error.includes("User with GUID ")) {
                                // we want the login-callback to create the user to trigger so we just let it pass through and have authentication-service handle it
                                return throwError(error);
                            } else {
                                this.router.navigateByUrl("/not-found", { replaceUrl: false }).then((x) => {
                                    if (typeof error.error === "string") {
                                        this.alertService.pushAlert(new Alert(error.error, AlertContext.Danger));
                                    }
                                });
                            }
                        }
                    }
                }

                // If you want to return a new response:
                //return of(new HttpResponse({body: [{name: "Default value..."}]}));

                // If you want to return nothing:
                //return EMPTY;

                // Otherwise pass it on to the upper level and let them take care of it:
                return throwError(error);
            })
        );
    }
}
