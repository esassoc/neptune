import { inject, Injectable } from "@angular/core";
import { Router } from "@angular/router";
import { BehaviorSubject, Observable, of, switchMap } from "rxjs";
import { tap } from "rxjs/operators";

import { FieldVisitService } from "src/app/shared/generated/api/field-visit.service";
import { FieldVisitWorkflowDto } from "src/app/shared/generated/model/field-visit-workflow-dto";
import { FieldVisitInventoryUpdatedDto } from "src/app/shared/generated/model/field-visit-inventory-updated-dto";
import { AlertService } from "src/app/shared/services/alert.service";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";

@Injectable({ providedIn: "root" })
export class FieldVisitWorkflowService {
    private workflowSubject = new BehaviorSubject<FieldVisitWorkflowDto | null>(null);
    public workflow$: Observable<FieldVisitWorkflowDto | null> = this.workflowSubject.asObservable();

    private router = inject(Router);
    private alertService = inject(AlertService);

    constructor(private fieldVisitService: FieldVisitService) {}

    public load(fieldVisitID: number): Observable<FieldVisitWorkflowDto> {
        return this.fieldVisitService.getByIDFieldVisit(fieldVisitID).pipe(tap((dto) => this.workflowSubject.next(dto)));
    }

    public refresh(): Observable<FieldVisitWorkflowDto | null> {
        const current = this.workflowSubject.value;
        if (!current) {
            return of(null);
        }
        return this.load(current.FieldVisitID);
    }

    public clear(): void {
        this.workflowSubject.next(null);
    }

    public getCurrent(): FieldVisitWorkflowDto | null {
        return this.workflowSubject.value;
    }

    /**
     * Flip InventoryUpdated=true on the field visit (server-side) and refresh the local workflow signal.
     * Used by every Inventory sub-step (Location / Photos / Attributes) on save so the Inventory parent
     * nav-item gets its green check, matching legacy MVC behavior where any sub-step submit set the flag.
     *
     * Short-circuits when the flag is already true — avoids redundant round-trips on every photo
     * upload/delete during a single visit (the photos sub-step calls this on each persistence event).
     */
    public markInventoryUpdatedAndRefresh(fieldVisitID: number): Observable<FieldVisitWorkflowDto | null> {
        const current = this.workflowSubject.value;
        if (current?.InventoryUpdated) {
            return of(current);
        }
        const dto = new FieldVisitInventoryUpdatedDto({ InventoryUpdated: true });
        return this.fieldVisitService.updateInventoryUpdatedFieldVisit(fieldVisitID, dto).pipe(switchMap(() => this.refresh()));
    }

    /** Clear any lingering AlertService alerts. Call from each workflow step's ngOnInit and at the start
     * of each save handler so success/error toasts from the prior step don't carry over. */
    public clearStepAlerts(): void {
        this.alertService.clearAlerts();
    }

    /**
     * Wrap up (finalize) the field visit: flip its status to Complete via the API, surface a
     * success alert, and navigate to the read-only detail page. Called by the "Save & Wrap Up
     * Visit" buttons on every step (which save first via their own form), by the standalone
     * "Wrap Up Visit" buttons on gateway pages (inventory/inventory-photos/assessment/maintenance),
     * and by the workflow-outlet's sidebar Wrap Up button (which wraps this in a confirm dialog).
     *
     * NPT-984: previously this method only navigated to /summary — it never called finalize, so
     * users hitting "Save & Wrap Up Visit" landed on Summary with the visit still InProgress and
     * concluded that wrap-up was broken. Only the sidebar button (which had its own inline
     * finalize call in the workflow-outlet) actually wrapped the visit. Centralizing the
     * finalize call here makes every wrap-up entry point work the same way.
     */
    public wrapUpVisit(fieldVisitID: number): void {
        this.fieldVisitService.finalizeFieldVisit(fieldVisitID).subscribe(() => {
            this.alertService.pushAlert(new Alert("Field Visit marked Complete.", AlertContext.Success));
            this.router.navigate(["/field-visits", fieldVisitID, "view"]);
        });
    }

    /** True when the field visit is no longer editable — anything past the InProgress (1) status.
     * Step components branch on this to render read-only views (no inputs, no Save buttons) so the
     * "View" action from /field-records and Visit Summary cards lands on a sensible page instead of
     * leaving the user in an edit form. */
    public isReadOnly(workflow: FieldVisitWorkflowDto | null): boolean {
        if (!workflow) return false;
        return workflow.FieldVisitStatusID !== 1;
    }
}
