import { Component } from "@angular/core";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { AsyncPipe } from "@angular/common";
import { HRUCharacteristicService } from "src/app/shared/generated/api/hru-characteristic.service";
import { HRUCharacteristicDto } from "src/app/shared/generated/model/hru-characteristic-dto";
import { Observable } from "rxjs";
import { UtilityFunctionsService } from "src/app/services/utility-functions.service";
import { NeptunePageTypeEnum } from "src/app/shared/generated/enum/neptune-page-type-enum";
import { HruCharacteristicsGridComponent } from "src/app/shared/components/hru-characteristics-grid/hru-characteristics-grid.component";
import { AuthenticationService } from "src/app/services/authentication.service";
import { RoleEnum } from "src/app/shared/generated/enum/role-enum";
import { ConfirmService } from "src/app/shared/services/confirm/confirm.service";
import { AlertService } from "src/app/shared/services/alert.service";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";

@Component({
    selector: "hru-characteristics",
    standalone: true,
    imports: [PageHeaderComponent, AlertDisplayComponent, AsyncPipe, HruCharacteristicsGridComponent],
    templateUrl: "./hru-characteristics.component.html",
})
export class HRUCharacteristicsComponent {
    public hruCharacteristics$: Observable<HRUCharacteristicDto[]>;
    public customRichTextTypeID = NeptunePageTypeEnum.HRUCharacteristics;
    public isSitkaAdmin = false;

    constructor(
        private hruCharacteristicService: HRUCharacteristicService,
        private utilityFunctions: UtilityFunctionsService,
        private authenticationService: AuthenticationService,
        private confirmService: ConfirmService,
        private alertService: AlertService
    ) {}

    ngOnInit(): void {
        this.hruCharacteristics$ = this.hruCharacteristicService.listHRUCharacteristic();
        this.authenticationService.getCurrentUser().subscribe((user) => {
            this.isSitkaAdmin = user?.RoleID === RoleEnum.SitkaAdmin;
        });
    }

    refreshHRUCharacteristics(): void {
        this.confirmService
            .confirm({
                title: "Refresh HRU Characteristics",
                message: "Are you sure you want to refresh the HRU Characteristics? This can take several hours and will prevent other scheduled jobs from running in the meantime.",
                buttonTextYes: "Refresh",
                buttonTextNo: "Cancel",
                buttonClassYes: "btn-primary",
            })
            .then((confirmed) => {
                if (confirmed) {
                    this.hruCharacteristicService.enqueueRefreshHRUCharacteristic().subscribe(() => {
                        this.alertService.clearAlerts();
                        this.alertService.pushAlert(new Alert("HRU Characteristic refresh will run in the background.", AlertContext.Success));
                    });
                }
            });
    }
}
