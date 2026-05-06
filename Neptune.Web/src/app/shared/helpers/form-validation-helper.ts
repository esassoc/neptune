import { FormGroup } from "@angular/forms";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { AlertService } from "src/app/shared/services/alert.service";

const VALIDATION_ALERT_CODE = "FormValidationError";

export function validateFormOrAlert(form: FormGroup, alertService: AlertService, message?: string): boolean {
    if (form.valid) {
        alertService.removeAlertByUniqueCode(VALIDATION_ALERT_CODE);
        return true;
    }
    form.markAllAsTouched();
    alertService.pushAlert(
        new Alert(message ?? "Please complete the required fields highlighted below before saving.", AlertContext.Danger, true, VALIDATION_ALERT_CODE)
    );
    return false;
}
