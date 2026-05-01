import { Component, inject, OnInit, signal } from "@angular/core";
import { AsyncPipe } from "@angular/common";
import { RouterLink } from "@angular/router";
import { Observable, shareReplay } from "rxjs";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { LoadingDirective } from "src/app/shared/directives/loading.directive";
import { AuthenticationService } from "src/app/services/authentication.service";
import { TreatmentBMPTypeService } from "src/app/shared/generated/api/treatment-bmp-type.service";
import { TreatmentBMPTypeDetailDto } from "src/app/shared/generated/model/treatment-bmp-type-detail-dto";
import { TreatmentBMPTypeObservationTypeDetailDto } from "src/app/shared/generated/model/treatment-bmp-type-observation-type-detail-dto";
import { NeptunePageTypeEnum } from "src/app/shared/generated/enum/neptune-page-type-enum";
import { CustomAttributeTypePurposes } from "src/app/shared/generated/enum/custom-attribute-type-purpose-enum";

interface PurposeCount {
    purposeDisplayName: string;
    count: number;
}

@Component({
    selector: "treatment-bmp-types",
    standalone: true,
    imports: [PageHeaderComponent, RouterLink, AsyncPipe, LoadingDirective],
    templateUrl: "./treatment-bmp-types.component.html",
    styleUrl: "./treatment-bmp-types.component.scss",
})
export class TreatmentBmpTypesComponent implements OnInit {
    private bmpTypeService = inject(TreatmentBMPTypeService);
    private authenticationService = inject(AuthenticationService);

    public bmpTypes$: Observable<TreatmentBMPTypeDetailDto[]>;
    public isLoading = signal(true);
    // The public BMP Types index in legacy MVC uses NeptunePageType.TreatmentBMPType (enum 7)
    // for its RTE — distinct from the admin Manage page enum. Matches existing seeded content.
    public NeptunePageTypeEnum = NeptunePageTypeEnum;

    public get isAuthenticated(): boolean {
        return this.authenticationService.isAuthenticated();
    }

    ngOnInit(): void {
        this.bmpTypes$ = this.bmpTypeService.listAsDetailDtoTreatmentBMPType().pipe(shareReplay(1));
        this.bmpTypes$.subscribe(() => this.isLoading.set(false));
    }

    public attributeCountsByPurpose(bmpType: TreatmentBMPTypeDetailDto): PurposeCount[] {
        // MVC pattern: render one bullet per Purpose, even when count is zero — gives readers a
        // consistent snapshot of which purposes are populated for each BMP Type.
        return CustomAttributeTypePurposes.map((purpose) => ({
            purposeDisplayName: purpose.DisplayName,
            count: (bmpType.CustomAttributeTypes ?? [])
                .filter((c) => c.CustomAttributeTypePurposeID === purpose.Value).length,
        }));
    }

    public weightDisplay(ot: TreatmentBMPTypeObservationTypeDetailDto): string {
        if (ot.AssessmentScoreWeight == null) return "pass/fail";
        return `${Number(ot.AssessmentScoreWeight)}%`;
    }
}
