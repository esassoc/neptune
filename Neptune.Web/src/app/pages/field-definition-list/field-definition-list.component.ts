import { Component, OnInit } from "@angular/core";
import { LinkRendererComponent } from "src/app/shared/components/ag-grid/link-renderer/link-renderer.component";
import { ColDef } from "ag-grid-community";
import { AsyncPipe } from "@angular/common";
import { Observable } from "rxjs";
import { FieldDefinitionDto } from "src/app/shared/generated/model/models";
import { FieldDefinitionService } from "src/app/shared/generated/api/field-definition.service";
import { NeptunePageTypeEnum } from "src/app/shared/generated/enum/neptune-page-type-enum";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { NeptuneGridComponent } from "src/app/shared/components/neptune-grid/neptune-grid.component";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";

@Component({
    selector: "field-definition-list",
    templateUrl: "./field-definition-list.component.html",
    styleUrls: ["./field-definition-list.component.scss"],
    imports: [AlertDisplayComponent, AsyncPipe, NeptuneGridComponent, PageHeaderComponent],
})
export class FieldDefinitionListComponent implements OnInit {
    public richTextTypeID: number = NeptunePageTypeEnum.HippocampLabelsAndDefinitionsList;

    public fieldDefinitions$: Observable<FieldDefinitionDto[]>;
    public columnDefs: ColDef[];

    constructor(private fieldDefinitionService: FieldDefinitionService) {}

    ngOnInit() {
        this.fieldDefinitions$ = this.fieldDefinitionService.listFieldDefinition();

        this.columnDefs = [
            {
                headerName: "Label",
                valueGetter: (params: any) => ({
                    // NPT-999: link target is `/field-definitions/{id}/edit` per AC. LinkValue
                    // carries both the ID and the trailing `edit` segment so the LinkRenderer's
                    // `inRouterLink` + LinkValue concatenation produces the full canonical URL
                    // without needing a custom cell renderer for the trailing path piece.
                    LinkValue: `${params.data.FieldDefinitionType.FieldDefinitionTypeID}/edit`,
                    LinkDisplay: params.data.FieldDefinitionType.FieldDefinitionTypeDisplayName,
                }),
                cellRenderer: LinkRendererComponent,
                cellRendererParams: { inRouterLink: "/field-definitions/" },
                filterValueGetter: (params: any) => params.data.FieldDefinitionType.FieldDefinitionTypeDisplayName,
                comparator: (id1: any, id2: any) => {
                    if (id1.LinkDisplay < id2.LinkDisplay) return -1;
                    if (id1.LinkDisplay > id2.LinkDisplay) return 1;
                    return 0;
                },
                sortable: true,
                filter: true,
                resizable: true,
                width: 200,
            },
            {
                headerName: "Definition",
                field: "FieldDefinitionValue",
                valueGetter: (params: any) => htmlToExcerpt(params.data.FieldDefinitionValue),
                sortable: true,
                filter: true,
                resizable: true,
                width: 900,
                tooltipValueGetter: (params: any) => htmlToExcerpt(params.data.FieldDefinitionValue),
            },
        ];
    }
}

function htmlToExcerpt(html: string | null | undefined): string {
    if (!html) return "";
    const temp = document.createElement("div");
    temp.innerHTML = html;
    return (temp.textContent || temp.innerText || "").replace(/\s+/g, " ").trim();
}
