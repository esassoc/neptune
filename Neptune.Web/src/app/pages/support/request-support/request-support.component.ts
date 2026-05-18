import { Component, inject, OnInit, signal } from "@angular/core";
import { FormControl, FormGroup, FormsModule, ReactiveFormsModule, Validators } from "@angular/forms";
import { RouterLink } from "@angular/router";
import { firstValueFrom } from "rxjs";
import { AuthenticationService } from "src/app/services/authentication.service";
import { SupportRequestService } from "src/app/shared/generated/api/support-request.service";
import { SupportRequestTypesAsSelectDropdownOptions } from "src/app/shared/generated/enum/support-request-type-enum";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { FormFieldComponent, FormFieldType } from "src/app/shared/components/forms/form-field/form-field.component";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { AlertService } from "src/app/shared/services/alert.service";
import { escapeHtml } from "src/app/shared/helpers/html-escape";

declare const grecaptcha: { ready: (cb: () => void) => void; execute: (siteKey: string, opts: { action: string }) => Promise<string> };

@Component({
    selector: "request-support",
    standalone: true,
    imports: [FormsModule, ReactiveFormsModule, RouterLink, PageHeaderComponent, AlertDisplayComponent, FormFieldComponent],
    templateUrl: "./request-support.component.html",
    styleUrl: "./request-support.component.scss",
})
export class RequestSupportComponent implements OnInit {
    private alertService = inject(AlertService);
    private supportRequestService = inject(SupportRequestService);
    private authenticationService = inject(AuthenticationService);

    public FormFieldType = FormFieldType;
    public supportRequestTypeOptions = SupportRequestTypesAsSelectDropdownOptions;

    public formGroup = new FormGroup({
        Name: new FormControl<string>("", { nonNullable: true, validators: [Validators.required, Validators.maxLength(200)] }),
        Email: new FormControl<string>("", { nonNullable: true, validators: [Validators.required, Validators.email, Validators.maxLength(256)] }),
        Organization: new FormControl<string>("", { nonNullable: true, validators: [Validators.maxLength(500)] }),
        Phone: new FormControl<string>("", { nonNullable: true, validators: [Validators.maxLength(50)] }),
        SupportRequestTypeID: new FormControl<number | null>(null, { validators: [Validators.required] }),
        Description: new FormControl<string>("", { nonNullable: true, validators: [Validators.required, Validators.maxLength(2000)] }),
    });

    public isSubmitting = signal(false);
    public successMessage = signal<string | null>(null);
    public isAuthenticated = signal(false);
    public siteKey = signal<string | null>(null);

    async ngOnInit(): Promise<void> {
        // Pre-populate name/email when the caller is authenticated; anonymous users see empty fields
        // and get a reCAPTCHA challenge on submit.
        this.isAuthenticated.set(this.authenticationService.isAuthenticated());
        if (this.isAuthenticated()) {
            const currentUser = await firstValueFrom(this.authenticationService.getCurrentUser());
            if (currentUser) {
                this.formGroup.patchValue({
                    Name: `${currentUser.FirstName ?? ""} ${currentUser.LastName ?? ""}`.trim(),
                    Email: currentUser.Email ?? "",
                    Organization: currentUser.OrganizationName ?? "",
                    Phone: currentUser.Phone ?? "",
                });
            }
        } else {
            // Fetch the public site key + load the reCAPTCHA v3 script for invisible challenge on submit.
            try {
                const siteKey = await firstValueFrom(this.supportRequestService.getRecaptchaSiteKeySupportRequest());
                if (typeof siteKey === "string" && siteKey.length > 0) {
                    this.siteKey.set(siteKey);
                    this.loadRecaptchaScript(siteKey);
                }
            } catch {
                // If the site-key endpoint fails, still let the user submit; the API will surface a friendly error.
            }
        }
    }

    private loadRecaptchaScript(siteKey: string): void {
        if (document.getElementById("recaptcha-v3-script")) return;
        const script = document.createElement("script");
        script.id = "recaptcha-v3-script";
        script.src = `https://www.google.com/recaptcha/api.js?render=${encodeURIComponent(siteKey)}`;
        script.async = true;
        document.head.appendChild(script);
    }

    public async submit(): Promise<void> {
        if (this.formGroup.invalid) return;
        this.isSubmitting.set(true);
        this.successMessage.set(null);

        let recaptchaToken = "";
        if (!this.isAuthenticated() && this.siteKey()) {
            try {
                recaptchaToken = await new Promise<string>((resolve, reject) => {
                    if (typeof grecaptcha === "undefined" || !grecaptcha.ready) {
                        reject(new Error("reCAPTCHA failed to load. Please refresh the page and try again."));
                        return;
                    }
                    grecaptcha.ready(() => {
                        grecaptcha.execute(this.siteKey()!, { action: "support" }).then(resolve).catch(reject);
                    });
                });
            } catch (err: any) {
                this.isSubmitting.set(false);
                this.alertService.pushAlert(new Alert(escapeHtml(err?.message ?? "reCAPTCHA failed."), AlertContext.Danger, true));
                return;
            }
        }

        const dto = {
            Name: this.formGroup.value.Name!,
            Email: this.formGroup.value.Email!,
            Organization: this.formGroup.value.Organization || "",
            Phone: this.formGroup.value.Phone || "",
            SupportRequestTypeID: this.formGroup.value.SupportRequestTypeID!,
            Description: this.formGroup.value.Description!,
            CurrentPageUrl: window.location.href,
            RecaptchaToken: recaptchaToken,
        };

        this.supportRequestService.submitSupportRequest(dto).subscribe({
            next: (result) => {
                this.isSubmitting.set(false);
                if (result.Success) {
                    this.successMessage.set(result.Message ?? "Support request sent.");
                    this.formGroup.reset();
                } else {
                    this.alertService.pushAlert(new Alert(escapeHtml(result.Message ?? "Failed to send support request."), AlertContext.Danger, true));
                }
            },
            error: (err) => {
                this.isSubmitting.set(false);
                const message = typeof err?.error?.Message === "string" ? err.error.Message : "Failed to send support request.";
                this.alertService.pushAlert(new Alert(escapeHtml(message), AlertContext.Danger, true));
            },
        });
    }
}
