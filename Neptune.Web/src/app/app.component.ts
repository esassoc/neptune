import { AuthService as Auth0Service } from "@auth0/auth0-angular";
import { AlertService } from "./shared/services/alert.service";
import { Alert } from "./shared/models/alert";
import { AlertContext } from "./shared/models/enums/alert-context.enum";
import { filter, take } from "rxjs/operators";
import { Component, Inject, Renderer2, ViewContainerRef, DOCUMENT } from "@angular/core";
import { environment } from "../environments/environment";
import { Router, RouterLink, RouterOutlet } from "@angular/router";
import { AuthenticationService } from "./services/authentication.service";
import { Title } from "@angular/platform-browser";
import { PersonDto } from "./shared/generated/model/person-dto";

@Component({
    selector: "app-root",
    templateUrl: "./app.component.html",
    styleUrls: ["./app.component.scss"],
    imports: [RouterOutlet, RouterLink],
})
export class AppComponent {
    userClaimsUpsertStarted = false;
    isIframe = false;
    ignoreSessionTerminated = false;

    private currentUser: PersonDto;

    public currentYear: number = new Date().getFullYear();

    constructor(
        @Inject(DOCUMENT) private _document: Document,
        private titleService: Title,
        private renderer: Renderer2,
        private authenticationService: AuthenticationService,
        private auth0: Auth0Service,
        private alertService: AlertService,
        private router: Router,
        public viewRef: ViewContainerRef
    ) {}

    ngOnInit() {
        this.auth0.error$.pipe(filter((e): e is Error => !!e)).subscribe((err) => {
            const authErr = err as any;
            const code = (authErr.error ?? "").toString().toLowerCase();
            const desc = (authErr.error_description ?? authErr.message ?? "").toString().toLowerCase();

            if (code === "access_denied" && desc.includes("verify your email")) {
                this.alertService.pushAlert(
                    new Alert(
                        "Please verify your email address before signing in. Check your inbox for a verification link (and spam/junk).",
                        AlertContext.Info,
                        true,
                        "EmailVerificationRequired"
                    )
                );
            }
            const target = sessionStorage.getItem("postAuthTarget");
            if (target && this.isSafeSpaTarget(target)) {
                this.router.navigateByUrl(target, { replaceUrl: true });
            }
        });

        this.isIframe = window !== window.parent && !window.opener;
        const environmentClassName = environment.production ? "env-prod" : environment.staging ? "env-qa" : "env-dev";
        this.renderer.addClass(this._document.body, environmentClassName);

        this.authenticationService.currentUserSetObservable.subscribe((currentUser) => {
            this.currentUser = currentUser;
        });

        this.titleService.setTitle(`Stormwater Tools | Orange County`);
        this.setAppFavicon();
    }

    setAppFavicon() {
        this._document.getElementById("appFavicon").setAttribute("href", "assets/main/favicons/favicon.ico");
    }

    private isSafeSpaTarget(target: string): boolean {
        // local-only; avoid loops
        if (!target.startsWith("/")) return false;
        if (target.startsWith("//")) return false;
        if (target.startsWith("/callback")) return false;
        if (target.startsWith("/unauthenticated")) return false;
        return true;
    }
}
