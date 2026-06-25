import { DatePipe } from "@angular/common";
import { Component, computed, inject, signal, Signal } from "@angular/core";
import { toSignal } from "@angular/core/rxjs-interop";
import { map, Observable, shareReplay } from "rxjs";
import { AuthenticationService } from "src/app/services/authentication.service";
import { DataHubService } from "src/app/shared/generated/api/data-hub.service";
import { HRUCharacteristicService } from "src/app/shared/generated/api/hru-characteristic.service";
import { ModelBasinService } from "src/app/shared/generated/api/model-basin.service";
import { OCTAPrioritizationService } from "src/app/shared/generated/api/octa-prioritization.service";
import { ParcelService } from "src/app/shared/generated/api/parcel.service";
import { PrecipitationZoneService } from "src/app/shared/generated/api/precipitation-zone.service";
import { RegionalSubbasinService } from "src/app/shared/generated/api/regional-subbasin.service";
import { RoleEnum } from "src/app/shared/generated/enum/role-enum";
import { NeptunePageTypeEnum } from "src/app/shared/generated/enum/neptune-page-type-enum";
import { DataHubLastUpdatedDto } from "src/app/shared/generated/model/data-hub-last-updated-dto";
import { CustomRichTextComponent } from "src/app/shared/components/custom-rich-text/custom-rich-text.component";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { AlertService } from "src/app/shared/services/alert.service";
import { ConfirmService } from "src/app/shared/services/confirm/confirm.service";
import { DataHubActionButtonComponent } from "../../components/data-hub-action-button/data-hub-action-button.component";
import { DataHubQuickLinksComponent } from "../../components/data-hub-quick-links/data-hub-quick-links.component";

type RefreshKey = "parcels" | "regional-subbasins" | "hru-characteristics" | "model-basins" | "precipitation-zones" | "octa-prioritizations";

@Component({
    selector: "county-gis-tab",
    standalone: true,
    imports: [DatePipe, CustomRichTextComponent, DataHubActionButtonComponent, DataHubQuickLinksComponent],
    templateUrl: "./county-gis-tab.component.html",
    styleUrl: "../../data-hub.component.scss",
})
export class CountyGisTabComponent {
    public NeptunePageTypeEnum = NeptunePageTypeEnum;

    private dataHubService = inject(DataHubService);
    private parcelService = inject(ParcelService);
    private regionalSubbasinService = inject(RegionalSubbasinService);
    private hruCharacteristicService = inject(HRUCharacteristicService);
    private modelBasinService = inject(ModelBasinService);
    private precipitationZoneService = inject(PrecipitationZoneService);
    private octaPrioritizationService = inject(OCTAPrioritizationService);
    private confirmService = inject(ConfirmService);
    private alertService = inject(AlertService);
    private authenticationService = inject(AuthenticationService);

    public lastUpdated = toSignal(
        this.dataHubService.getLastUpdatedDataHub().pipe(
            shareReplay(1),
            map((dto) => dto ?? ({} as DataHubLastUpdatedDto))
        ),
        { initialValue: null }
    );

    private currentUser = toSignal(this.authenticationService.currentUserSetObservable.pipe(map((u) => u ?? null)), { initialValue: null });
    public isAdmin: Signal<boolean> = computed(() => {
        const u = this.currentUser();
        return !!u && (u.RoleID === RoleEnum.Admin || u.RoleID === RoleEnum.SitkaAdmin);
    });

    public refreshing = signal<RefreshKey | null>(null);
    public adminOnlyTooltip = "Only Administrators can refresh OC Survey data.";

    public refresh(key: RefreshKey, displayName: string): void {
        this.confirmService
            .confirm({
                title: `Refresh ${displayName} from OC Survey`,
                message: `Are you sure you want to refresh the ${displayName} layer from OC Survey?<br /><br />This can take a little while to run.`,
                buttonTextYes: "Refresh",
                buttonTextNo: "Cancel",
                buttonClassYes: "btn-primary",
            })
            .then((confirmed) => {
                if (!confirmed) return;
                this.refreshing.set(key);
                this.callRefresh(key).subscribe({
                    next: () => {
                        this.refreshing.set(null);
                        this.alertService.pushAlert(new Alert(`${displayName} refresh has been queued.`, AlertContext.Success, true));
                    },
                    error: () => {
                        this.refreshing.set(null);
                        this.alertService.pushAlert(new Alert(`Failed to queue ${displayName} refresh.`, AlertContext.Danger, true));
                    },
                });
            });
    }

    private callRefresh(key: RefreshKey): Observable<unknown> {
        switch (key) {
            case "parcels":
                return this.parcelService.enqueueRefreshParcel();
            case "regional-subbasins":
                return this.regionalSubbasinService.enqueueRefreshRegionalSubbasin();
            case "hru-characteristics":
                return this.hruCharacteristicService.enqueueRefreshHRUCharacteristic();
            case "model-basins":
                return this.modelBasinService.enqueueRefreshModelBasin();
            case "precipitation-zones":
                return this.precipitationZoneService.enqueueRefreshPrecipitationZone();
            case "octa-prioritizations":
                return this.octaPrioritizationService.enqueueRefreshOCTAPrioritization();
        }
    }
}
