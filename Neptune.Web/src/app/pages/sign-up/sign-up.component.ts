import { Component, inject, OnInit } from "@angular/core";
import { AuthenticationService } from "src/app/services/authentication.service";

// NPT-734: stable deep link for the invite email. The Auth0 SDK has to build the /authorize URL itself
// (it stores PKCE state in the browser), so the email can't point at Auth0 directly. This page starts the
// same sign-up flow as the home page's "Create Account" button. It deliberately doesn't wait on
// isAuthenticated$ first: that can stall on a silent-auth check, and a visitor who already has an Auth0
// session is simply passed back through to the home page by Auth0.
@Component({
    selector: "sign-up",
    standalone: true,
    template: `<div class="page-body"><p><i class="fa fa-spinner fa-spin"></i> Taking you to account sign-up...</p></div>`,
})
export class SignUpComponent implements OnInit {
    private authenticationService = inject(AuthenticationService);

    ngOnInit(): void {
        this.authenticationService.signUp("/");
    }
}
