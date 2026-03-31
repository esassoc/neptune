import { Component, Input } from "@angular/core";
import { BtnGroupRadioInputComponent } from "src/app/shared/components/inputs/btn-group-radio-input/btn-group-radio-input.component";
import { DecimalPipe } from "@angular/common";
import { NeptuneModelingResultSigFigPipe } from "src/app/shared/pipes/neptune-modeling-result-sig-fig.pipe";
import { ProjectLoadReducingResultDto } from "src/app/shared/generated/model/project-load-reducing-result-dto";
import { FieldDefinitionComponent } from "../field-definition/field-definition.component";

@Component({
    selector: "modeled-bmp-performance",
    templateUrl: "./modeled-bmp-performance.component.html",
    styleUrls: ["./modeled-bmp-performance.component.scss"],
    standalone: true,
    imports: [BtnGroupRadioInputComponent, DecimalPipe, NeptuneModelingResultSigFigPipe, FieldDefinitionComponent],
})
export class ModeledBmpPerformanceComponent {
    @Input() loadReducingResult!: ProjectLoadReducingResultDto;
    @Input() selectedTreatmentBMPID?: number | null;
    @Input() siteRunoffLabel?: string;
    public ModeledPerformanceDisplayTypeEnum = ModeledPerformanceDisplayTypeEnum;
    public activeID: ModeledPerformanceDisplayTypeEnum = ModeledPerformanceDisplayTypeEnum.Total;
    public activeTab: string = "Total";
    public tabs = [
        { label: "Total", value: "Total" },
        { label: "Dry", value: "Dry" },
        { label: "Wet", value: "Wet" },
    ];
    public setActiveTab(event: string) {
        this.activeTab = event;
    }
}

export enum ModeledPerformanceDisplayTypeEnum {
    Total = "Total",
    Dry = "Dry",
    Wet = "Wet",
}
