import { Injectable } from "@angular/core";
import { Observable } from "rxjs";

export type GeolocationFailure = "unsupported" | "denied" | "unavailable" | "timeout";

export interface GeolocationCoordinates {
    latitude: number;
    longitude: number;
}

@Injectable({
    providedIn: "root",
})
export class GeolocationService {
    // Emits once then completes; errors with a GeolocationFailure so callers can explain what went wrong
    // instead of swallowing it (the older lat-lon-picker / zoom-to-my-location paths only console.warn).
    public getCurrentPosition(): Observable<GeolocationCoordinates> {
        return new Observable<GeolocationCoordinates>((subscriber) => {
            if (!("geolocation" in navigator) || !navigator.geolocation) {
                subscriber.error("unsupported" as GeolocationFailure);
                return;
            }

            navigator.geolocation.getCurrentPosition(
                (position) => {
                    subscriber.next({ latitude: position.coords.latitude, longitude: position.coords.longitude });
                    subscriber.complete();
                },
                (error) => subscriber.error(GeolocationService.toFailure(error)),
                { enableHighAccuracy: true, timeout: 15000, maximumAge: 60000 }
            );
        });
    }

    private static toFailure(error: GeolocationPositionError): GeolocationFailure {
        switch (error.code) {
            case error.PERMISSION_DENIED:
                return "denied";
            case error.TIMEOUT:
                return "timeout";
            default:
                return "unavailable";
        }
    }
}
