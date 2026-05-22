import { Component, OnInit, signal } from "@angular/core";
import { ActivatedRoute, Router, RouterLink } from "@angular/router";
import { AsyncPipe, DatePipe } from "@angular/common";
import { FormControl, FormsModule, ReactiveFormsModule } from "@angular/forms";
import { BehaviorSubject, map, Observable, shareReplay, tap } from "rxjs";
import * as L from "leaflet";

import { RegionalSubbasinRevisionRequestService } from "src/app/shared/generated/api/regional-subbasin-revision-request.service";
import { RegionalSubbasinRevisionRequestDto } from "src/app/shared/generated/model/regional-subbasin-revision-request-dto";
import { RegionalSubbasinRevisionRequestStatusEnum } from "src/app/shared/generated/enum/regional-subbasin-revision-request-status-enum";

import { AuthenticationService } from "src/app/services/authentication.service";
import { AlertService } from "src/app/shared/services/alert.service";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";

import { NeptuneMapComponent, NeptuneMapInitEvent } from "src/app/shared/components/leaflet/neptune-map/neptune-map.component";
import { RegionalSubbasinsLayerComponent } from "src/app/shared/components/leaflet/layers/regional-subbasins-layer/regional-subbasins-layer.component";
import { JurisdictionsLayerComponent } from "src/app/shared/components/leaflet/layers/jurisdictions-layer/jurisdictions-layer.component";
import { OverlayMode } from "src/app/shared/components/leaflet/layers/generic-wms-wfs-layer/overlay-mode.enum";

import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { FormFieldComponent, FormFieldType } from "src/app/shared/components/forms/form-field/form-field.component";
import { HttpClient } from "@angular/common/http";
import { environment } from "src/environments/environment";

@Component({
    selector: "revision-request-detail",
    templateUrl: "./revision-request-detail.component.html",
    styleUrl: "./revision-request-detail.component.scss",
    imports: [
        AsyncPipe,
        DatePipe,
        FormsModule,
        ReactiveFormsModule,
        RouterLink,
        NeptuneMapComponent,
        RegionalSubbasinsLayerComponent,
        JurisdictionsLayerComponent,
        PageHeaderComponent,
        AlertDisplayComponent,
        FormFieldComponent,
    ],
})
export class RevisionRequestDetailComponent implements OnInit {
    public OverlayMode = OverlayMode;
    public FormFieldType = FormFieldType;
    public StatusEnum = RegionalSubbasinRevisionRequestStatusEnum;

    public initData$: Observable<boolean>;
    public mapHeight = "550px";
    public map: L.Map;
    public layerControl: L.Control.Layers;
    public mapIsReady = false;

    public request = signal<RegionalSubbasinRevisionRequestDto | null>(null);
    public showCloseForm = signal<boolean>(false);
    public closeNotesControl = new FormControl<string>("", { nonNullable: true });

    private requestID: number;
    private geometryLayer: L.GeoJSON | null = null;
    private currentUserPersonID: number | null = null;

    private readonly savingSubject = new BehaviorSubject<boolean>(false);
    public readonly saving$ = this.savingSubject.asObservable();

    private readonly polygonStyle: L.PathOptions = { color: "yellow", fillOpacity: 0.4, opacity: 1 };

    constructor(
        private route: ActivatedRoute,
        private router: Router,
        private alertService: AlertService,
        private authenticationService: AuthenticationService,
        private httpClient: HttpClient,
        private regionalSubbasinRevisionRequestService: RegionalSubbasinRevisionRequestService
    ) {}

    public ngOnInit(): void {
        this.requestID = Number(this.route.snapshot.paramMap.get("regionalSubbasinRevisionRequestID"));
        this.authenticationService.getCurrentUser().subscribe((user) => (this.currentUserPersonID = user?.PersonID ?? null));
    }

    public handleMapReady(event: NeptuneMapInitEvent): void {
        this.map = event.map;
        this.layerControl = event.layerControl;
        this.mapIsReady = true;

        this.initData$ = this.regionalSubbasinRevisionRequestService.getRegionalSubbasinRevisionRequest(this.requestID).pipe(
            tap((dto) => {
                this.request.set(dto);
                this.renderGeometry(dto);
            }),
            map(() => true),
            shareReplay(1)
        );
    }

    private renderGeometry(dto: RegionalSubbasinRevisionRequestDto): void {
        if (this.geometryLayer) {
            this.map.removeLayer(this.geometryLayer);
            this.geometryLayer = null;
        }
        if (!dto.GeometryGeoJson) {
            return;
        }
        const feature = JSON.parse(dto.GeometryGeoJson);
        this.geometryLayer = L.geoJSON(feature, { style: this.polygonStyle }).addTo(this.map);
        try {
            this.map.flyToBounds(this.geometryLayer.getBounds(), { padding: [50, 50] });
        } catch {
            // empty bounds
        }
    }

    public canClose(): boolean {
        const dto = this.request();
        if (!dto || dto.RegionalSubbasinRevisionRequestStatusID === RegionalSubbasinRevisionRequestStatusEnum.Closed) {
            return false;
        }
        return this.authenticationService.isCurrentUserAnAdministrator() || this.currentUserPersonID === dto.RequestPersonID;
    }

    public canDownload(): boolean {
        const dto = this.request();
        if (!dto) {
            return false;
        }
        return this.authenticationService.doesCurrentUserHaveJurisdictionManagePermission();
    }

    public openCloseForm(): void {
        this.closeNotesControl.setValue("");
        this.showCloseForm.set(true);
    }

    public cancelClose(): void {
        this.showCloseForm.set(false);
    }

    public submitClose(): void {
        this.savingSubject.next(true);
        this.regionalSubbasinRevisionRequestService
            .closeRegionalSubbasinRevisionRequest(this.requestID, { CloseNotes: this.closeNotesControl.value || null })
            .subscribe({
                next: (dto) => {
                    this.savingSubject.next(false);
                    this.alertService.pushAlert(new Alert("Successfully closed the Regional Subbasin Revision Request.", AlertContext.Success, true));
                    this.request.set(dto);
                    this.showCloseForm.set(false);
                },
                error: () => this.savingSubject.next(false),
            });
    }

    public download(): void {
        const url = `${environment.mainAppApiUrl}/regional-subbasin-revision-requests/${this.requestID}/gdb`;
        this.httpClient.get(url, { responseType: "blob" }).subscribe({
            next: (blob) => {
                const objectUrl = window.URL.createObjectURL(blob);
                const a = document.createElement("a");
                a.href = objectUrl;
                a.download = `BMP_${this.request()?.TreatmentBMPID}_RevisionRequest.zip`;
                a.click();
                window.URL.revokeObjectURL(objectUrl);
            },
            error: () => this.alertService.pushAlert(new Alert("Failed to download the revision request geometry.", AlertContext.Danger, true)),
        });
    }

    public bmpDetailUrl(): string {
        const dto = this.request();
        return dto ? `/treatment-bmps/${dto.TreatmentBMPID}` : "#";
    }
}
