import { AsyncPipe } from "@angular/common";
import { Component, inject, OnInit, signal } from "@angular/core";
import { FormsModule, ReactiveFormsModule } from "@angular/forms";
import { DialogRef } from "@ngneat/dialog";
import { map, Observable } from "rxjs";
import { StormwaterJurisdictionService } from "src/app/shared/generated/api/stormwater-jurisdiction.service";
import { UserService } from "src/app/shared/generated/api/user.service";
import { PersonDetailDto } from "src/app/shared/generated/model/person-detail-dto";

interface EditJurisdictionsModalContext {
    detail: PersonDetailDto;
}

interface JurisdictionOption {
    StormwaterJurisdictionID: number;
    StormwaterJurisdictionName: string;
    selected: boolean;
}

@Component({
    selector: "edit-jurisdictions-modal",
    standalone: true,
    imports: [AsyncPipe, FormsModule, ReactiveFormsModule],
    templateUrl: "./edit-jurisdictions-modal.component.html",
    styleUrl: "./edit-jurisdictions-modal.component.scss",
})
export class EditJurisdictionsModalComponent implements OnInit {
    public ref: DialogRef<EditJurisdictionsModalContext, boolean> = inject(DialogRef);
    private userService = inject(UserService);
    private stormwaterJurisdictionService = inject(StormwaterJurisdictionService);

    public options$: Observable<JurisdictionOption[]>;
    public selectedIDs = new Set<number>();
    public isSaving = signal(false);
    public errorMessage = signal<string | null>(null);

    ngOnInit(): void {
        const assignedIDs = new Set(this.ref.data.detail.AssignedStormwaterJurisdictions?.map((j) => j.StormwaterJurisdictionID) ?? []);
        this.selectedIDs = new Set(assignedIDs);

        this.options$ = this.stormwaterJurisdictionService.listStormwaterJurisdiction().pipe(
            map((js) =>
                js
                    .map((j) => ({
                        StormwaterJurisdictionID: j.StormwaterJurisdictionID,
                        StormwaterJurisdictionName: j.StormwaterJurisdictionName,
                        selected: assignedIDs.has(j.StormwaterJurisdictionID),
                    }))
                    .sort((a, b) => a.StormwaterJurisdictionName.localeCompare(b.StormwaterJurisdictionName))
            )
        );
    }

    public toggle(option: JurisdictionOption): void {
        option.selected = !option.selected;
        if (option.selected) {
            this.selectedIDs.add(option.StormwaterJurisdictionID);
        } else {
            this.selectedIDs.delete(option.StormwaterJurisdictionID);
        }
    }

    public save(): void {
        this.isSaving.set(true);
        this.errorMessage.set(null);
        this.userService
            .updateJurisdictionsUser(this.ref.data.detail.PersonID, { StormwaterJurisdictionIDs: Array.from(this.selectedIDs) })
            .subscribe({
                next: () => {
                    this.isSaving.set(false);
                    this.ref.close(true);
                },
                error: (err) => {
                    this.isSaving.set(false);
                    const message = typeof err?.error === "string" ? err.error : "Failed to update jurisdictions.";
                    this.errorMessage.set(message);
                },
            });
    }

    public cancel(): void {
        this.ref.close(false);
    }
}
