import { Component, inject, Input, OnInit } from "@angular/core";
import { AsyncPipe } from "@angular/common";
import { RouterLink } from "@angular/router";
import { catchError, EMPTY, Observable, shareReplay, tap } from "rxjs";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { LoadingDirective } from "src/app/shared/directives/loading.directive";
import { AlertService } from "src/app/shared/services/alert.service";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { AuthenticationService } from "src/app/services/authentication.service";
import { TreatmentBMPTypeService } from "src/app/shared/generated/api/treatment-bmp-type.service";
import { TreatmentBMPTypeDetailDto } from "src/app/shared/generated/model/treatment-bmp-type-detail-dto";
import { TreatmentBMPTypeObservationTypeDetailDto } from "src/app/shared/generated/model/treatment-bmp-type-observation-type-detail-dto";
import { TreatmentBMPTypeCustomAttributeTypeDetailDto } from "src/app/shared/generated/model/treatment-bmp-type-custom-attribute-type-detail-dto";
import { CustomAttributeTypePurposes } from "src/app/shared/generated/enum/custom-attribute-type-purpose-enum";

interface AttributesByPurpose {
    purposeID: number;
    purposeDisplayName: string;
    rows: TreatmentBMPTypeCustomAttributeTypeDetailDto[];
}

@Component({
    selector: "treatment-bmp-type-detail",
    standalone: true,
    imports: [AsyncPipe, RouterLink, PageHeaderComponent, LoadingDirective],
    templateUrl: "./treatment-bmp-type-detail.component.html",
    styleUrl: "./treatment-bmp-type-detail.component.scss",
})
export class TreatmentBmpTypeDetailComponent implements OnInit {
    @Input() treatmentBMPTypeID!: number;

    private bmpTypeService = inject(TreatmentBMPTypeService);
    private alertService = inject(AlertService);
    private authenticationService = inject(AuthenticationService);

    public bmpType$: Observable<TreatmentBMPTypeDetailDto>;
    public isLoading = true;

    public get isAdmin(): boolean {
        return this.authenticationService.isCurrentUserAnAdministrator();
    }

    ngOnInit(): void {
        this.bmpType$ = this.bmpTypeService.getDetailTreatmentBMPType(this.treatmentBMPTypeID).pipe(
            tap(() => (this.isLoading = false)),
            catchError(() => {
                this.isLoading = false;
                this.alertService.pushAlert(new Alert("Failed to load Treatment BMP Type.", AlertContext.Danger));
                return EMPTY;
            }),
            shareReplay(1),
        );
    }

    public weightDisplay(ot: TreatmentBMPTypeObservationTypeDetailDto): string {
        if (ot.AssessmentScoreWeight == null) return "pass/fail";
        return `${Number(ot.AssessmentScoreWeight)}%`;
    }

    public attributesByPurpose(bmpType: TreatmentBMPTypeDetailDto): AttributesByPurpose[] {
        // Match MVC's Detail.cshtml: render one card per CustomAttributeTypePurpose, in enum order,
        // even when a purpose has zero attributes (so readers can confirm "Modeling Attributes: none").
        return CustomAttributeTypePurposes.map((purpose) => ({
            purposeID: purpose.Value,
            purposeDisplayName: purpose.DisplayName,
            rows: (bmpType.CustomAttributeTypes ?? [])
                .filter((c) => c.CustomAttributeTypePurposeID === purpose.Value),
        }));
    }
}
