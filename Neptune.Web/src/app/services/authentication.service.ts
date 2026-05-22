import { Injectable } from "@angular/core";
import { Observable, ReplaySubject, Subject, of, race } from "rxjs";
import { first, switchMap, takeUntil } from "rxjs/operators";
import { Router } from "@angular/router";
import { AlertService } from "../shared/services/alert.service";
import { Alert } from "../shared/models/alert";
import { AlertContext } from "../shared/models/enums/alert-context.enum";
import { environment } from "src/environments/environment";
import { PersonDto } from "../shared/generated/model/person-dto";
import { RoleEnum } from "../shared/generated/enum/role-enum";
import { UserClaimsService } from "../shared/generated/api/user-claims.service";
import { ImpersonationService } from "../shared/generated/api/impersonation.service";
import { AuthService as Auth0Service } from "@auth0/auth0-angular";

@Injectable({
    providedIn: "root",
})
export class AuthenticationService {
    private currentUser: PersonDto;
    private claimsUser: any;
    private readonly _destroying$ = new Subject<void>();

    private _currentUserSetSubject = new ReplaySubject<PersonDto>(1);
    public currentUserSetObservable = this._currentUserSetSubject.asObservable();

    constructor(
        private router: Router,
        private auth0: Auth0Service,
        private userClaimsService: UserClaimsService,
        private impersonationService: ImpersonationService,
        private alertService: AlertService
    ) {
        // Subscribe to Auth0 user stream to update claims and current user
        this.auth0.user$.pipe(takeUntil(this._destroying$)).subscribe((user) => {
            if (user) {
                this.alertService.removeAlertByUniqueCode("EmailVerificationRequired");

                this.claimsUser = user as any;
                this.postUser();

                const target = sessionStorage.getItem("postAuthTarget");
                if (target) {
                    sessionStorage.removeItem("postAuthTarget");
                    this.router.navigateByUrl(target);
                }
            } else {
                this.claimsUser = null;
                this.currentUser = null;
                this._currentUserSetSubject.next(this.currentUser);
            }
        });
    }

    private postUser() {
        this.userClaimsService.postUserClaimsUserClaims().subscribe(
            (result) => {
                this.updateUser(result);
            },
            () => {
                this.onGetUserError();
            }
        );
    }

    private updateUser(user: PersonDto) {
        this.currentUser = user;
        this._currentUserSetSubject.next(this.currentUser);
    }

    private onGetUserError() {
        this.router.navigate(["/"]).then((x) => {
            this.alertService.pushAlert(
                new Alert(
                    "There was an error authorizing with the application. The application will force log you out in 3 seconds, please try to login again.",
                    AlertContext.Danger
                )
            );
            setTimeout(() => {
                this.auth0.logout({ logoutParams: { returnTo: window.location.origin } } as any);
            }, 3000);
        });
    }

    public refreshUserInfo(user: PersonDto) {
        this.updateUser(user);
    }

    public isAuthenticated(): boolean {
        return this.claimsUser != null;
    }

    public handleUnauthorized(): void {
        this.forcedLogout();
    }

    public forcedLogout() {
        this.logout();
    }

    public guardInitObservable(): Observable<any> {
        // For Auth0, return an observable that completes when loading finishes and user info is available
        return this.auth0.isLoading$.pipe(
            first((loading) => loading === false),
            switchMap(() => of(null as any))
        );
    }

    private storePostAuthTarget(target?: string) {
        const safeTarget = target ?? this.router.url ?? "/";
        sessionStorage.setItem("postAuthTarget", safeTarget);
        return safeTarget;
    }

    public login(target?: string) {
        const safeTarget = this.storePostAuthTarget(target);
        this.auth0.loginWithRedirect({ appState: { target: safeTarget } } as any);
    }

    signUp(target?: string) {
        const safeTarget = this.storePostAuthTarget(target);
        const baseRedirect = environment.auth0?.redirectUri ?? window.location.origin + "/callback";

        this.auth0.loginWithRedirect({
            authorizationParams: { screen_hint: "signup", redirect_uri: baseRedirect },
            appState: { target: safeTarget },
        } as any);
    }

    resetPassword() {
        const safeTarget = this.storePostAuthTarget();
        this.auth0.loginWithRedirect({
            authorizationParams: { screen_hint: "reset-password" },
            appState: { target: safeTarget },
        } as any);
    }

    private auth0Logout(): void {
        const areaRoot = this.getAreaRootFromUrl(this.router.url);
        const returnTo = window.location.origin + areaRoot;
        this.auth0.logout({ logoutParams: { returnTo } } as any);
    }

    public logout(): void {
        // While impersonating, the "logout" button stops impersonation instead of signing
        // out of Auth0 entirely. Mirrors WADNR's behavior — the admin's Auth0 session is
        // preserved. On error, fall through to a full Auth0 logout so the user is never
        // stuck with a stale banner / unable to log out.
        if (this.isCurrentUserBeingImpersonated()) {
            this.impersonationService.stopImpersonationImpersonation().subscribe({
                next: (response) => {
                    this.refreshUserInfo(response);
                    this.router.navigateByUrl("/").then(() => {
                        this.alertService.pushAlert(new Alert("Finished impersonating.", AlertContext.Success));
                    });
                },
                error: () => {
                    this.alertService.pushAlert(new Alert("Failed to stop impersonating; logging you out instead.", AlertContext.Danger));
                    this.auth0Logout();
                },
            });
            return;
        }

        this.auth0Logout();
    }

    // Impersonation: the SPA detects active impersonation by comparing the Auth0 JWT's `sub`
    // claim (always the authenticated user) against the currentUser's GlobalID (which flips
    // to the impersonated user's GlobalID after the impersonate endpoint runs).
    public isCurrentUserBeingImpersonated(user: PersonDto | null = this.currentUser): boolean {
        if (user && this.claimsUser) {
            return this.claimsUser.sub !== user.GlobalID;
        }
        return false;
    }

    public impersonate(personID: number): void {
        this.impersonationService.impersonateUserImpersonation(personID).subscribe({
            next: (response) => {
                this.refreshUserInfo(response);
                this.router.navigateByUrl("/").then(() => {
                    this.alertService.pushAlert(new Alert("Now impersonating user.", AlertContext.Success));
                });
            },
            error: () => {
                this.alertService.pushAlert(new Alert("Failed to start impersonating that user.", AlertContext.Danger));
            },
        });
    }

    private getAreaRootFromUrl(url: string): string {
        // url like "/admin/projects/123?x=1"
        const path = url.split("?")[0].split("#")[0];

        // pick your “areas” here:
        const firstSeg = "/" + (path.split("/").filter(Boolean)[0] ?? "");

        // Example mapping — adjust to your app:
        switch (firstSeg.toLowerCase()) {
            case "/trash":
            case "/planning":
                return firstSeg; // area homepage route
            default:
                return "/"; // main homepage
        }
    }

    public isCurrentUserNullOrUndefined(): boolean {
        return !this.currentUser;
    }

    public getCurrentUser(): Observable<PersonDto> {
        return race(
            new Observable((subscriber) => {
                if (this.currentUser) {
                    subscriber.next(this.currentUser);
                    subscriber.complete();
                }
            }),
            this.currentUserSetObservable.pipe(first())
        );
    }

    public getAccessToken(): Observable<string> {
        return this.auth0.getAccessTokenSilently();
    }

    public isUserAnAdministrator(user: PersonDto): boolean {
        const role = user ? user.RoleID : null;
        return role === RoleEnum.Admin || role === RoleEnum.SitkaAdmin;
    }

    public isCurrentUserAnAdministrator(): boolean {
        return this.isUserAnAdministrator(this.currentUser);
    }

    public isCurrentUserAnOCTAGrantReviewer(): boolean {
        if (!this.currentUser) {
            return false;
        }
        return this.currentUser.IsOCTAGrantReviewer;
    }

    public isUserAJurisdictionManager(user: PersonDto): boolean {
        const role = user ? user.RoleID : null;
        return role === RoleEnum.JurisdictionManager;
    }

    public isCurrentUserAJurisdictionManagerWithAssignedJurisdiction(): boolean {
        return this.isUserAJurisdictionManager(this.currentUser) && this.doesCurrentUserHaveAssignedStormwaterJurisdiction();
    }

    public isCurrentUserAJurisdictionEditorWithAssignedJurisdiction(): boolean {
        return this.isUserAJurisdictionEditor(this.currentUser) && this.doesCurrentUserHaveAssignedStormwaterJurisdiction();
    }

    public isUserAJurisdictionEditor(user: PersonDto): boolean {
        const role = user ? user.RoleID : null;
        return role === RoleEnum.JurisdictionEditor;
    }

    public doesCurrentUserHaveAssignedStormwaterJurisdiction(): boolean {
        if (!this.currentUser) {
            return false;
        }
        return this.currentUser.HasAssignedStormwaterJurisdiction;
    }

    public doesCurrentUserHaveJurisdictionManagePermission(): boolean {
        return this.isCurrentUserAnAdministrator() || this.isCurrentUserAJurisdictionManagerWithAssignedJurisdiction();
    }

    public doesCurrentUserHaveJurisdictionEditPermission(): boolean {
        return (
            this.isCurrentUserAnAdministrator() ||
            this.isCurrentUserAJurisdictionManagerWithAssignedJurisdiction() ||
            this.isCurrentUserAJurisdictionEditorWithAssignedJurisdiction()
        );
    }

    public isCurrentUserUnassigned(): boolean {
        return this.isUserUnassigned(this.currentUser);
    }

    // NPT-1064: nav-visibility check that matches MVC behavior. Anonymous (not logged in)
    // and Unassigned (logged in, no role) users share the same accessible surface area —
    // only AnonymousUnclassifiedFeature-allowed pages on the MVC side. The SPA uses this
    // to hide top-nav links to pages those users would just bounce off of anyway.
    //
    // Checking `!this.currentUser` (instead of `!isAuthenticated()`) covers both anonymous
    // visitors AND the brief race between Auth0 setting `claimsUser` and the subsequent
    // /people/me round-trip populating `currentUser` — during that window an Unassigned
    // user would otherwise see the full nav and could click into a route that 403s.
    public isCurrentUserAnonymousOrUnassigned(): boolean {
        return !this.currentUser || this.isCurrentUserUnassigned();
    }

    public isUserUnassigned(user: PersonDto): boolean {
        const role = user ? user.RoleID : null;
        return role === RoleEnum.Unassigned;
    }

    public doesCurrentUserHaveOneOfTheseRoles(roleIDs: Array<number>): boolean {
        if (roleIDs.length === 0) {
            return false;
        }
        const roleID = this.currentUser ? this.currentUser.RoleID : null;
        return roleIDs.includes(roleID);
    }

    ngOnDestroy(): void {
        this._destroying$.next(undefined);
        this._destroying$.complete();
    }
}
