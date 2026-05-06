import { DatePipe } from "@angular/common";
import { Component, inject } from "@angular/core";
import { toSignal } from "@angular/core/rxjs-interop";
import { map, of, shareReplay } from "rxjs";
import { DataHubService } from "src/app/shared/generated/api/data-hub.service";
import { DataHubLastUpdatedDto } from "src/app/shared/generated/model/data-hub-last-updated-dto";
import { DataHubActionButtonComponent } from "../../components/data-hub-action-button/data-hub-action-button.component";
import { DataHubQuickLinksComponent } from "../../components/data-hub-quick-links/data-hub-quick-links.component";

@Component({
    selector: "county-gis-tab",
    standalone: true,
    imports: [DatePipe, DataHubActionButtonComponent, DataHubQuickLinksComponent],
    templateUrl: "./county-gis-tab.component.html",
    styleUrl: "../../data-hub.component.scss",
})
export class CountyGisTabComponent {
    private dataHubService = inject(DataHubService);

    public lastUpdated = toSignal(
        this.dataHubService.getLastUpdatedDataHub().pipe(
            shareReplay(1),
            map((dto) => dto ?? ({} as DataHubLastUpdatedDto))
        ),
        { initialValue: null }
    );

    public comingSoonTooltip = "Migration in progress.";
}
