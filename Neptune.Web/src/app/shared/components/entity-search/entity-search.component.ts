import { ChangeDetectionStrategy, Component, ElementRef, booleanAttribute, computed, effect, input, model, numberAttribute, output, signal, viewChild } from "@angular/core";

/** A facet. Results group under these, and the scope strip filters to one. */
export interface SearchScope {
    id: string;
    label: string;
    /** Image URL for the facet and its rows' marker, e.g. a map-icons PNG. */
    icon?: string;
    /**
     * A PINNED scope is not a facet: no radio in the scope strip and no share of the counts. Its entities render as
     * the first group under "All" — every one of them on an empty query, the matching ones otherwise — and never
     * under a facet. For a short fixed list that sits alongside the records, e.g. the actions a user can start.
     */
    pinned?: boolean;
}

/** A searchable record. Falls back to its scope's icon when `icon` is omitted. */
export interface SearchEntity {
    id: string;
    /** The record's name. */
    title: string;
    /** The record's distinguishing attribute — type or status. Matched by the query. */
    subtitle?: string;
    /** id of the SearchScope this entity belongs to. */
    scope: string;
    /** Image URL. */
    icon?: string;
    /** Where selecting it should go. The CONSUMER navigates; this component does not. */
    url?: string;
    /** The trailing datum that pins which one this is (jurisdiction, date). Not matched. */
    meta?: string;
    /** A quiet label after the title, for a row that is a different KIND of thing from its neighbours ("Action"). */
    badge?: string;
}

/** A per-row secondary action. Restrict it to certain scopes with `scopes`. */
export interface SearchRowAction {
    id: string;
    label: string;
    scopes?: string[];
}

/** A row with everything the template needs already resolved. */
interface RenderedRow {
    entity: SearchEntity;
    domId: string;
    icon?: string;
    titleSegments: Segment[];
    subtitleSegments: Segment[] | null;
    actions: SearchRowAction[];
}

interface RenderedGroup {
    key: string;
    label: string;
    rows: RenderedRow[];
    /** Heading count — "5 of 212" once the group is cut to its limit; null for pinned and recent groups. */
    countLabel: string | null;
    /** The group's accessible name. The visible heading is aria-hidden, so the count has to be spoken from here. */
    ariaLabel: string;
}

interface RenderGroup {
    key: string;
    label: string;
    /** The rows that render — already cut to the group limit. */
    items: SearchEntity[];
    /** How many matched before the cut. */
    total: number;
    counted: boolean;
}

/** One run of text, flagged as a query match or not. */
interface Segment {
    text: string;
    match: boolean;
}

/**
 * entity-search — a scoped record-search overlay (NPT-1125).
 *
 * Ported from ProjectFirma2's Angular reimplementation of Ecology's esa-entity-search. It is a COPY, not a
 * dependency: nothing here references either project, so change it freely. Added for Neptune: `badge`, `pinned`
 * scopes, `groupLimit` / `scopedLimit`, and `requireQuery`, which together let it sit over tens of thousands of
 * records with a short fixed list of actions pinned on top.
 *
 * Feed it `entities` + `scopes`; it filters client-side, shows per-scope counts, highlights matches and groups
 * by scope. It owns no data and no routing — it emits `entitySelect` and the consumer acts.
 *
 * THE OVERLAY IS A NATIVE <dialog> OPENED WITH showModal(). That gives the focus trap, a genuinely inert page
 * behind it, and focus RETURN to whatever opened it — hand-rolled overlays kept dropping focus to <body> on close.
 *
 * THREE KEYBOARD RULES, all load-bearing:
 *   1. TAB IS NOT HIJACKED. Cycling facets with Tab left the per-row action buttons reachable by no key at all.
 *      Left/Right drive the facets (a radiogroup), roving tabindex gives the checked one a real tab stop, and
 *      showModal keeps Tab inside the dialog.
 *   2. LEFT/RIGHT ONLY ONCE FOCUS HAS LEFT THE INPUT. The handler sits on the dialog and sees the field's keys
 *      too — unguarded, pressing Left to fix a typo changed the scope instead of moving the caret.
 *   3. UP/DOWN WORK FROM ANYWHERE IN THE OVERLAY, and return focus to the input when they fire, because
 *      aria-activedescendant only announces from the element that HAS focus.
 */
@Component({
    selector: "entity-search",
    templateUrl: "./entity-search.component.html",
    styleUrl: "./entity-search.component.scss",
    changeDetection: ChangeDetectionStrategy.OnPush,
    // A DOCUMENT listener because its whole job is to fire when focus is elsewhere. Inert unless `hotkey` is set.
    host: { "(document:keydown)": "onGlobalKeydown($event)" },
})
export class EntitySearchComponent {
    readonly entities = input<SearchEntity[]>([]);
    readonly scopes = input<SearchScope[]>([]);
    readonly recent = input<SearchEntity[]>([]);
    readonly rowActions = input<SearchRowAction[]>([]);
    readonly placeholder = input<string>("Search…");
    /** The input's accessible name. Falls back to the placeholder, but a placeholder's trailing "…" gets read aloud. */
    readonly label = input<string>("");
    readonly allLabel = input<string>("All");

    /** Rows each group renders under "All"; a cut group's heading reads "5 of 212". 0 = every match. */
    readonly groupLimit = input(0, { transform: numberAttribute });
    /** The same cap once a single facet is active. 0 = every match. */
    readonly scopedLimit = input(0, { transform: numberAttribute });
    /** List no records until something is typed (pinned and recent groups still show). */
    readonly requireQuery = input(false, { transform: booleanAttribute });

    /** Opt-in per instance, so two searches on a page don't fight over the key. */
    readonly hotkey = input<"" | "mod+k" | "slash">("");

    /** Two-way, so a consumer can open it from its own trigger. */
    readonly open = model(false);

    // NOT `select`: Angular also attaches a native DOM listener for any bound event name, and the inner <input> fires a
    // bubbling native `select` whenever text in it is selected — so a consumer's handler got a DOM Event, not an entity.
    readonly entitySelect = output<SearchEntity>();
    readonly scopeChange = output<string>();
    readonly showAll = output<{ query: string; scope: string }>();

    /**
     * Whether a consumer handles `showAll`. OutputEmitterRef exposes no subscriber count, so this is an explicit
     * opt-in; without it Cmd/Ctrl+Enter would close the overlay and discard the query with nothing handling it.
     */
    readonly showAllEnabled = input(false, { transform: booleanAttribute });
    readonly rowAction = output<{ action: SearchRowAction; entity: SearchEntity }>();

    private readonly dialogRef = viewChild<ElementRef<HTMLDialogElement>>("dialog");
    private readonly inputRef = viewChild<ElementRef<HTMLInputElement>>("input");

    /** Per-instance id prefix: aria-controls / aria-activedescendant are document-wide, and the dialog is always in the DOM. */
    private static nextUid = 0;
    private readonly uid = `entity-search-${EntitySearchComponent.nextUid++}`;
    protected readonly resultsId = `${this.uid}-results`;

    protected readonly query = signal("");
    protected readonly activeScope = signal("");
    protected readonly activeId = signal<string | null>(null);

    constructor() {
        // The dialog's open state follows the model, so show()/close() and a consumer's binding cannot disagree.
        effect(() => {
            const el = this.dialogRef()?.nativeElement;
            if (!el) return;
            if (this.open()) {
                if (!el.open) el.showModal();
                requestAnimationFrame(() => this.focusInput());
            } else if (el.open) {
                el.close();
            }
        });
    }

    // ---- data ----

    protected readonly facetScopes = computed(() => this.scopes().filter((s) => !s.pinned));
    private readonly pinnedScopes = computed(() => this.scopes().filter((s) => s.pinned));

    protected readonly hasQuery = computed(() => this.query().trim().length > 0);

    /**
     * Matches the query IGNORING the active scope, bucketed by scope in the same pass — the counts and the
     * grouping both read this, and with tens of thousands of entities a per-scope re-filter on every keystroke
     * is the cost that matters.
     */
    private readonly matchesByScope = computed<Map<string, SearchEntity[]>>(() => {
        const q = this.query().toLowerCase().trim();
        const byScope = new Map<string, SearchEntity[]>();
        for (const e of this.entities()) {
            if (q && !`${e.title} ${e.subtitle ?? ""}`.toLowerCase().includes(q)) continue;
            const list = byScope.get(e.scope);
            if (list) list.push(e);
            else byScope.set(e.scope, [e]);
        }
        return byScope;
    });

    /** Per-facet counts. Pinned scopes are not facets and count toward nothing, "All" included. */
    protected readonly scopeCounts = computed<Record<string, number>>(() => {
        const byScope = this.matchesByScope();
        const counts: Record<string, number> = {};
        for (const scope of this.facetScopes()) counts[scope.id] = byScope.get(scope.id)?.length ?? 0;
        return counts;
    });

    protected readonly allCount = computed(() => Object.values(this.scopeCounts()).reduce((sum, n) => sum + n, 0));

    /** Counts are shown once there is a query, or always when the list is not waiting on one. */
    protected readonly showCounts = computed(() => this.hasQuery() || !this.requireQuery());

    private group(key: string, label: string, matches: SearchEntity[], limit: number, counted = true): RenderGroup {
        return { key, label, items: limit > 0 ? matches.slice(0, limit) : matches, total: matches.length, counted };
    }

    /**
     * What renders, in keyboard-nav order.
     *   - a facet is active: that scope alone, cut to `scopedLimit` (nothing before a query under `requireQuery`).
     *   - "All": pinned scopes first, then — on an empty query — `recent` if there is any, else nothing under
     *     `requireQuery`, else every record grouped by scope; with a query, each facet scope that matched, cut
     *     to `groupLimit`. Entities whose scope is not declared surface as "Other" rather than vanish.
     */
    protected readonly renderGroups = computed<RenderGroup[]>(() => {
        const active = this.activeScope();
        const byScope = this.matchesByScope();
        const facets = this.facetScopes();
        const hasQuery = this.hasQuery();

        // Ungrouped when no scopes are declared, so a plain search still lists its matches.
        if (!this.scopes().length) {
            if (!hasQuery && this.requireQuery()) return [];
            const all = [...byScope.values()].flat();
            return all.length ? [this.group("__ungrouped__", "Results", all, this.groupLimit(), false)] : [];
        }

        if (active) {
            if (!hasQuery && this.requireQuery()) return [];
            const scope = facets.find((s) => s.id === active);
            const items = byScope.get(active) ?? [];
            return scope && items.length ? [this.group(scope.id, scope.label, items, this.scopedLimit())] : [];
        }

        const pinned = this.pinnedScopes()
            .map((scope) => this.group(scope.id, scope.label, byScope.get(scope.id) ?? [], 0, false))
            .filter((g) => g.items.length > 0);
        if (!hasQuery && this.recent().length) return [...pinned, this.group("__recent__", "Recent", this.recent(), 0, false)];
        if (!hasQuery && this.requireQuery()) return pinned;

        const records = facets.map((scope) => this.group(scope.id, scope.label, byScope.get(scope.id) ?? [], this.groupLimit())).filter((g) => g.items.length > 0);

        const known = new Set(this.scopes().map((s) => s.id));
        const orphans = [...byScope.entries()].filter(([scopeID]) => !known.has(scopeID)).flatMap(([, items]) => items);
        const other = orphans.length ? [this.group("__other__", "Other", orphans, this.groupLimit())] : [];
        return [...pinned, ...records, ...other];
    });

    /** The flat keyboard order across groups. */
    protected readonly flatItems = computed<SearchEntity[]>(() => this.renderGroups().flatMap((g) => g.items));

    /** An empty query under `requireQuery` with nothing pinned or recent is not a no-results state — nothing was asked yet. */
    protected readonly showEmpty = computed(() => !this.renderGroups().length && (this.hasQuery() || !this.requireQuery()));

    /** Everything the template needs, resolved once per change rather than by methods called from the markup. */
    protected readonly renderRows = computed<RenderedGroup[]>(() => {
        const scopes = this.scopes();
        const actions = this.rowActions();
        return this.renderGroups().map((g) => {
            const countLabel = g.counted ? (g.items.length < g.total ? `${g.items.length} of ${g.total}` : `${g.total}`) : null;
            return {
                key: g.key,
                label: g.label,
                countLabel,
                ariaLabel: countLabel ? `${g.label}, ${countLabel}` : g.label,
                rows: g.items.map((entity) => ({
                    entity,
                    // THE ONLY PLACE A ROW ID IS SPELLED; activeDomId reads it back rather than rebuilding it.
                    domId: `${this.uid}-row-${entity.id}`,
                    icon: entity.icon ?? scopes.find((s) => s.id === entity.scope)?.icon,
                    titleSegments: this.splitOnQuery(entity.title),
                    subtitleSegments: entity.subtitle ? this.splitOnQuery(entity.subtitle) : null,
                    actions: actions.filter((a) => !a.scopes || a.scopes.includes(entity.scope)),
                })),
            };
        });
    });

    /** The highlighted row's DOM id for aria-activedescendant — looked up from the rendered rows, so it can only name a real one. */
    protected readonly activeDomId = computed<string | null>(() => {
        const id = this.activeId();
        if (id === null) return null;
        for (const group of this.renderRows()) {
            const row = group.rows.find((r) => r.entity.id === id);
            if (row) return row.domId;
        }
        return null;
    });

    /** Splits text into matched and unmatched runs, so no markup is ever built from data (no innerHTML). */
    private splitOnQuery(text: string): Segment[] {
        const q = this.query().trim();
        if (!q) return [{ text, match: false }];
        const out: Segment[] = [];
        const lower = text.toLowerCase();
        const needle = q.toLowerCase();
        let i = 0;
        for (;;) {
            const at = lower.indexOf(needle, i);
            if (at === -1) break;
            if (at > i) out.push({ text: text.slice(i, at), match: false });
            out.push({ text: text.slice(at, at + needle.length), match: true });
            i = at + needle.length;
        }
        if (i < text.length) out.push({ text: text.slice(i), match: false });
        return out;
    }

    // ---- open / close ----

    show(): void {
        this.query.set("");
        this.activeScope.set("");
        this.activeId.set(null);
        this.open.set(true);
    }

    close(): void {
        this.open.set(false);
    }

    toggle(): void {
        this.open() ? this.close() : this.show();
    }

    /** The native dialog can close itself (Escape, backdrop) — keep the model in step. */
    protected onNativeClose(): void {
        this.open.set(false);
    }

    // ---- interaction ----

    protected onQuery(event: Event): void {
        this.query.set((event.target as HTMLInputElement).value);
        // Highlight the first row rather than clearing the highlight, so type-then-Enter opens it.
        const first = this.flatItems()[0];
        this.activeId.set(first ? first.id : null);
    }

    protected onGlobalKeydown(event: KeyboardEvent): void {
        const key = this.hotkey();
        if (key === "mod+k" && (event.metaKey || event.ctrlKey) && event.key?.toLowerCase() === "k") {
            event.preventDefault();
            this.toggle();
        } else if (key === "slash" && event.key === "/" && !this.isEditable(event.target) && !this.open()) {
            // isEditable: or every "/" typed into any field opens search. !open(): or "/" on a focused facet
            // re-enters show() and wipes the query out from under the user.
            event.preventDefault();
            this.show();
        }
    }

    private isEditable(target: EventTarget | null): boolean {
        const node = target as HTMLElement | null;
        if (!node) return false;
        const tag = node.tagName;
        return tag === "INPUT" || tag === "TEXTAREA" || tag === "SELECT" || node.isContentEditable;
    }

    /** The input is where focus rests, so clicked controls hand it back. */
    private focusInput(): void {
        this.inputRef()?.nativeElement.focus();
    }

    private activeInput(): boolean {
        const el = this.inputRef()?.nativeElement;
        return !!el && document.activeElement === el;
    }

    /**
     * Focus follows whatever moved the scope: typing leaves it in the input, arrow keys move it to the newly
     * checked facet (roving tabindex), and a click hands it back to the input so the next keystrokes still land.
     */
    protected setScope(scopeId: string, via: "pointer" | "keyboard" = "keyboard", fromInput = this.activeInput()): void {
        this.activeScope.set(scopeId);
        this.activeId.set(null);
        this.scopeChange.emit(scopeId);
        if (fromInput) return;
        if (via === "pointer") {
            requestAnimationFrame(() => this.focusInput());
            return;
        }
        requestAnimationFrame(() => this.checkedScopeButton()?.focus());
    }

    private checkedScopeButton(): HTMLElement | null {
        return this.dialogRef()?.nativeElement.querySelector<HTMLElement>(".entity-search__scope[tabindex='0']") ?? null;
    }

    private cycleScope(dir: 1 | -1): void {
        const ids = ["", ...this.facetScopes().map((s) => s.id)];
        const idx = ids.indexOf(this.activeScope());
        this.setScope(ids[(idx + dir + ids.length) % ids.length], "keyboard", false);
    }

    protected onKeydown(event: KeyboardEvent): void {
        if (event.key === "Escape") {
            event.preventDefault();
            this.close();
            return;
        }

        // Rule 2: the facets own Left/Right only once focus is actually on them.
        if (this.facetScopes().length && !this.activeInput() && (event.key === "ArrowRight" || event.key === "ArrowLeft")) {
            event.preventDefault();
            this.cycleScope(event.key === "ArrowRight" ? 1 : -1);
            return;
        }

        // Only if someone handles it; otherwise fall through so plain Enter handling still opens the row.
        if (event.key === "Enter" && (event.metaKey || event.ctrlKey) && this.showAllEnabled()) {
            event.preventDefault();
            this.showAll.emit({ query: this.query(), scope: this.activeScope() });
            this.close();
            return;
        }

        const flat = this.flatItems();

        // Rule 3: from anywhere in the overlay, and focus comes back to the input.
        if (event.key === "ArrowDown" || event.key === "ArrowUp") {
            event.preventDefault();
            if (!flat.length) return;
            if (!this.activeInput()) this.focusInput();
            const current = flat.findIndex((e) => e.id === this.activeId());
            const next = event.key === "ArrowDown" ? (current < flat.length - 1 ? current + 1 : 0) : current > 0 ? current - 1 : flat.length - 1;
            this.activeId.set(flat[next].id);
            this.scrollActiveIntoView();
            return;
        }

        // Enter stays with whatever has focus: on a facet or a row action it activates THAT control.
        if (!this.activeInput()) return;
        if (event.key === "Enter" && flat.length) {
            event.preventDefault();
            const active = flat.find((e) => e.id === this.activeId()) ?? (flat.length === 1 ? flat[0] : null);
            if (active) this.selectEntity(active);
        }
    }

    private scrollActiveIntoView(): void {
        const id = this.activeId();
        if (!id) return;
        requestAnimationFrame(() => {
            this.dialogRef()
                ?.nativeElement.querySelector<HTMLElement>(`[data-entity-id="${CSS.escape(id)}"]`)
                ?.scrollIntoView({ block: "nearest" });
        });
    }

    protected selectEntity(entity: SearchEntity): void {
        this.entitySelect.emit(entity);
        this.close();
    }

    protected onRowAction(event: Event, action: SearchRowAction, entity: SearchEntity): void {
        // The row is itself activatable, so a click on the action must not also select it.
        event.stopPropagation();
        this.rowAction.emit({ action, entity });
    }
}
