import { Component, DestroyRef, ElementRef, HostListener, ViewChild, computed, effect, inject, input, signal } from "@angular/core";
import { Router } from "@angular/router";
import { DialogService } from "@ngneat/dialog";
import { Subscription } from "rxjs";
import { AuthenticationService } from "src/app/services/authentication.service";
import { BMP_SCOPE, OVTA_SCOPE, PROJECT_SCOPE, RecordSearchService, WQMP_SCOPE } from "src/app/services/record-search.service";
import { PersonDto } from "src/app/shared/generated/model/person-dto";
import { EntitySearchComponent, SearchEntity, SearchScope } from "src/app/shared/components/entity-search/entity-search.component";

const ACTIONS_SCOPE = "actions";

// The same marker PNGs MarkerHelper puts on the maps: orange for inventoried BMPs, violet for every other record type
const BMP_MARKER = "/assets/main/map-icons/marker-icon-orange.png";
const RECORD_MARKER = "/assets/main/map-icons/marker-icon-violet.png";

// Facet order is AC 9: All (implicit), BMPs, WQMPs, OVTAs, Projects. Actions are pinned — no facet, no count, and
// no marker: the badge is what sets them apart from records.
const SCOPES: SearchScope[] = [
    { id: ACTIONS_SCOPE, label: "Actions", pinned: true },
    { id: BMP_SCOPE, label: "BMPs", icon: BMP_MARKER },
    { id: WQMP_SCOPE, label: "WQMPs", icon: RECORD_MARKER },
    { id: OVTA_SCOPE, label: "OVTAs", icon: RECORD_MARKER },
    { id: PROJECT_SCOPE, label: "Projects", icon: RECORD_MARKER },
];

// A fixed list, the same for everyone who may start them — each lands in its existing flow, which keeps its own
// gate. Start Visit needs no "Continue Visit" twin: the visit modal offers continue-vs-new for the BMP picked.
const START_VISIT = "action-start-visit";
const START_OM_VERIFICATION = "action-start-om-verification";
const ADD_BMP = "action-add-bmp";
const DELINEATE = "action-delineate";
const ACTIONS: SearchEntity[] = [
    { id: START_VISIT, scope: ACTIONS_SCOPE, badge: "Action", title: "Start Visit", subtitle: "Record a field visit to a BMP" },
    { id: START_OM_VERIFICATION, scope: ACTIONS_SCOPE, badge: "Action", title: "Start O&M Verification", subtitle: "Verify operation and maintenance for a WQMP" },
    { id: ADD_BMP, scope: ACTIONS_SCOPE, badge: "Action", title: "Add a BMP", subtitle: "Add a treatment BMP to the inventory" },
    { id: DELINEATE, scope: ACTIONS_SCOPE, badge: "Action", title: "Delineate Drainage Areas", subtitle: "Draw or edit BMP drainage areas on the map" },
];

/**
 * NPT-1125: header record search. A field-styled trigger plus the shared entity-search overlay, fed a
 * client-side index of every BMP, WQMP, OVTA and Planning Project the user can see.
 */
@Component({
    selector: "record-search",
    templateUrl: "./record-search.component.html",
    styleUrls: ["./record-search.component.scss"],
    imports: [EntitySearchComponent],
})
export class RecordSearchComponent {
    public user = input.required<PersonDto>();

    @ViewChild("trigger", { static: true }) private trigger: ElementRef<HTMLButtonElement>;
    @ViewChild("search", { static: true }) private search: EntitySearchComponent;

    private recordSearchService = inject(RecordSearchService);
    private authenticationService = inject(AuthenticationService);
    private dialogService = inject(DialogService);
    private router = inject(Router);

    private records = signal<SearchEntity[]>([]);
    private recordsPersonID: number | null = null;
    private indexSubscription: Subscription | null = null;

    public readonly scopes = SCOPES;
    public readonly shortcutLabel = RecordSearchComponent.isMac() ? "⌘K" : "Ctrl K";

    // Every flow behind the actions needs edit rights in an assigned jurisdiction (or Admin)
    private canStartActions = computed(() => {
        this.user();
        return this.authenticationService.doesCurrentUserHaveJurisdictionEditPermission();
    });

    public entities = computed(() => [...(this.canStartActions() ? ACTIONS : []), ...this.records()]);

    constructor() {
        // Impersonation swaps the user without recreating the header, so the list here may still be the previous
        // person's. Drop it (and any open overlay) as soon as the person changes; the fetch itself waits for intent.
        effect(() => {
            const personID = this.user().PersonID;
            if (this.recordsPersonID === personID) {
                return;
            }
            this.recordsPersonID = personID;
            this.indexSubscription?.unsubscribe();
            this.records.set([]);
            this.search.close();
        });
        inject(DestroyRef).onDestroy(() => this.indexSubscription?.unsubscribe());
    }

    // Handled here rather than by the component's own hotkey: focusing the trigger first means the dialog
    // returns focus to the header field on close, however the overlay was opened.
    @HostListener("document:keydown", ["$event"])
    public onDocumentKeydown(event: KeyboardEvent): void {
        // key is undefined on the synthetic keydown Chrome fires for autofill, and this listens document-wide
        if ((event.metaKey || event.ctrlKey) && event.key?.toLowerCase() === "k") {
            // While another modal is up, opening search would return focus to the header behind its backdrop
            if (document.querySelector("ngneat-dialog")) {
                return;
            }
            event.preventDefault();
            if (this.search.open()) {
                this.search.close();
            } else {
                this.open();
            }
        }
    }

    public open(): void {
        this.trigger.nativeElement.focus();
        this.prefetch();
        this.search.show();
    }

    // The index is every record the user can see, so it is fetched on intent (hover, focus, open) rather than on
    // every page load; require-query means nothing lists before the first keystroke, so the fetch is never seen.
    // The service caches per person, which is what swaps the index across impersonation.
    public prefetch(): void {
        this.loadIndex(this.user().PersonID);
    }

    public onSelect(entity: SearchEntity): void {
        if (entity.scope !== ACTIONS_SCOPE) {
            this.router.navigateByUrl(entity.url);
            return;
        }
        // The overlay closes (and hands focus back to the trigger) right after it emits select, so open any
        // modal on the next task or the returning focus lands behind it.
        setTimeout(() => this.startAction(entity.id));
    }

    private loadIndex(personID: number): void {
        this.indexSubscription?.unsubscribe();
        this.indexSubscription = this.recordSearchService.index(personID).subscribe((entities) => this.records.set(entities));
    }

    private async startAction(actionID: string): Promise<void> {
        switch (actionID) {
            case START_VISIT: {
                const { BeginFieldVisitModalComponent } =
                    await import("src/app/pages/treatment-bmps/treatment-bmp-detail/begin-field-visit-modal/begin-field-visit-modal.component");
                // No treatmentBMPID: the modal's BMP picker, which checks for a visit in progress (NPT-1122)
                this.dialogService.open(BeginFieldVisitModalComponent, { data: {} }).afterClosed$.subscribe((fieldVisit) => {
                    if (fieldVisit) {
                        this.router.navigate(["/field-visits", fieldVisit.FieldVisitID]);
                    }
                });
                break;
            }
            case START_OM_VERIFICATION: {
                const { StartOMVisitModalComponent } = await import("src/app/pages/wqmps/wqmp-verifications/start-om-visit-modal/start-om-visit-modal.component");
                this.dialogService.open(StartOMVisitModalComponent).afterClosed$.subscribe((result) => {
                    if (result) {
                        this.router.navigate(["/water-quality-management-plans", result.waterQualityManagementPlanID, "verifications", "new", "basics"], {
                            queryParams: { verificationDate: result.verificationDate },
                        });
                    }
                });
                break;
            }
            case ADD_BMP:
                this.router.navigate(["/treatment-bmps", "new"]);
                break;
            case DELINEATE:
                this.router.navigate(["/delineation", "delineation-map"]);
                break;
        }
    }

    private static isMac(): boolean {
        const platform = (navigator as Navigator & { userAgentData?: { platform?: string } }).userAgentData?.platform ?? navigator.platform ?? "";
        return /mac|iphone|ipad/i.test(platform);
    }
}
