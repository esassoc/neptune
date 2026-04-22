import { Component, computed, signal } from "@angular/core";
import { FormsModule } from "@angular/forms";
import { NgSelectModule } from "@ng-select/ng-select";
import { NeptunePageTypeEnum } from "src/app/shared/generated/enum/neptune-page-type-enum";
import { NeptuneAreas } from "src/app/shared/generated/enum/neptune-area-enum";
import { TrainingVideoDto } from "src/app/shared/generated/model/training-video-dto";
import { TrainingVideoService } from "src/app/shared/generated/api/training-video.service";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { SafeResourceUrlPipe } from "src/app/shared/pipes/safe-resource-url.pipe";
import { LoadingDirective } from "src/app/shared/directives/loading.directive";

@Component({
    selector: "training",
    templateUrl: "./training.component.html",
    styleUrls: ["./training.component.scss"],
    imports: [PageHeaderComponent, SafeResourceUrlPipe, LoadingDirective, FormsModule, NgSelectModule],
})
export class TrainingComponent {
    public customRichTextTypeID: number = NeptunePageTypeEnum.Training;

    public allVideos = signal<TrainingVideoDto[]>([]);
    public isLoading = signal(true);
    public selectedModuleID = signal(0);

    public filterOptions = computed(() => {
        const videos = this.allVideos();
        const areaIDs = new Set(videos.map((v) => v.NeptuneAreaID).filter((id) => id != null));
        const moduleOptions = NeptuneAreas
            .filter((area) => areaIDs.has(area.Value))
            .sort((a, b) => a.DisplayName.localeCompare(b.DisplayName))
            .map((area) => ({ Label: area.DisplayName, Value: area.Value }));
        return [{ Label: "All Modules", Value: 0 }, ...moduleOptions];
    });

    public filteredVideos = computed(() => {
        const moduleID = this.selectedModuleID();
        const videos = this.allVideos();
        return moduleID === 0 ? videos : videos.filter((v) => v.NeptuneAreaID === moduleID);
    });

    constructor(private trainingVideoService: TrainingVideoService) {}

    ngOnInit() {
        this.trainingVideoService.listTrainingVideo().subscribe((videos) => {
            this.allVideos.set(videos);
            this.isLoading.set(false);
        });
    }

    onModuleFilterChange(value: number) {
        this.selectedModuleID.set(value);
    }
}
