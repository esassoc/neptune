import { Component } from "@angular/core";
import { NeptunePageTypeEnum } from "src/app/shared/generated/enum/neptune-page-type-enum";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";

@Component({
    selector: "modeling-calculations",
    standalone: true,
    imports: [PageHeaderComponent],
    templateUrl: "./modeling-calculations.component.html",
})
export class ModelingCalculationsComponent {
    public customRichTextTypeID: number = NeptunePageTypeEnum.AboutModelingBMPPerformance;
}
