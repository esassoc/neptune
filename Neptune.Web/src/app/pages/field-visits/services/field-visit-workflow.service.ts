import { Injectable } from "@angular/core";
import { BehaviorSubject, Observable, of, switchMap } from "rxjs";
import { tap } from "rxjs/operators";

import { FieldVisitService } from "src/app/shared/generated/api/field-visit.service";
import { FieldVisitWorkflowDto } from "src/app/shared/generated/model/field-visit-workflow-dto";
import { FieldVisitInventoryUpdatedDto } from "src/app/shared/generated/model/field-visit-inventory-updated-dto";

@Injectable({ providedIn: "root" })
export class FieldVisitWorkflowService {
    private workflowSubject = new BehaviorSubject<FieldVisitWorkflowDto | null>(null);
    public workflow$: Observable<FieldVisitWorkflowDto | null> = this.workflowSubject.asObservable();

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
     */
    public markInventoryUpdatedAndRefresh(fieldVisitID: number): Observable<FieldVisitWorkflowDto | null> {
        const dto = new FieldVisitInventoryUpdatedDto({ InventoryUpdated: true });
        return this.fieldVisitService.updateInventoryUpdatedFieldVisit(fieldVisitID, dto).pipe(switchMap(() => this.refresh()));
    }
}
