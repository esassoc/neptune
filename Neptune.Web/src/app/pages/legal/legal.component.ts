import { Component } from "@angular/core";
import { NeptunePageTypeEnum } from "src/app/shared/generated/enum/neptune-page-type-enum";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";

@Component({
    selector: "legal",
    standalone: true,
    imports: [PageHeaderComponent],
    templateUrl: "./legal.component.html",
})
export class LegalComponent {
    public customRichTextTypeID: number = NeptunePageTypeEnum.Legal;
}
