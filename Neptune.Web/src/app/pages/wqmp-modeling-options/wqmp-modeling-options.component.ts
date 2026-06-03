import { Component } from "@angular/core";
import { NeptunePageTypeEnum } from "src/app/shared/generated/enum/neptune-page-type-enum";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";

@Component({
    selector: "wqmp-modeling-options",
    standalone: true,
    imports: [PageHeaderComponent],
    templateUrl: "./wqmp-modeling-options.component.html",
})
export class WqmpModelingOptionsComponent {
    public customRichTextTypeID: number = NeptunePageTypeEnum.WQMPModelingOptions;
}
