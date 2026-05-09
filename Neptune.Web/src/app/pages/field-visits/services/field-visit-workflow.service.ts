import { inject, Injectable } from "@angular/core";
import { Router } from "@angular/router";
import { BehaviorSubject, Observable, of, switchMap } from "rxjs";
import { tap } from "rxjs/operators";

import { FieldVisitService } from "src/app/shared/generated/api/field-visit.service";
import { FieldVisitWorkflowDto } from "src/app/shared/generated/model/field-visit-workflow-dto";
import { FieldVisitInventoryUpdatedDto } from "src/app/shared/generated/model/field-visit-inventory-updated-dto";
import { AlertService } from "src/app/shared/services/alert.service";

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

    /** Navigate to the Visit Summary for the current field visit. Used by the "Wrap Up Visit" buttons
     * on gateway/edit pages and by the "Save & Wrap Up Visit" save targets. */
    public wrapUpVisit(fieldVisitID: number): void {
        this.router.navigate(["/field-visits", fieldVisitID, "summary"]);
    }
}
