import { Component, OnInit } from "@angular/core";
import { AuthenticationService } from "src/app/services/authentication.service";
import { PersonDto } from "src/app/shared/generated/model/person-dto";
import { AsyncPipe } from "@angular/common";
import { RouterLink, RouterLinkActive, RouterOutlet } from "@angular/router";
import { DropdownToggleDirective } from "src/app/shared/directives/dropdown-toggle.directive";
import { IconComponent } from "src/app/shared/components/icon/icon.component";
import { HeaderNavComponent } from "../../../shared/components/header-nav/header-nav.component";
import { Observable } from "rxjs";
import { environment } from "src/environments/environment";

@Component({
    selector: "planning-site-layout",
    templateUrl: "./planning-site-layout.component.html",
    styleUrls: ["./planning-site-layout.component.scss"],
    imports: [RouterLink, RouterLinkActive, RouterOutlet, AsyncPipe, DropdownToggleDirective, IconComponent, HeaderNavComponent]
})
export class PlanningSiteLayoutComponent implements OnInit {
    public currentUser$: Observable<PersonDto>;

    constructor(private authenticationService: AuthenticationService) {}

    ngOnInit() {
        this.currentUser$ = this.authenticationService.getCurrentUser();
    }

    public isAuthenticated(): boolean {
        return this.authenticationService.isAuthenticated();
    }

    public isNotUnassigned(currentUser: PersonDto): boolean {
        if (!currentUser) {
            return false;
        }
        return !this.authenticationService.isUserUnassigned(currentUser);
    }

    public isOCTAGrantReviewer(): boolean {
        return this.authenticationService.isCurrentUserAnOCTAGrantReviewer();
    }

    public isAdministrator(currentUser: PersonDto): boolean {
        return this.authenticationService.isUserAnAdministrator(currentUser);
    }

    public isJurisdicionManager(currentUser: PersonDto): boolean {
        return this.authenticationService.isUserAJurisdictionManager(currentUser);
    }

    public usersListUrl(): string {
        return `${environment.ocStormwaterToolsBaseUrl}/User/Index`;
    }

    public organizationsIndexUrl(): string {
        return `${environment.ocStormwaterToolsBaseUrl}/Organization/Index`;
    }
}
