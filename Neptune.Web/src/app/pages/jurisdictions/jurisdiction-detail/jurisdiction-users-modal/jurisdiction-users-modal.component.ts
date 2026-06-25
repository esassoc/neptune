import { Component, computed, inject, OnInit, signal } from "@angular/core";
import { FormsModule } from "@angular/forms";
import { DialogRef } from "@ngneat/dialog";
import { NgSelectModule } from "@ng-select/ng-select";
import { StormwaterJurisdictionService } from "src/app/shared/generated/api/stormwater-jurisdiction.service";
import { UserService } from "src/app/shared/generated/api/user.service";
import { PersonSimpleDto } from "src/app/shared/generated/model/person-simple-dto";
import { RoleEnum } from "src/app/shared/generated/enum/role-enum";

export interface JurisdictionUsersModalContext {
    jurisdictionID: number;
    jurisdictionName: string;
    assignedPersonIDs: number[];
}

@Component({
    selector: "jurisdiction-users-modal",
    standalone: true,
    imports: [FormsModule, NgSelectModule],
    templateUrl: "./jurisdiction-users-modal.component.html",
    styleUrl: "./jurisdiction-users-modal.component.scss",
})
export class JurisdictionUsersModalComponent implements OnInit {
    public ref: DialogRef<JurisdictionUsersModalContext, boolean> = inject(DialogRef);
    private userService = inject(UserService);
    private jurisdictionService = inject(StormwaterJurisdictionService);

    // NPT-1061-2 (KE 5/27): the old checkbox-of-everyone UX was unusable at scale. New shape:
    // searchable picker → Add → list-with-remove. Admin and SitkaAdmin users are filtered out
    // because they implicitly view/manage every jurisdiction and shouldn't be assigned per-row.
    private allPeople = signal<PersonSimpleDto[]>([]);
    public assignedPersonIDs = signal<Set<number>>(new Set());
    public pickerPersonID: number | null = null;
    public isSaving = signal(false);
    public errorMessage = signal<string | null>(null);

    // People not yet assigned — what the dropdown offers.
    public availablePeople = computed(() => {
        const assigned = this.assignedPersonIDs();
        return this.allPeople()
            .filter((p) => !assigned.has(p.PersonID))
            .sort((a, b) => (a.FullName ?? "").localeCompare(b.FullName ?? ""));
    });

    // People currently assigned — rendered as the editable list below the picker.
    public assignedPeople = computed(() => {
        const assigned = this.assignedPersonIDs();
        return this.allPeople()
            .filter((p) => assigned.has(p.PersonID))
            .sort((a, b) => (a.FullName ?? "").localeCompare(b.FullName ?? ""));
    });

    ngOnInit(): void {
        this.assignedPersonIDs.set(new Set(this.ref.data.assignedPersonIDs ?? []));
        this.userService.listUser().subscribe((people) => {
            // Admins and SitkaAdmins are filtered out per KE — they have implicit access already.
            // Pre-existing assignments to those roles (legacy data) stay in `assignedPersonIDs`
            // but won't render because they aren't in `allPeople`; save still preserves them.
            const filtered = people.filter((p) => p.RoleID !== RoleEnum.Admin && p.RoleID !== RoleEnum.SitkaAdmin);
            this.allPeople.set(filtered);
        });
    }

    public addPerson(): void {
        if (this.pickerPersonID == null) return;
        const next = new Set(this.assignedPersonIDs());
        next.add(this.pickerPersonID);
        this.assignedPersonIDs.set(next);
        this.pickerPersonID = null;
    }

    public removePerson(personID: number): void {
        const next = new Set(this.assignedPersonIDs());
        next.delete(personID);
        this.assignedPersonIDs.set(next);
    }

    public save(): void {
        this.isSaving.set(true);
        this.errorMessage.set(null);
        this.jurisdictionService
            .updateUsersStormwaterJurisdiction(this.ref.data.jurisdictionID, { PersonIDs: Array.from(this.assignedPersonIDs()) })
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
