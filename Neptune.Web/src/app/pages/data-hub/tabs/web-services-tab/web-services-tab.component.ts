import { DatePipe } from "@angular/common";
import { Component, computed, inject, signal, Signal } from "@angular/core";
import { takeUntilDestroyed, toSignal } from "@angular/core/rxjs-interop";
import { map } from "rxjs";
import { environment } from "src/environments/environment";
import { AuthenticationService } from "src/app/services/authentication.service";
import { UserService } from "src/app/shared/generated/api/user.service";
import { NeptunePageTypeEnum } from "src/app/shared/generated/enum/neptune-page-type-enum";
import { CustomRichTextComponent } from "src/app/shared/components/custom-rich-text/custom-rich-text.component";
import { CopyToClipboardDirective } from "src/app/shared/directives/copy-to-clipboard.directive";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { AlertService } from "src/app/shared/services/alert.service";
import { ConfirmService } from "src/app/shared/services/confirm/confirm.service";
import { DataHubQuickLinksComponent } from "../../components/data-hub-quick-links/data-hub-quick-links.component";

@Component({
    selector: "web-services-tab",
    standalone: true,
    imports: [CustomRichTextComponent, CopyToClipboardDirective, DataHubQuickLinksComponent, DatePipe],
    templateUrl: "./web-services-tab.component.html",
    styleUrls: ["../../data-hub.component.scss", "./web-services-tab.component.scss"],
})
export class WebServicesTabComponent {
    public NeptunePageTypeEnum = NeptunePageTypeEnum;
    public scalarUrl = environment.externalApiScalarUrl;

    private authenticationService = inject(AuthenticationService);
    private userService = inject(UserService);
    private alertService = inject(AlertService);
    private confirmService = inject(ConfirmService);

    private currentUser = toSignal(this.authenticationService.currentUserSetObservable.pipe(map((u) => u ?? null)), { initialValue: null });

    public webServiceToken: Signal<string | null> = computed(() => this.currentUser()?.WebServiceAccessToken ?? null);
    public personID: Signal<number | null> = computed(() => this.currentUser()?.PersonID ?? null);
    public lastWebServiceAccessDate: Signal<string | null> = computed(() => this.currentUser()?.LastWebServiceAccessDate ?? null);
    public busy = signal(false);

    public generateToken(): void {
        const id = this.personID();
        if (id == null || this.busy()) return;
        this.busy.set(true);
        this.userService.generateWebServiceTokenUser(id).subscribe({
            next: (newToken) => {
                const user = this.currentUser();
                if (user) {
                    this.authenticationService.refreshUserInfo({ ...user, WebServiceAccessToken: newToken });
                }
                this.busy.set(false);
                this.alertService.pushAlert(new Alert("Web service token generated.", AlertContext.Success, true));
            },
            error: () => {
                this.busy.set(false);
                this.alertService.pushAlert(new Alert("Failed to generate web service token.", AlertContext.Danger, true));
            },
        });
    }

    public regenerateToken(): void {
        if (this.busy()) return;
        this.confirmService
            .confirm({
                title: "Regenerate web service token?",
                message:
                    "Your current token will stop working immediately. Any PowerBI dashboards or external tools using the old token must be updated to the new one.<br /><br />Continue?",
                buttonTextYes: "Regenerate",
                buttonTextNo: "Cancel",
                buttonClassYes: "btn-primary",
            })
            .then((confirmed) => {
                if (!confirmed) return;
                this.generateToken();
            });
    }
}
