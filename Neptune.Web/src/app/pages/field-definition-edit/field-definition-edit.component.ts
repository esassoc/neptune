import { Component, OnInit, ChangeDetectorRef, ViewChild } from "@angular/core";
import { AuthenticationService } from "src/app/services/authentication.service";
import { Router, ActivatedRoute, RouterLink } from "@angular/router";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { AlertService } from "src/app/shared/services/alert.service";
import { FieldDefinitionDto, PersonDto } from "src/app/shared/generated/model/models";
import { FieldDefinitionService } from "src/app/shared/generated/api/field-definition.service";
import { EditorComponent, EditorModule, TINYMCE_SCRIPT_SRC } from "@tinymce/tinymce-angular";
import TinyMCEHelpers from "src/app/shared/helpers/tiny-mce-helpers";
import { FormsModule } from "@angular/forms";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";

import { PageHeaderComponent } from "../../shared/components/page-header/page-header.component";

@Component({
    selector: "field-definition-edit",
    templateUrl: "./field-definition-edit.component.html",
    styleUrls: ["./field-definition-edit.component.scss"],
    imports: [RouterLink, AlertDisplayComponent, EditorModule, FormsModule, PageHeaderComponent],
    providers: [{ provide: TINYMCE_SCRIPT_SRC, useValue: "tinymce/tinymce.min.js" }],
})
export class FieldDefinitionEditComponent implements OnInit {
    private currentUser: PersonDto;

    public fieldDefinition: FieldDefinitionDto;
    public loadFailed = false;
    public tinyMceConfig: object;

    // The <editor> sits inside an `@if (fieldDefinition)` so the ViewChild ref doesn't exist at
    // ngAfterViewInit time. Use the setter form so we can wire the image-upload-aware config the
    // moment the editor is queried in, after the data load resolves and the @if hydrates.
    private _tinyMceEditor: EditorComponent;
    @ViewChild("tinyMceEditor") set tinyMceEditor(ref: EditorComponent) {
        if (ref && !this._tinyMceEditor) {
            this._tinyMceEditor = ref;
            this.tinyMceConfig = TinyMCEHelpers.DefaultInitConfig(ref);
        }
    }

    public isLoadingSubmit: boolean;

    constructor(
        private route: ActivatedRoute,
        private router: Router,
        private alertService: AlertService,
        private fieldDefinitionService: FieldDefinitionService,
        private authenticationService: AuthenticationService,
        private cdr: ChangeDetectorRef
    ) {}

    ngOnInit() {
        this.authenticationService.getCurrentUser().subscribe((currentUser) => {
            this.currentUser = currentUser;
            // NPT-999: read `fieldDefinitionTypeID` (canonical AC param). Legacy `definitionID`
            // route still resolves via a redirect in app.routes.ts that preserves the value, so
            // either param name reaching this component is treated the same.
            const raw = this.route.snapshot.paramMap.get("fieldDefinitionTypeID")
                ?? this.route.snapshot.paramMap.get("definitionID");
            const id = raw ? parseInt(raw, 10) : NaN;
            if (Number.isFinite(id)) {
                this.fieldDefinitionService.getFieldDefinition(id).subscribe({
                    next: (fieldDefinition) => {
                        if (fieldDefinition) {
                            this.fieldDefinition = fieldDefinition;
                        } else {
                            this.loadFailed = true;
                        }
                    },
                    error: () => {
                        this.loadFailed = true;
                    },
                });
            } else {
                this.loadFailed = true;
            }
        });
    }

    ngOnDestroy() {
        this.cdr.detach();
    }

    public currentUserIsAdmin(): boolean {
        return this.authenticationService.isUserAnAdministrator(this.currentUser);
    }

    saveDefinition(): void {
        this.isLoadingSubmit = true;

        this.fieldDefinitionService.updateFieldDefinition(this.fieldDefinition.FieldDefinitionType.FieldDefinitionTypeID, this.fieldDefinition).subscribe({
            next: () => {
                this.isLoadingSubmit = false;
                this.router.navigateByUrl("/labels-and-definitions").then(() => {
                    this.alertService.pushAlert(
                        new Alert(`The definition for ${this.fieldDefinition.FieldDefinitionType.FieldDefinitionTypeDisplayName} was successfully updated.`, AlertContext.Success)
                    );
                });
            },
            error: () => {
                this.isLoadingSubmit = false;
                this.alertService.pushAlert(
                    new Alert(
                        `Failed to save the definition for ${this.fieldDefinition.FieldDefinitionType.FieldDefinitionTypeDisplayName}. Please try again.`,
                        AlertContext.Danger,
                        true
                    )
                );
                this.cdr.detectChanges();
            },
        });
    }
}
