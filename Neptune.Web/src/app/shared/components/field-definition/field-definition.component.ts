import { Component, OnInit, Input, ChangeDetectorRef, ViewChild, AfterViewInit, OnDestroy } from "@angular/core";
import { Alert } from "../../models/alert";
import { AuthenticationService } from "src/app/services/authentication.service";
import { AlertService } from "../../services/alert.service";
import { EditorComponent, TINYMCE_SCRIPT_SRC } from "@tinymce/tinymce-angular";
import TinyMCEHelpers from "../../helpers/tiny-mce-helpers";
import { AlertContext } from "../../models/enums/alert-context.enum";
import { FieldDefinitionDto } from "src/app/shared/generated/model/field-definition-dto";
import { FieldDefinitionService } from "src/app/shared/generated/api/field-definition.service";
import { FieldDefinitionTypeEnum } from "src/app/shared/generated/enum/field-definition-type-enum";
import { PopperDirective } from "src/app/shared/directives/popper.directive";
import { FormsModule } from "@angular/forms";

@Component({
    selector: "field-definition",
    templateUrl: "./field-definition.component.html",
    styleUrls: ["./field-definition.component.scss"],
    imports: [EditorComponent, FormsModule, PopperDirective],
    providers: [{ provide: TINYMCE_SCRIPT_SRC, useValue: "tinymce/tinymce.min.js" }],
})
export class FieldDefinitionComponent implements OnInit, AfterViewInit, OnDestroy {
    @Input() fieldDefinitionType: string;
    @Input() labelOverride: string;
    @Input() inline: boolean = false;
    @Input() useBodyContainer: boolean = false;

    @ViewChild("tinyMceEditor") tinyMceEditor: EditorComponent;
    public tinyMceConfig: object;

    public fieldDefinition: FieldDefinitionDto;
    public isLoading: boolean = true;
    public isEditing: boolean = false;
    public emptyContent: boolean = false;

    public editedContent: string;

    constructor(
        private fieldDefinitionService: FieldDefinitionService,
        private authenticationService: AuthenticationService,
        private cdr: ChangeDetectorRef,
        private alertService: AlertService
    ) {}

    ngAfterViewInit(): void {
        // We need to use ngAfterViewInit because the image upload needs a reference to the component
        // to setup the blobCache for image base64 encoding
        this.tinyMceConfig = TinyMCEHelpers.DefaultInitConfig(this.tinyMceEditor);
    }

    ngOnInit() {
        this.fieldDefinitionService.getFieldDefinition(FieldDefinitionTypeEnum[this.fieldDefinitionType]).subscribe((x) => this.loadFieldDefinition(x));
    }

    ngOnDestroy() {
        this.cdr.detach();
    }

    private loadFieldDefinition(fieldDefinition: FieldDefinitionDto) {
        this.fieldDefinition = fieldDefinition;
        this.emptyContent = fieldDefinition.FieldDefinitionValue?.length == 0;
        this.isLoading = false;
        this.cdr.detectChanges();
    }

    public getLabelText() {
        if (this.labelOverride !== null && this.labelOverride !== undefined) {
            return this.labelOverride;
        }

        return this.fieldDefinition.FieldDefinitionType.FieldDefinitionTypeDisplayName;
    }

    public showEditButton(): boolean {
        return this.authenticationService.isCurrentUserAnAdministrator();
    }

    public enterEdit(event: any): void {
        event.preventDefault();

        this.editedContent = this.fieldDefinition.FieldDefinitionValue;
        this.isEditing = true;
    }

    public cancelEdit(): void {
        this.isEditing = false;
    }

    public saveEdit(): void {
        this.isEditing = false;
        this.isLoading = true;

        this.fieldDefinition.FieldDefinitionValue = this.editedContent;
        this.fieldDefinitionService.updateFieldDefinition(this.fieldDefinition.FieldDefinitionType.FieldDefinitionTypeID, this.fieldDefinition).subscribe(
            (x) => this.loadFieldDefinition(x),
            (error) => {
                this.isLoading = false;
                this.alertService.pushAlert(new Alert("There was an error updating the field definition", AlertContext.Danger));
            }
        );
    }
}
