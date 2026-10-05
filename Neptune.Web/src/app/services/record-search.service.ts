import { Injectable } from "@angular/core";
import { Observable, catchError, map, of, shareReplay } from "rxjs";
import { SearchService } from "src/app/shared/generated/api/search.service";
import { SearchRecordDto } from "src/app/shared/generated/model/search-record-dto";
import { SearchEntity } from "src/app/shared/components/entity-search/entity-search.component";

// Scope ids match SearchRecordDto's constants on the API
export const BMP_SCOPE = "bmp";
export const WQMP_SCOPE = "wqmp";
export const OVTA_SCOPE = "ovta";
export const PROJECT_SCOPE = "project";

const DETAIL_ROUTES: Record<string, string> = {
    [BMP_SCOPE]: "/treatment-bmps",
    [WQMP_SCOPE]: "/water-quality-management-plans",
    // OVTA rows are Assessment Areas (their page lists the area's assessments)
    [OVTA_SCOPE]: "/trash/onland-visual-trash-assessment-areas",
    [PROJECT_SCOPE]: "/planning/projects",
};

// The index is refetched on open once it is this old, so records created mid-session become findable
const STALE_AFTER_MS = 5 * 60 * 1000;

/**
 * NPT-1125: caches the header record search index — every record the user can see, slimmed to what the
 * search overlay matches and shows. One copy per person, so impersonation swaps it out.
 */
@Injectable({ providedIn: "root" })
export class RecordSearchService {
    private personID: number | null = null;
    private loadedAt = 0;
    private index$: Observable<SearchEntity[]> | null = null;

    constructor(private searchService: SearchService) {}

    /** The cached index for this person, refetched when missing, for someone else, or stale. */
    public index(personID: number): Observable<SearchEntity[]> {
        const isStale = Date.now() - this.loadedAt > STALE_AFTER_MS;
        if (!this.index$ || this.personID !== personID || isStale) {
            this.personID = personID;
            this.loadedAt = Date.now();
            this.index$ = this.searchService.listIndexSearch().pipe(
                map((records) => records.map((record) => this.toEntity(record))),
                catchError(() => {
                    // Don't cache the failure; the next open retries
                    this.index$ = null;
                    return of([] as SearchEntity[]);
                }),
                shareReplay(1)
            );
        }
        return this.index$;
    }

    private toEntity(record: SearchRecordDto): SearchEntity {
        return {
            id: `${record.Scope}-${record.ID}`,
            scope: record.Scope,
            title: record.Title,
            subtitle: record.Subtitle ?? undefined,
            meta: record.Meta ?? undefined,
            url: `${DETAIL_ROUTES[record.Scope]}/${record.ID}`,
        };
    }
}
