import { Component, OnInit, OnChanges, SimpleChanges, ViewChild, TemplateRef, Input, inject } from "@angular/core";
import { Router, RouterLink } from "@angular/router";
import { AsyncPipe, CommonModule } from "@angular/common";
import { catchError, EMPTY, Observable, tap } from "rxjs";
import { DialogService } from "@ngneat/dialog";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { ColDef } from "ag-grid-community";
import { UtilityFunctionsService } from "src/app/services/utility-functions.service";
import { AuthenticationService } from "src/app/services/authentication.service";
import { AlertService } from "src/app/shared/services/alert.service";
import { NeptuneGridComponent } from "src/app/shared/components/neptune-grid/neptune-grid.component";
import { IconComponent } from "src/app/shared/components/icon/icon.component";
import { PersonDisplayDto, StormwaterJurisdictionGridDto, TreatmentBMPGridDto } from "src/app/shared/generated/model/models";
import { StormwaterJurisdictionService } from "src/app/shared/generated/api/stormwater-jurisdiction.service";
import { JurisdictionBasicsModalComponent, JurisdictionBasicsModalContext } from "./jurisdiction-basics-modal/jurisdiction-basics-modal.component";
import { JurisdictionUsersModalComponent, JurisdictionUsersModalContext } from "./jurisdiction-users-modal/jurisdiction-users-modal.component";

@Component({
    selector: "jurisdiction-detail",
    templateUrl: "./jurisdiction-detail.component.html",
    styleUrls: ["./jurisdiction-detail.component.scss"],
    standalone: true,
    imports: [CommonModule, RouterLink, AsyncPipe, PageHeaderComponent, AlertDisplayComponent, NeptuneGridComponent, IconComponent],
})
export class JurisdictionDetailComponent implements OnInit, OnChanges {
    private dialogService = inject(DialogService);
    private authenticationService = inject(AuthenticationService);
    private alertService = inject(AlertService);
    private router = inject(Router);

    public currentJurisdiction: StormwaterJurisdictionGridDto;
    private assignedPersonIDs: number[] = [];

    public get canManage(): boolean {
        return this.authenticationService.doesCurrentUserHaveJurisdictionManagePermission();
    }
    ngOnChanges(changes: SimpleChanges): void {
        if (changes["jurisdictionID"] && !changes["jurisdictionID"].firstChange) {
            this.loadData();
        }
    }

    @ViewChild("templateAbove", { static: true }) templateAbove!: TemplateRef<any>;
    @Input() jurisdictionID!: number;

    // Observables for async pipe
    jurisdiction$!: Observable<StormwaterJurisdictionGridDto>;
    users$: Observable<PersonDisplayDto[]>;
    treatmentBMPs$: Observable<TreatmentBMPGridDto[]>;
    treatmentBMPColumnDefs: Array<ColDef>;

    usersCol1: PersonDisplayDto[] = [];
    usersCol2: PersonDisplayDto[] = [];
    usersCol3: PersonDisplayDto[] = [];

    constructor(private stormwaterJurisdictionService: StormwaterJurisdictionService, private utilityFunctionsService: UtilityFunctionsService) {}

    ngOnInit(): void {
        this.treatmentBMPColumnDefs = [
            this.utilityFunctionsService.createLinkColumnDef("Name", "TreatmentBMPName", "TreatmentBMPID", {
                InRouterLink: "/treatment-bmps/",
            }),
            this.utilityFunctionsService.createBasicColumnDef("Jurisdiction", "StormwaterJurisdictionName"),
            this.utilityFunctionsService.createBasicColumnDef("Owner Organization", "OwnerOrganizationName"),
            this.utilityFunctionsService.createBasicColumnDef("Type", "TreatmentBMPTypeName"),
            this.utilityFunctionsService.createBasicColumnDef("Year Built", "YearBuilt"),
            this.utilityFunctionsService.createBasicColumnDef("Notes", "Notes"),
            this.utilityFunctionsService.createBasicColumnDef("Last Assessment Date", "LatestAssessmentDate"),
            this.utilityFunctionsService.createBasicColumnDef("Last Assessed Score", "LatestAssessmentScore"),
            this.utilityFunctionsService.createBasicColumnDef("# of Assessments", "NumberOfAssessments"),
            this.utilityFunctionsService.createBasicColumnDef("Last Maintenance Date", "LatestMaintenanceDate"),
            this.utilityFunctionsService.createBasicColumnDef("# of Maintenance Events", "NumberOfMaintenanceRecords"),
            this.utilityFunctionsService.createBasicColumnDef("Benchmark and Threshold Set?", "BenchmarkAndThresholdSet"),
            this.utilityFunctionsService.createBasicColumnDef("Required Lifespan of Installation", "TreatmentBMPLifespanTypeDisplayName"),
            this.utilityFunctionsService.createBasicColumnDef("Lifespan End Date (if Fixed End Date)", "TreatmentBMPLifespanEndDate"),
            this.utilityFunctionsService.createBasicColumnDef("Required Field Visits/Year", "RequiredFieldVisitsPerYear"),
            this.utilityFunctionsService.createBasicColumnDef("Required Post-Storm Field Visits/Year", "RequiredPostStormFieldVisitsPerYear"),
            this.utilityFunctionsService.createBasicColumnDef("Sizing Basis", "SizingBasisTypeDisplayName"),
            this.utilityFunctionsService.createBasicColumnDef("Trash Capture Status", "TrashCaptureStatusTypeDisplayName"),
            this.utilityFunctionsService.createBasicColumnDef("Trash Capture Effectiveness (%)", "TrashCaptureEffectiveness"),
            this.utilityFunctionsService.createBasicColumnDef("Delineation Type", "DelineationTypeDisplayName"),
        ];
        this.loadData();
    }

    private loadData(): void {
        this.jurisdiction$ = this.stormwaterJurisdictionService.getStormwaterJurisdiction(this.jurisdictionID).pipe(
            tap((jurisdiction) => (this.currentJurisdiction = jurisdiction)),
            // NPT-1061 item 4c: JE/JM hitting a jurisdiction they're not assigned to get a 403 from
            // the API; show the standard not-found/unauthorized alert and bounce to the list.
            catchError(() => {
                this.router.navigate(["/jurisdictions"]).then(() => this.alertService.pushNotFoundUnauthorizedAlert());
                return EMPTY;
            })
        );
        this.users$ = this.stormwaterJurisdictionService.listUsersStormwaterJurisdiction(this.jurisdictionID).pipe(
            tap((users) => {
                this.assignedPersonIDs = users.map((u) => u.PersonID);
                const third = Math.ceil(users.length / 3);
                this.usersCol1 = users.slice(0, third);
                this.usersCol2 = users.slice(third, third * 2);
                this.usersCol3 = users.slice(third * 2);
            })
        );
        this.treatmentBMPs$ = this.stormwaterJurisdictionService.listTreatmentBMPsStormwaterJurisdiction(this.jurisdictionID);
    }

    public openBasicsModal(): void {
        if (!this.currentJurisdiction) return;
        const ref = this.dialogService.open(JurisdictionBasicsModalComponent, {
            data: { jurisdiction: this.currentJurisdiction } as JurisdictionBasicsModalContext,
        });
        ref.afterClosed$.subscribe((result) => {
            if (result) this.loadData();
        });
    }

    public openUsersModal(): void {
        if (!this.currentJurisdiction) return;
        const ref = this.dialogService.open(JurisdictionUsersModalComponent, {
            data: {
                jurisdictionID: this.jurisdictionID,
                jurisdictionName: this.currentJurisdiction.StormwaterJurisdictionName,
                assignedPersonIDs: this.assignedPersonIDs,
            } as JurisdictionUsersModalContext,
        });
        ref.afterClosed$.subscribe((result) => {
            if (result) this.loadData();
        });
    }
}
