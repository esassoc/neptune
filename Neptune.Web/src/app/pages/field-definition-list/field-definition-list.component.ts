import { Component, OnInit, ChangeDetectorRef, ViewChild } from "@angular/core";
import { AuthenticationService } from "src/app/services/authentication.service";
import { LinkRendererComponent } from "src/app/shared/components/ag-grid/link-renderer/link-renderer.component";
import { ColDef } from "ag-grid-community";
import { AgGridAngular, AgGridModule } from "ag-grid-angular";
import { FieldDefinitionDto, PersonDto } from "src/app/shared/generated/model/models";
import { FieldDefinitionService } from "src/app/shared/generated/api/field-definition.service";
import { NeptunePageTypeEnum } from "src/app/shared/generated/enum/neptune-page-type-enum";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { NeptuneGridComponent } from "src/app/shared/components/neptune-grid/neptune-grid.component";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";

@Component({
    selector: "field-definition-list",
    templateUrl: "./field-definition-list.component.html",
    styleUrls: ["./field-definition-list.component.scss"],
    imports: [AlertDisplayComponent, AgGridModule, NeptuneGridComponent, PageHeaderComponent],
})
export class FieldDefinitionListComponent implements OnInit {
    @ViewChild("fieldDefinitionsGrid") fieldDefinitionsGrid: AgGridAngular;

    private currentUser: PersonDto;

    public fieldDefinitions: Array<FieldDefinitionDto>;
    public richTextTypeID: number = NeptunePageTypeEnum.HippocampLabelsAndDefinitionsList;

    public rowData = [];
    public columnDefs: ColDef[];

    constructor(private fieldDefinitionService: FieldDefinitionService, private authenticationService: AuthenticationService, private cdr: ChangeDetectorRef) {}

    ngOnInit() {
        this.authenticationService.getCurrentUser().subscribe((currentUser) => {
            this.currentUser = currentUser;
            this.fieldDefinitionsGrid?.api.showLoadingOverlay();
            this.fieldDefinitionService.listFieldDefinition().subscribe((fieldDefinitions) => {
                this.fieldDefinitions = fieldDefinitions;
                this.rowData = fieldDefinitions;
                this.fieldDefinitionsGrid.api.hideOverlay();
                this.cdr.detectChanges();
            });
            this.columnDefs = [
                {
                    headerName: "Label",
                    valueGetter: function (params: any) {
                        return { LinkValue: params.data.FieldDefinitionType.FieldDefinitionTypeID, LinkDisplay: params.data.FieldDefinitionType.FieldDefinitionTypeDisplayName };
                    },
                    cellRenderer: LinkRendererComponent,
                    cellRendererParams: { inRouterLink: "/labels-and-definitions/" },
                    filterValueGetter: function (params: any) {
                        return params.data.FieldDefinitionType.FieldDefinitionTypeDisplayName;
                    },
                    comparator: function (id1: any, id2: any) {
                        let link1 = id1.LinkDisplay;
                        let link2 = id2.LinkDisplay;
                        if (link1 < link2) {
                            return -1;
                        }
                        if (link1 > link2) {
                            return 1;
                        }
                        return 0;
                    },
                    sortable: true,
                    filter: true,
                    width: 200,
                },
                {
                    headerName: "Definition",
                    field: "FieldDefinitionValue",
                    valueGetter: (params: any) => htmlToExcerpt(params.data.FieldDefinitionValue),
                    sortable: true,
                    filter: true,
                    width: 900,
                    tooltipValueGetter: (params: any) => htmlToExcerpt(params.data.FieldDefinitionValue),
                },
            ];

            this.columnDefs.forEach((x) => {
                x.resizable = true;
            });
        });
    }

    ngOnDestroy(): void {
        this.cdr.detach();
    }
}

function htmlToExcerpt(html: string | null | undefined): string {
    if (!html) return "";
    const temp = document.createElement("div");
    temp.innerHTML = html;
    return (temp.textContent || temp.innerText || "").replace(/\s+/g, " ").trim();
}
