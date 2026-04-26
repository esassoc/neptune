import { Injectable } from "@angular/core";
import { BehaviorSubject, Observable, defer, distinctUntilChanged, finalize, map, shareReplay } from "rxjs";

@Injectable()
export class MapLayerLoadingService {
    private readonly loadingCountSubject = new BehaviorSubject<number>(0);

    public readonly isLoading$: Observable<boolean> = this.loadingCountSubject.pipe(
        map((count) => count > 0),
        distinctUntilChanged(),
        shareReplay({ bufferSize: 1, refCount: true })
    );

    // defer ensures increment fires on subscribe, not at call-time. Layer components call
    // track$() synchronously in ngAfterViewInit and assign the result to a field that the
    // template then subscribes to via async pipe — incrementing eagerly meant the counter
    // ticked up before subscription, and if the resulting CD pass was delayed (it sometimes
    // is on prod-optimized builds), the counter sat at 1 until a stray click forced CD,
    // pinning the map spinner indefinitely.
    public track$<T>(source$: Observable<T>): Observable<T> {
        return defer(() => {
            this.increment();
            return source$.pipe(finalize(() => this.decrement()));
        });
    }

    /**
     * Marks a loading operation as in-progress and returns a function to mark it complete.
     * Safe to call the returned function multiple times.
     */
    public begin(): () => void {
        this.increment();

        let completed = false;
        return () => {
            if (completed) return;
            completed = true;
            this.decrement();
        };
    }

    private increment(): void {
        this.loadingCountSubject.next((this.loadingCountSubject.value ?? 0) + 1);
    }

    private decrement(): void {
        this.loadingCountSubject.next(Math.max(0, (this.loadingCountSubject.value ?? 0) - 1));
    }
}
