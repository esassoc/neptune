import { Component, OnInit } from "@angular/core";
import { AgGridModule } from "ag-grid-angular";
import { ColDef, GridApi, ValueGetterParams } from "ag-grid-community";
import { UtilityFunctionsService } from "src/app/services/utility-functions.service";
import { PersonDto } from "src/app/shared/generated/model/person-dto";
import { AuthenticationService } from "src/app/services/authentication.service";
import { Router, RouterLink } from "@angular/router";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { AlertService } from "src/app/shared/services/alert.service";
import { CustomDropdownFilterComponent } from "src/app/shared/components/custom-dropdown-filter/custom-dropdown-filter.component";
import { environment } from "src/environments/environment";
import { ProjectService } from "src/app/shared/generated/api/project.service";
import { NeptunePageTypeEnum } from "src/app/shared/generated/enum/neptune-page-type-enum";
import { NeptuneGridComponent } from "src/app/shared/components/neptune-grid/neptune-grid.component";
import { ProjectDto } from "src/app/shared/generated/model/project-dto";
import { ConfirmService } from "src/app/shared/services/confirm/confirm.service";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { DropdownToggleDirective } from "src/app/shared/directives/dropdown-toggle.directive";
import { CommonModule } from "@angular/common";
import { Observable, Subject, catchError, finalize, of, shareReplay, startWith, switchMap, take } from "rxjs";

@Component({
    selector: "project-list",
    templateUrl: "./project-list.component.html",
    styleUrls: ["./project-list.component.scss"],
    imports: [CommonModule, DropdownToggleDirective, AgGridModule, NeptuneGridComponent, PageHeaderComponent, RouterLink],
})
export class ProjectListComponent implements OnInit {
    private currentUser: PersonDto;
    private refreshProjectsSubject = new Subject<void>();
    public projects$!: Observable<ProjectDto[]>;

    private gridApi: GridApi;
    public richTextTypeID = NeptunePageTypeEnum.HippocampProjectsList;
    public projectColumnDefs: Array<ColDef>;
    public defaultColDef: ColDef;
    public projectNameToDelete: string;
    public isLoadingDelete = false;

    constructor(
        private router: Router,
        private authenticationService: AuthenticationService,
        private alertService: AlertService,
        private projectService: ProjectService,
        private utilityFunctionsService: UtilityFunctionsService,
        private confirmService: ConfirmService
    ) {}

    ngOnInit(): void {
        this.authenticationService
            .getCurrentUser()
            .pipe(take(1))
            .subscribe((currentUser) => {
                this.currentUser = currentUser;

                this.createProjectGridColDefs();

                this.projects$ = this.refreshProjectsSubject.pipe(
                    startWith(void 0),
                    switchMap(() =>
                        this.projectService.listProject().pipe(
                            catchError(() => {
                                this.alertService.pushAlert(new Alert(`There was an error while loading projects. Please refresh the page and try again.`, AlertContext.Danger));
                                return of([] as ProjectDto[]);
                            })
                        )
                    ),
                    shareReplay({ bufferSize: 1, refCount: true })
                );
            });
    }

    public onGridReady(params) {
        this.gridApi = params.api;
    }

    private createProjectGridColDefs() {
        this.projectColumnDefs = [
            this.utilityFunctionsService.createActionsColumnDef((params: any) => {
                return [
                    { ActionName: "View", ActionIcon: "fas fa-file-alt", ActionHandler: () => this.router.navigate(["planning", "projects", params.data.ProjectID]) },
                    {
                        ActionName: "Edit",
                        ActionIcon: "fas fa-edit",
                        ActionHandler: () =>
                            this.router.navigateByUrl(`/planning/projects/edit/${params.data.ProjectID + (params.data.ShareOCTAM2Tier2Scores ? "/review-and-share" : "")}`),
                    },
                    { ActionName: "Delete", ActionIcon: "fas fa-trash text-danger", ActionHandler: () => this.deleteModal(params) },
                ];
            }),
            this.utilityFunctionsService.createLinkColumnDef("Project ID", "ProjectID", "ProjectID", {
                InRouterLink: "/planning/projects/",
            }),
            this.utilityFunctionsService.createLinkColumnDef("Project Name", "ProjectName", "ProjectID", {
                InRouterLink: "/planning/projects/",
            }),
            this.utilityFunctionsService.createLinkColumnDef("Organization", "Organization.OrganizationName", "Organization.OrganizationID", {
                InRouterLink: "/organizations/",
            }),
            this.utilityFunctionsService.createBasicColumnDef("Jurisdiction", "StormwaterJurisdiction.StormwaterJurisdictionName"),
            this.utilityFunctionsService.createDateColumnDef("Date Created", "DateCreated", "M/d/yyyy", { Width: 120 }),
            this.utilityFunctionsService.createBasicColumnDef("Project Description", "ProjectDescription", { MaxWidth: 400 }),
            {
                headerName: "Shared with OCTA M2 Tier 2 Grant Program",
                valueGetter: (params) => (params.data.ShareOCTAM2Tier2Scores ? "Yes" : "No"),
                filter: CustomDropdownFilterComponent,
                filterParams: { field: "ShareOCTAM2Tier2Scores" },
            },
            this.utilityFunctionsService.createDateColumnDef("Last Shared with OCTA M2 Tier 2 Grant Program", "OCTAM2Tier2ScoresLastSharedDate", "short"),
        ];

        this.defaultColDef = {
            filter: true,
            sortable: true,
            resizable: true,
        };
    }

    private deleteModal(params: ValueGetterParams<ProjectDto, any>) {
        const confirmOptions = {
            title: "Delete Project",
            message: `<p>You are about to delete ${params.data.ProjectName}.</p><p>Are you sure you wish to proceed?</p>`,
            buttonClassYes: "btn btn-danger",
            buttonTextYes: "Delete",
            buttonTextNo: "Cancel",
        };
        this.confirmService.confirm(confirmOptions).then((confirmed) => {
            if (confirmed) {
                this.isLoadingDelete = true;
                this.projectService
                    .deleteProject(params.data.ProjectID)
                    .pipe(
                        finalize(() => {
                            this.isLoadingDelete = false;
                        })
                    )
                    .subscribe(() => {
                        this.alertService.pushAlert(new Alert("Successfully deleted project", AlertContext.Success));
                        this.refreshProjectsSubject.next();
                    });
            }
        });
    }

    public downloadProjectModelResults() {
        this.projectService.downloadProjectsWithModelsProject().subscribe(
            (csv) => {
                //Create a fake object for us to click and download
                var a = document.createElement("a");
                a.href = URL.createObjectURL(csv);
                a.download = `project-modeled-results.csv`;
                document.body.appendChild(a);
                a.click();
                //Revoke the generated url so the blob doesn't hang in memory https://javascript.info/blob
                URL.revokeObjectURL(a.href);
                document.body.removeChild(a);
            },
            () => {
                this.alertService.pushAlert(new Alert(`There was an error while downloading the file. Please refresh the page and try again.`, AlertContext.Danger));
            }
        );
    }

    public downloadTreatmentBMPModelResults() {
        this.projectService.downloadTreatmentBMPsForProjectsProject().subscribe(
            (csv) => {
                //Create a fake object for us to click and download
                var a = document.createElement("a");
                a.href = URL.createObjectURL(csv);
                a.download = `project-BMP-modeled-results.csv`;
                document.body.appendChild(a);
                a.click();
                //Revoke the generated url so the blob doesn't hang in memory https://javascript.info/blob
                URL.revokeObjectURL(a.href);
                document.body.removeChild(a);
            },
            () => {
                this.alertService.pushAlert(new Alert(`There was an error while downloading the file. Please refresh the page and try again.`, AlertContext.Danger));
            }
        );
    }

    public downloadTreatmentBMPDelineationShapefile() {
        return (
            environment.geoserverMapServiceUrl +
            "/ows?service=WFS&version=2.0&request=GetFeature&typeName=OCStormwater:TreatmentBMPDelineation&outputFormat=shape-zip&SrsName=EPSG:4326"
        );
    }

    public downloadTreatmentBMPLocationPointShapefile() {
        return (
            environment.geoserverMapServiceUrl +
            "/ows?service=WFS&version=2.0&request=GetFeature&typeName=OCStormwater:TreatmentBMPPointLocation&outputFormat=shape-zip&SrsName=EPSG:4326"
        );
    }
}
