import { Component } from "@angular/core";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { NeptuneGridComponent } from "src/app/shared/components/neptune-grid/neptune-grid.component";
import { AsyncPipe } from "@angular/common";
import { ColDef } from "ag-grid-community";
import { Observable } from "rxjs";
import { OrganizationDto } from "src/app/shared/generated/model/organization-dto";
import { UtilityFunctionsService } from "src/app/services/utility-functions.service";
import { OrganizationService } from "src/app/shared/generated/api/organization.service";
import { DialogService } from "@ngneat/dialog";
import { AlertService } from "src/app/shared/services/alert.service";
import { ConfirmService } from "src/app/shared/services/confirm/confirm.service";
import { OrganizationModalComponent } from "./organization-modal/organization-modal.component";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { NeptunePageTypeEnum } from "src/app/shared/generated/enum/neptune-page-type-enum";

@Component({
    selector: "organizations",
    standalone: true,
    imports: [PageHeaderComponent, AlertDisplayComponent, NeptuneGridComponent, AsyncPipe],
    templateUrl: "./organizations.component.html",
    styleUrl: "./organizations.component.scss",
})
export class OrganizationsComponent {
    public organizations$: Observable<OrganizationDto[]>;
    public columnDefs: ColDef[];
    public customRichTextTypeID = NeptunePageTypeEnum.OrganizationsList;

    constructor(
        private utilityFunctions: UtilityFunctionsService,
        private dialogService: DialogService,
        private alertService: AlertService,
        private confirmService: ConfirmService,
        private organizationService: OrganizationService
    ) {}

    ngOnInit(): void {
        this.columnDefs = [
            this.utilityFunctions.createActionsColumnDef((params: any) => [
                {
                    ActionName: "Edit",
                    ActionIcon: "fa fa-edit",
                    ActionHandler: () => this.openEditModal(params.data),
                },
                {
                    ActionName: "Delete",
                    ActionIcon: "fa fa-trash text-danger",
                    ActionHandler: () => this.deleteOrganization(params.data),
                },
            ]),
            this.utilityFunctions.createLinkColumnDef("Name", "OrganizationName", "OrganizationID", {
                InRouterLink: "/organizations/",
            }),
            this.utilityFunctions.createBasicColumnDef("Short Name", "OrganizationShortName"),
            this.utilityFunctions.createBasicColumnDef("URL", "OrganizationUrl"),
            this.utilityFunctions.createBasicColumnDef("Type", "OrganizationType.OrganizationTypeName"),
            this.utilityFunctions.createBasicColumnDef("Primary Contact", "PrimaryContactPerson.FullName"),
            this.utilityFunctions.createBooleanColumnDef("Active?", "IsActive"),
        ];
        this.organizations$ = this.organizationService.listOrganization();
    }

    openAddModal() {
        const dialogRef = this.dialogService.open(OrganizationModalComponent, {
            data: {
                mode: "add",
                organization: null,
            },
        });
        dialogRef.afterClosed$.subscribe((result) => {
            if (result) {
                this.alertService.clearAlerts();
                this.alertService.pushAlert(new Alert("Organization added successfully.", AlertContext.Success));
                this.organizations$ = this.organizationService.listOrganization();
            }
        });
    }

    openEditModal(organization: OrganizationDto) {
        const dialogRef = this.dialogService.open(OrganizationModalComponent, {
            data: {
                mode: "edit",
                organization,
            },
        });
        dialogRef.afterClosed$.subscribe((result) => {
            if (result) {
                this.alertService.clearAlerts();
                this.alertService.pushAlert(new Alert("Organization updated successfully.", AlertContext.Success));
                this.organizations$ = this.organizationService.listOrganization();
            }
        });
    }

    deleteOrganization(organization: OrganizationDto) {
        this.confirmService
            .confirm({
                title: "Delete Organization",
                message: `Are you sure you want to delete organization '<strong>${organization.OrganizationName}</strong>'?`,
                buttonTextYes: "Delete",
                buttonTextNo: "Cancel",
                buttonClassYes: "btn-danger",
            })
            .then((confirmed) => {
                if (confirmed) {
                    this.organizationService.deleteOrganization(organization.OrganizationID).subscribe(() => {
                        this.alertService.clearAlerts();
                        this.alertService.pushAlert(new Alert("Organization deleted successfully.", AlertContext.Success));
                        this.organizations$ = this.organizationService.listOrganization();
                    });
                }
            });
    }
}
