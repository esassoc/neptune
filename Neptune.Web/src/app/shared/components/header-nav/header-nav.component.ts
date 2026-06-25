import { Component, OnInit, Input } from "@angular/core";
import { AuthenticationService } from "src/app/services/authentication.service";
import { environment } from "src/environments/environment";
import { PersonDto } from "src/app/shared/generated/model/person-dto";
import { AsyncPipe } from "@angular/common";
import { RouterLink, RouterLinkActive } from "@angular/router";
import { DropdownToggleDirective } from "src/app/shared/directives/dropdown-toggle.directive";
import { IconComponent } from "src/app/shared/components/icon/icon.component";
import { Observable } from "rxjs";

@Component({
    selector: "header-nav",
    templateUrl: "./header-nav.component.html",
    styleUrls: ["./header-nav.component.scss"],
    imports: [RouterLink, RouterLinkActive, AsyncPipe, DropdownToggleDirective, IconComponent],
})
export class HeaderNavComponent implements OnInit {
    @Input() moduleTitle: string;
    public currentUser$: Observable<PersonDto>;

    constructor(private authenticationService: AuthenticationService) {}

    ngOnInit() {
        // Bind to the long-lived ReplaySubject(1) directly, NOT getCurrentUser() — the
        // latter race-completes after the first emission, so the async pipe's subscription
        // closes and never sees later impersonate/stop-impersonate updates. With the
        // replay subject, the pipe stays subscribed and the banner + Welcome name
        // re-render on every user change.
        this.currentUser$ = this.authenticationService.currentUserSetObservable;
    }

    public isAuthenticated(): boolean {
        return this.authenticationService.isAuthenticated();
    }

    public isBeingImpersonated(user: PersonDto): boolean {
        return this.authenticationService.isCurrentUserBeingImpersonated(user);
    }

    public stopImpersonation(): void {
        // logout() in AuthenticationService routes to the stop-impersonation endpoint when
        // the user is currently impersonating — no separate endpoint call needed here.
        this.authenticationService.logout();
    }

    public login(): void {
        this.authenticationService.login();
    }

    public logout(): void {
        //MP 8/28/25 While we are in this half SPA half MVC state, redirect people to the home page of where they came from
        const url = new URL(window.location.href);
        const firstPathPart = url.pathname.split("/")[1];
        sessionStorage["authRedirectUrl"] = `/${firstPathPart}`;
        this.authenticationService.logout();
    }

    public showTestingWarning(): boolean {
        return environment.staging || environment.dev;
    }

    public testingWarningText(): string {
        return environment.staging ? "QA Environment" : "Development Environment";
    }
}
