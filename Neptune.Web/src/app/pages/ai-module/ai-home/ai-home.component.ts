import { Component, inject, OnInit, OnDestroy } from "@angular/core";
import { HttpErrorResponse } from "@angular/common/http";
import { AlertService } from "src/app/shared/services/alert.service";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { filter, map, Observable, of, switchMap, tap, catchError, shareReplay, BehaviorSubject, interval, takeUntil, Subject, share } from "rxjs";
import { CommonModule } from "@angular/common";
import { ReactiveFormsModule, FormControl } from "@angular/forms";
import { WaterQualityManagementPlanService } from "src/app/shared/generated/api/water-quality-management-plan.service";
import { WaterQualityManagementPlanKeyDocumentsComponent } from "src/app/pages/ai-module/ai-home/water-quality-management-plan-documents/water-quality-management-plan-documents.component";
import { FormFieldComponent, FormFieldType, SelectDropdownOption } from "src/app/shared/components/forms/form-field/form-field.component";
import { WaterQualityManagementPlanDocumentDto, WaterQualityManagementPlanDocumentExtractionResultDto, WaterQualityManagementPlanDto } from "src/app/shared/generated/model/models";
import { RouterModule } from "@angular/router";
import { WfsService } from "src/app/shared/services/wfs.service";
import { IconComponent } from "src/app/shared/components/icon/icon.component";
import { AIService } from "src/app/shared/generated/api/ai.service";
import { WaterQualityManagementPlanChatbotComponent } from "./water-quality-management-plan-chatbot/water-quality-management-plan-chatbot.component";
import { LoadingDirective } from "src/app/shared/directives/loading.directive";
import { environment } from "src/environments/environment";
import { ModalService, ModalSizeEnum } from "src/app/shared/services/modal/modal.service";
import { FieldSourceModalComponent } from "./field-source-modal/field-source-modal.component";

@Component({
    selector: "ai-home",
    templateUrl: "./ai-home.component.html",
    styleUrls: ["./ai-home.component.scss"],
    standalone: true,
    imports: [
        CommonModule,
        ReactiveFormsModule,
        WaterQualityManagementPlanKeyDocumentsComponent,
        FormFieldComponent,
        RouterModule,
        IconComponent,
        WaterQualityManagementPlanChatbotComponent,
        LoadingDirective,
    ],
})
export class AiHomeComponent implements OnInit, OnDestroy {
    public waterQualityManagementPlans$: Observable<SelectDropdownOption[]>;
    public waterQualityManagementPlans: WaterQualityManagementPlanDto[];
    public selectedPlan$: Observable<WaterQualityManagementPlanDto | null>;

    public selectedDocument: WaterQualityManagementPlanDocumentDto = null;
    public activeChatbotDocument: WaterQualityManagementPlanDocumentDto = null;

    public extractionResult$: Observable<WaterQualityManagementPlanDocumentExtractionResultDto>;
    public finalOutputObject: any = null;

    public imagePreview$: Observable<string>;

    public planControl = new FormControl<number | null>(null);
    public FormFieldType = FormFieldType;

    public isExtracting: boolean = false;
    
    private currentExtractingTextIndexSubject = new BehaviorSubject<number>(0);
    public currentExtractingTextIndex$ = this.currentExtractingTextIndexSubject.asObservable();
    public currentExtractingTextIndex: number = 0;
    private destroy$ = new Subject<void>();
    
    public extractingTexts: string[] = [
        "Extracting water quality plan data",
        "Parsing report structure and contents",
        "Analyzing document text for key water quality details",
        "Identifying relevant sections and data fields",
        "Processing water management plan tables and figures",
        "Mapping extracted data to standardized schema",
        "Performing quality checks on extracted values",
        "Generating structured summary for review",
        "Scanning your PDF for water quality parameters and management data",
        "Looking for treatment details, monitoring plans, and compliance info",
        "Reviewing extracted sections for accuracy",
        "Sifting through sediment... I mean, text",
        "Filtering the noise to find clean data",
        "Analyzing flow... of text and tables",
        "Your plan is under the microscope — extracting key water data",
        "Performing a final consistency check before returning results",
    ];

    public isChatbotOpen: boolean = false;

    private alertService = inject(AlertService);

    constructor(
        private waterQualityManagementPlanService: WaterQualityManagementPlanService,
        private aiService: AIService,
        private wfsService: WfsService,
        private modalService: ModalService
    ) {}

    ngOnInit(): void {
        this.waterQualityManagementPlans$ = this.waterQualityManagementPlanService.listWithFinalWQMPDocumentWaterQualityManagementPlan().pipe(
            tap((plans) => {
                this.waterQualityManagementPlans = plans;
            }),
            map((list) => {
                let options = list.map((x) => ({ Value: x.WaterQualityManagementPlanID, Label: x.WaterQualityManagementPlanName }) as SelectDropdownOption);
                return options;
            })
        );

        this.selectedPlan$ = this.planControl.valueChanges.pipe(
            tap(() => {
                this.isExtracting = false;
                this.selectedDocument = null;
                this.activeChatbotDocument = null;
            }),
            map((value) => {
                return this.waterQualityManagementPlans.find((plan) => plan.WaterQualityManagementPlanID === value) || null;
            }),
            shareReplay(1)
        );

        this.imagePreview$ = this.selectedPlan$.pipe(
            filter((plan) => !!plan),
            switchMap((plan) => {
                return this.wfsService.getWQMPPreviewImage(plan).pipe(
                    catchError((err) => {
                        console.error("Error loading preview image", err);
                        return of(null);
                    })
                );
            })
        );
    }

    ngOnDestroy(): void {
        this.destroy$.next();
        this.destroy$.complete();
    }

    onDocumentSelectedChange(document: WaterQualityManagementPlanDocumentDto) {
        this.selectedDocument = document;
        this.activeChatbotDocument = null;
        this.isChatbotOpen = false;
    }

    onClickExtractData() {
        this.activeChatbotDocument = this.selectedDocument;
        this.isExtracting = true;

        this.currentExtractingTextIndex = 0;
        this.currentExtractingTextIndexSubject.next(0);
        
        const textUpdateSubscription = interval(7500).pipe(
            takeUntil(this.destroy$)
        ).subscribe(() => {
            this.currentExtractingTextIndex++;
            if (this.currentExtractingTextIndex >= this.extractingTexts.length) {
                this.currentExtractingTextIndex = this.extractingTexts.length - 1;
            }
            this.currentExtractingTextIndexSubject.next(this.currentExtractingTextIndex);
        });

        this.extractionResult$ = this.aiService.extractAllAI(this.selectedDocument.WaterQualityManagementPlanDocumentID).pipe(
            share(),
            tap(result => {
                this.finalOutputObject = JSON.parse(result.FinalOutput);

                this.isExtracting = false;
            }),
            catchError((error: HttpErrorResponse) => {
                this.isExtracting = false;
                // NPT-1051: don't swallow extraction failures — surface them so the user knows
                // the run errored instead of seeing the spinner stop with no feedback. Append
                // a hint when Anthropic returns the generic "Could not process PDF" message.
                const rawMsg = error?.error?.message ?? "Extraction failed. Please try again or contact support.";
                const baseMsg = rawMsg.trim().replace(/\.?$/, ".");
                const msg = /could not process pdf/i.test(rawMsg)
                    ? `${baseMsg} Common reasons: more than 100 pages, over 200 MB, or password-protected.`
                    : baseMsg;
                this.alertService.pushAlert(new Alert(msg, AlertContext.Danger));
                return of(null);
            })
        );
    }

    onExtractingChange(isExtracting: boolean) {
        this.isExtracting = isExtracting;
        if (isExtracting) {
            this.planControl.disable();
        } else {
            this.planControl.enable();
        }
    }

    toggleChatbot() {
        if (this.activeChatbotDocument) {
            this.isChatbotOpen = !this.isChatbotOpen;
        }
    }

    onViewSource(fieldLabel: string, field: any) {
        if (!field) return;
        this.modalService.open(FieldSourceModalComponent, null, { ModalSize: ModalSizeEnum.Medium, CloseOnClickOut: true }, { FieldLabel: fieldLabel, Field: field });
    }

    ocStormwaterToolsMainUrl(): string {
        return environment.ocStormwaterToolsBaseUrl;
    }
}
