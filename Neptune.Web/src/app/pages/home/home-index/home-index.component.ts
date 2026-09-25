import { Component, OnInit } from "@angular/core";
import { AuthenticationService } from "src/app/services/authentication.service";
import { ActivatedRoute, Router, RouterLink } from "@angular/router";
import { PersonDto } from "src/app/shared/generated/model/person-dto";
import { NeptunePageTypeEnum } from "src/app/shared/generated/enum/neptune-page-type-enum";
import { CustomRichTextComponent } from "src/app/shared/components/custom-rich-text/custom-rich-text.component";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { AsyncPipe } from "@angular/common";
import { Observable, map, of, shareReplay, switchMap } from "rxjs";
import { IconComponent } from "src/app/shared/components/icon/icon.component";
import { StormwaterJurisdictionService } from "src/app/shared/generated/api/stormwater-jurisdiction.service";
import { CommonTasksPanelComponent } from "./common-tasks-panel/common-tasks-panel.component";
import { FieldActionsPanelComponent } from "./field-actions-panel/field-actions-panel.component";

@Component({
    selector: "app-home-index",
    templateUrl: "./home-index.component.html",
    styleUrls: ["./home-index.component.scss"],
    imports: [AlertDisplayComponent, RouterLink, CustomRichTextComponent, AsyncPipe, IconComponent, CommonTasksPanelComponent, FieldActionsPanelComponent],
})
export class HomeIndexComponent implements OnInit {
    public currentUser$: Observable<PersonDto>;
    // NPT-1123: Common Tasks + Field Actions for every signed-in role except Unassigned
    public showActionPanels$: Observable<boolean>;
    public jurisdictionMessage$: Observable<string>;

    public customRichTextTypeID: number = NeptunePageTypeEnum.SPAHomePage;

    constructor(
        private authenticationService: AuthenticationService,
        private stormwaterJurisdictionService: StormwaterJurisdictionService,
        private router: Router,
        private route: ActivatedRoute
    ) {}

    public ngOnInit(): void {
        this.currentUser$ = this.authenticationService.getCurrentUser().pipe(shareReplay({ bufferSize: 1, refCount: true }));
        this.showActionPanels$ = this.currentUser$.pipe(map((user) => !!user && !this.authenticationService.isUserUnassigned(user)));
        this.jurisdictionMessage$ = this.currentUser$.pipe(switchMap((user) => this.buildJurisdictionMessage(user)));
    }

    public isUserUnassigned(user: PersonDto): boolean {
        return this.authenticationService.isUserUnassigned(user);
    }

    public login(): void {
        this.authenticationService.login();
    }

    public signUp(): void {
        this.authenticationService.signUp();
    }

    private buildJurisdictionMessage(user: PersonDto): Observable<string> {
        if (!user || this.authenticationService.isUserUnassigned(user)) {
            return of(null);
        }
        if (this.authenticationService.isUserAnAdministrator(user)) {
            return of("You have access to all jurisdictions.");
        }
        return this.stormwaterJurisdictionService.listViewableStormwaterJurisdiction().pipe(
            map((jurisdictions) => {
                const names = jurisdictions.map((x) => x.StormwaterJurisdictionName).filter((x) => !!x);
                if (names.length === 0) {
                    return "You are not yet assigned to a jurisdiction.";
                }
                return names.length === 1 ? `Your jurisdiction is ${names[0]}.` : `Your jurisdictions are ${names.join(", ")}.`;
            })
        );
    }
}
