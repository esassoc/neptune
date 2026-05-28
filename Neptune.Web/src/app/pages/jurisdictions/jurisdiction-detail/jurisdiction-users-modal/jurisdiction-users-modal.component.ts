import { AsyncPipe } from "@angular/common";
import { Component, inject, OnInit, signal } from "@angular/core";
import { DialogRef } from "@ngneat/dialog";
import { map, Observable } from "rxjs";
import { StormwaterJurisdictionService } from "src/app/shared/generated/api/stormwater-jurisdiction.service";
import { UserService } from "src/app/shared/generated/api/user.service";

export interface JurisdictionUsersModalContext {
    jurisdictionID: number;
    jurisdictionName: string;
    assignedPersonIDs: number[];
}

interface PersonOption {
    PersonID: number;
    Label: string;
    selected: boolean;
}

@Component({
    selector: "jurisdiction-users-modal",
    standalone: true,
    imports: [AsyncPipe],
    templateUrl: "./jurisdiction-users-modal.component.html",
})
export class JurisdictionUsersModalComponent implements OnInit {
    public ref: DialogRef<JurisdictionUsersModalContext, boolean> = inject(DialogRef);
    private userService = inject(UserService);
    private jurisdictionService = inject(StormwaterJurisdictionService);

    public options$: Observable<PersonOption[]>;
    public selectedIDs = new Set<number>();
    public isSaving = signal(false);
    public errorMessage = signal<string | null>(null);

    ngOnInit(): void {
        const assignedIDs = new Set(this.ref.data.assignedPersonIDs ?? []);
        this.selectedIDs = new Set(assignedIDs);

        this.options$ = this.userService.listUser().pipe(
            map((people) =>
                people
                    .map((p) => ({
                        PersonID: p.PersonID,
                        Label: `${p.FullName} (${p.OrganizationName})`,
                        selected: assignedIDs.has(p.PersonID),
                    }))
                    .sort((a, b) => a.Label.localeCompare(b.Label))
            )
        );
    }

    public toggle(option: PersonOption): void {
        option.selected = !option.selected;
        if (option.selected) {
            this.selectedIDs.add(option.PersonID);
        } else {
            this.selectedIDs.delete(option.PersonID);
        }
    }

    public save(): void {
        this.isSaving.set(true);
        this.errorMessage.set(null);
        this.jurisdictionService
            .updateUsersStormwaterJurisdiction(this.ref.data.jurisdictionID, { PersonIDs: Array.from(this.selectedIDs) })
            .subscribe({
                next: () => {
                    this.isSaving.set(false);
                    this.ref.close(true);
                },
                error: (err) => {
                    this.isSaving.set(false);
                    const message = typeof err?.error === "string" ? err.error : "Failed to update assigned users.";
                    this.errorMessage.set(message);
                },
            });
    }

    public cancel(): void {
        this.ref.close(false);
    }
}
