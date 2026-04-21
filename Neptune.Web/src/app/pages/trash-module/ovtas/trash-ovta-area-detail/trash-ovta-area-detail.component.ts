import { Component } from "@angular/core";
import { ActivatedRoute, Router, RouterLink } from "@angular/router";
import { Input } from "@angular/core";
import { BehaviorSubject, Observable, switchMap, tap } from "rxjs";
import { OnlandVisualTrashAssessmentAreaService } from "src/app/shared/generated/api/onland-visual-trash-assessment-area.service";
import { OnlandVisualTrashAssessmentAreaDetailDto } from "src/app/shared/generated/model/onland-visual-trash-assessment-area-detail-dto";
import { PageHeaderComponent } from "../../../../shared/components/page-header/page-header.component";
import { AlertDisplayComponent } from "../../../../shared/components/alert-display/alert-display.component";
import { AsyncPipe, DatePipe } from "@angular/common";
import { FieldDefinitionComponent } from "../../../../shared/components/field-definition/field-definition.component";
import { NeptuneGridComponent } from "../../../../shared/components/neptune-grid/neptune-grid.component";
import { ColDef } from "ag-grid-community";
import { UtilityFunctionsService } from "src/app/services/utility-functions.service";
import { NeptuneMapComponent, NeptuneMapInitEvent } from "../../../../shared/components/leaflet/neptune-map/neptune-map.component";
import * as L from "leaflet";
import { TransectLineLayerComponent } from "../../../../shared/components/leaflet/layers/transect-line-layer/transect-line-layer.component";
import { UpdateOvtaAreaDetailsModalComponent, UpdateOvtaAreaModalContext } from "../update-ovta-area-details-modal/update-ovta-area-details-modal.component";
import { OnlandVisualTrashAssessmentGridDto } from "src/app/shared/generated/model/onland-visual-trash-assessment-grid-dto";
import { LoadingDirective } from "src/app/shared/directives/loading.directive";
import { ConfirmService } from "src/app/shared/services/confirm/confirm.service";
import { OnlandVisualTrashAssessmentService } from "src/app/shared/generated/api/onland-visual-trash-assessment.service";
import { AlertService } from "src/app/shared/services/alert.service";
import { OnlandVisualTrashAssessmentSimpleDto } from "src/app/shared/generated/model/models";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { OnlandVisualTrashAssessmentStatusEnum } from "src/app/shared/generated/enum/onland-visual-trash-assessment-status-enum";
import { OvtaAreaLayerComponent } from "../../../../shared/components/leaflet/layers/ovta-area-layer/ovta-area-layer.component";
import { DialogService } from "@ngneat/dialog";

@Component({
    selector: "trash-ovta-area-detail",
    imports: [
        PageHeaderComponent,
        AlertDisplayComponent,
        AsyncPipe,
        FieldDefinitionComponent,
        NeptuneGridComponent,
        NeptuneMapComponent,
        TransectLineLayerComponent,
        RouterLink,
        LoadingDirective,
        OvtaAreaLayerComponent,
    ],
    templateUrl: "./trash-ovta-area-detail.component.html",
    styleUrl: "./trash-ovta-area-detail.component.scss",
})
export class TrashOvtaAreaDetailComponent {
    public onlandVisualTrashAssessmentArea$: Observable<OnlandVisualTrashAssessmentAreaDetailDto>;
    public ovtaColumnDefs: ColDef[];
    public onlandVisualTrashAssessments$: Observable<OnlandVisualTrashAssessmentGridDto[]>;

    public refreshOVTAAreasTrigger: BehaviorSubject<void> = new BehaviorSubject(null);
    public refreshOVTAAreasTrigger$: Observable<void> = this.refreshOVTAAreasTrigger.asObservable();

    public refreshOVTAGridTrigger: BehaviorSubject<void> = new BehaviorSubject(null);
    public refreshOVTAGridTrigger$: Observable<void> = this.refreshOVTAGridTrigger.asObservable();

    public map: L.Map;
    public mapIsReady: boolean = false;
    public layerControl: L.Control.Layers;

    public isLoading: boolean;
    public isLoadingGrid: boolean = true;

    @Input() onlandVisualTrashAssessmentAreaID!: number;
    constructor(
        private onlandVisualTrashAssessmentService: OnlandVisualTrashAssessmentService,
        private onlandVisualTrashAssessmentAreaService: OnlandVisualTrashAssessmentAreaService,
        private utilityFunctionsService: UtilityFunctionsService,
        private alertService: AlertService,
        private confirmService: ConfirmService,
        private router: Router,
        private datePipe: DatePipe,
        private dialogService: DialogService
    ) {}

    ngOnInit(): void {
        this.ovtaColumnDefs = [
            this.utilityFunctionsService.createActionsColumnDef((params: any) => {
                const actions = [
                    { ActionName: "View", ActionHandler: () => this.router.navigate(["trash", "onland-visual-trash-assessments", params.data.OnlandVisualTrashAssessmentID]) },
                    {
                        ActionName: params.data.OnlandVisualTrashAssessmentStatusID == OnlandVisualTrashAssessmentStatusEnum.Complete ? "Return to Edit" : "Edit",
                        ActionIcon: "fas fa-edit",
                        ActionHandler: () =>
                            params.data.OnlandVisualTrashAssessmentStatusID == OnlandVisualTrashAssessmentStatusEnum.Complete
                                ? this.confirmEditOVTA(params.data.OnlandVisualTrashAssessmentID, params.data.CompletedDate)
                                : this.router.navigateByUrl(`/trash/onland-visual-trash-assessments/edit/${params.data.OnlandVisualTrashAssessmentID}/record-observations`),
                    },
                ];
                if (params.data.OnlandVisualTrashAssessmentStatusID != OnlandVisualTrashAssessmentStatusEnum.Complete) {
                    actions.push({
                        ActionName: "Delete",
                        ActionIcon: "fas fa-trash text-danger",
                        ActionHandler: () => this.deleteOVTA(params.data.OnlandVisualTrashAssessmentID, params.data.CreatedDate),
                    });
                }
                return actions;
            }),
            this.utilityFunctionsService.createLinkColumnDef("Assessment ID", "OnlandVisualTrashAssessmentID", "OnlandVisualTrashAssessmentID", {
                InRouterLink: "../../onland-visual-trash-assessments/",
            }),
            this.utilityFunctionsService.createBasicColumnDef("Assessment Score", "OnlandVisualTrashAssessmentScoreName"),
            this.utilityFunctionsService.createBasicColumnDef("Assessment Type", "IsProgressAssessment", { CustomDropdownFilterField: "IsProgressAssessment" }),
            this.utilityFunctionsService.createDateColumnDef("Last Assessment Date", "CompletedDate", "shortDate"),
            this.utilityFunctionsService.createBasicColumnDef("Status", "OnlandVisualTrashAssessmentStatusName", {
                CustomDropdownFilterField: "OnlandVisualTrashAssessmentStatusName",
            }),
            this.utilityFunctionsService.createBasicColumnDef("Jurisdiction", "StormwaterJurisdictionName", {
                CustomDropdownFilterField: "StormwaterJurisdictionName",
            }),
            this.utilityFunctionsService.createBasicColumnDef("Created By", "CreatedByPersonFullName"),
            this.utilityFunctionsService.createDateColumnDef("Created On", "CreatedDate", "short"),
        ];

        this.onlandVisualTrashAssessmentArea$ = this.refreshOVTAAreasTrigger$.pipe(
            tap(() => {
                this.isLoading = true;
            }),
            switchMap(() => this.onlandVisualTrashAssessmentAreaService.getOnlandVisualTrashAssessmentArea(this.onlandVisualTrashAssessmentAreaID)),
            tap(() => {
                this.isLoading = false;
            })
        );

        this.onlandVisualTrashAssessments$ = this.refreshOVTAGridTrigger$.pipe(
            tap(() => (this.isLoadingGrid = true)),
            switchMap(() => this.onlandVisualTrashAssessmentAreaService.listAssessmentsByOVTAIDOnlandVisualTrashAssessmentArea(this.onlandVisualTrashAssessmentAreaID)),
            tap(() => (this.isLoadingGrid = false))
        );
    }

    public handleMapReady(event: NeptuneMapInitEvent): void {
        this.map = event.map;
        this.layerControl = event.layerControl;
        this.mapIsReady = true;
    }

    public addNewOVTA(onlandVisualTrashAssessmentAreaID: number, onlandVisualTrashAssessmentAreaName: string, stormwaterJurisdictionID: number) {
        const modalContents = `<p>You are about to begin a new OVTA for Assessment Area: ${onlandVisualTrashAssessmentAreaName}. 
        This will create a new Assessment record and allow you to start entering Assessment Observations on the next page.</p>`;
        this.confirmService
            .confirm({ buttonClassYes: "btn-primary", buttonTextYes: "Continue", buttonTextNo: "Cancel", title: "Add New OVTA", message: modalContents })
            .then((confirmed) => {
                if (confirmed) {
                    var onlandVisualTrashAssessmentSimpleDto = new OnlandVisualTrashAssessmentSimpleDto();
                    onlandVisualTrashAssessmentSimpleDto.OnlandVisualTrashAssessmentAreaID = onlandVisualTrashAssessmentAreaID;
                    onlandVisualTrashAssessmentSimpleDto.StormwaterJurisdictionID = stormwaterJurisdictionID;
                    onlandVisualTrashAssessmentSimpleDto.AssessingNewArea = false;
                    this.onlandVisualTrashAssessmentService.createOnlandVisualTrashAssessment(onlandVisualTrashAssessmentSimpleDto).subscribe((response) => {
                        this.alertService.clearAlerts();
                        this.alertService.pushAlert(new Alert("The OVTA was successfully created.", AlertContext.Success));
                        this.router.navigate([`/trash/onland-visual-trash-assessments/edit/${response.OnlandVisualTrashAssessmentID}/record-observations`]);
                    });
                }
            });
    }

    public confirmEditOVTA(onlandVisualTrashAssessmentID: number, completedDate: string) {
        const modalContents = `<p>This OVTA was finalized on ${this.datePipe.transform(
            completedDate,
            "MM/dd/yyyy"
        )}. Are you sure you want to revert this OVTA to the \"In Progress\" status?</p>`;
        this.confirmService
            .confirm({ buttonClassYes: "btn-primary", buttonTextYes: "Continue", buttonTextNo: "Cancel", title: "Return OVTA to Edit", message: modalContents })
            .then((confirmed) => {
                if (confirmed) {
                    this.onlandVisualTrashAssessmentService.editStatusToAllowEditOnlandVisualTrashAssessment(onlandVisualTrashAssessmentID).subscribe((response) => {
                        this.alertService.clearAlerts();
                        this.alertService.pushAlert(new Alert('The OVTA was successfully returned to the "In Progress" status.', AlertContext.Success));
                        this.router.navigateByUrl(`/trash/onland-visual-trash-assessments/edit/${onlandVisualTrashAssessmentID}/record-observations`);
                    });
                }
            });
    }

    public deleteOVTA(onlandVisualTrashAssessmentID: number, createdDate: string) {
        const modalContents = `<p>Are you sure you want to delete the assessment from ${this.datePipe.transform(createdDate, "MM/dd/yyyy")}?</p>`;
        this.confirmService
            .confirm({ buttonClassYes: "btn-primary", buttonTextYes: "Delete", buttonTextNo: "Cancel", title: "Delete OVTA", message: modalContents })
            .then((confirmed) => {
                if (confirmed) {
                    this.onlandVisualTrashAssessmentService.deleteOnlandVisualTrashAssessment(onlandVisualTrashAssessmentID).subscribe(() => {
                        this.alertService.clearAlerts();
                        this.alertService.pushAlert(new Alert("Your OVTA was successfully deleted.", AlertContext.Success));
                        this.refreshOVTAGridTrigger.next();
                        this.refreshOVTAAreasTrigger.next();
                    });
                }
            });
    }

    public editLocation(assessmentsCount: number) {
        const modalContents =
            assessmentsCount < 2
                ? `<p>Any changes you make to the Assessment Area will apply to all future assessments.</p>`
                : `<p>Any changes you make to the Assessment Area will apply to the ${assessmentsCount} Assessments associated with this area. Proceed?</p>`;
        this.confirmService
            .confirm({ buttonClassYes: "btn-primary", buttonTextYes: "Continue", buttonTextNo: "Cancel", title: "Edit Location", message: modalContents })
            .then((confirmed) => {
                if (confirmed) {
                    this.router.navigate(`trash/onland-visual-trash-assessment-areas/${this.onlandVisualTrashAssessmentAreaID}/edit-location`.split("/"));
                }
            });
    }

    updateOVTAAreaDetails(ovtaAreaDto: OnlandVisualTrashAssessmentAreaDetailDto) {
        const dialogRef = this.dialogService.open(UpdateOvtaAreaDetailsModalComponent, {
            data: {
                OnlandVisualTrashAssessmentAreaID: ovtaAreaDto.OnlandVisualTrashAssessmentAreaID,
                OnlandVisualTrashAssessmentAreaName: ovtaAreaDto.OnlandVisualTrashAssessmentAreaName,
                AssessmentAreaDescription: ovtaAreaDto.AssessmentAreaDescription,
            } as UpdateOvtaAreaModalContext,
        });

        dialogRef.afterClosed$.subscribe((result) => {
            if (result) {
                this.alertService.clearAlerts();
                this.alertService.pushAlert(new Alert("Successfully updated Assessment Area.", AlertContext.Success));
                this.refreshOVTAAreasTrigger.next();
            }
        });
    }

    public getLastAssessmentDate(onlandVisualTrashAssessments: OnlandVisualTrashAssessmentGridDto[]): string {
        const completedAssessments = onlandVisualTrashAssessments.filter((x) => x.OnlandVisualTrashAssessmentStatusID == OnlandVisualTrashAssessmentStatusEnum.Complete);
        if (completedAssessments.length > 0) {
            const latestAssessmentDate = completedAssessments.sort((a, b) => {
                if (a.CompletedDate < b.CompletedDate) {
                    return 1;
                }
                if (b.CompletedDate < a.CompletedDate) {
                    return -1;
                }
                return 0;
            })[0].CompletedDate;
            return this.datePipe.transform(latestAssessmentDate, "MM/dd/yyyy");
        }
        return "No completed assessments";
    }
}
