import { Component, OnInit } from "@angular/core";
import { AuthenticationService } from "src/app/services/authentication.service";
import { ActivatedRoute, Router, RouterLink } from "@angular/router";
import { PersonDto } from "src/app/shared/generated/model/person-dto";
import { NeptunePageTypeEnum } from "src/app/shared/generated/enum/neptune-page-type-enum";
import { CustomRichTextComponent } from "src/app/shared/components/custom-rich-text/custom-rich-text.component";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { AsyncPipe } from "@angular/common";
import { Observable } from "rxjs";
import { IconComponent } from "src/app/shared/components/icon/icon.component";

@Component({
    selector: "app-home-index",
    templateUrl: "./home-index.component.html",
    styleUrls: ["./home-index.component.scss"],
    imports: [AlertDisplayComponent, RouterLink, CustomRichTextComponent, AsyncPipe, IconComponent],
})
export class HomeIndexComponent implements OnInit {
    public currentUser$: Observable<PersonDto>;

    public customRichTextTypeID: number = NeptunePageTypeEnum.SPAHomePage;

    constructor(
        private authenticationService: AuthenticationService,
        private router: Router,
        private route: ActivatedRoute
    ) {}

    public ngOnInit(): void {
        this.currentUser$ = this.authenticationService.getCurrentUser();
    }

    public login(): void {
        this.authenticationService.login();
    }

    public signUp(): void {
        this.authenticationService.signUp();
    }
}
