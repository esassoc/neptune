import { Component, OnInit } from "@angular/core";
import { NeptunePageTypeEnum } from "src/app/shared/generated/enum/neptune-page-type-enum";
import { TrainingVideoDto } from "src/app/shared/generated/model/training-video-dto";
import { TrainingVideoService } from "src/app/shared/generated/api/training-video.service";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { SafeResourceUrlPipe } from "src/app/shared/pipes/safe-resource-url.pipe";
import { LoadingDirective } from "src/app/shared/directives/loading.directive";

@Component({
    selector: "training",
    templateUrl: "./training.component.html",
    styleUrls: ["./training.component.scss"],
    imports: [PageHeaderComponent, SafeResourceUrlPipe, LoadingDirective],
})
export class TrainingComponent implements OnInit {
    public customRichTextTypeID: number = NeptunePageTypeEnum.Training;
    public videos: TrainingVideoDto[] = [];
    public isLoading: boolean = true;

    constructor(private trainingVideoService: TrainingVideoService) {}

    ngOnInit() {
        this.trainingVideoService.listTrainingVideo().subscribe((videos) => {
            this.videos = videos;
            this.isLoading = false;
        });
    }
}
