import { Component, Input, OnChanges, OnDestroy, SimpleChanges } from "@angular/core";
import * as L from "leaflet";

// Self-contained Leaflet control that recenters the map on the user's current location.
// Projected into a <neptune-map> alongside the reference layers; takes only [map] so it can be
// dropped onto any map. Geolocation logic mirrors lat-lon-picker.useCurrentLocation().
@Component({
    selector: "zoom-to-my-location-control",
    standalone: true,
    imports: [],
    templateUrl: "./zoom-to-my-location-control.component.html",
    styleUrl: "./zoom-to-my-location-control.component.scss",
})
export class ZoomToMyLocationControlComponent implements OnChanges, OnDestroy {
    @Input() map: L.Map;

    private control: L.Control = null;
    private marker: L.Marker = null;

    ngOnChanges(changes: SimpleChanges): void {
        if (changes.map && this.map && !this.control) {
            this.addControl();
        }
    }

    private addControl(): void {
        const ZoomToLocationControl = L.Control.extend({
            options: { position: "topleft" as L.ControlPosition },
            onAdd: () => {
                const container = L.DomUtil.create("div", "leaflet-bar leaflet-control zoom-to-my-location-control");
                const button = L.DomUtil.create("a", "", container) as HTMLAnchorElement;
                button.href = "#";
                button.title = "Zoom to my location";
                button.setAttribute("role", "button");
                button.setAttribute("aria-label", "Zoom to my location");
                button.innerHTML = '<i class="fa fa-location-arrow"></i>';
                L.DomEvent.disableClickPropagation(container);
                L.DomEvent.on(button, "click", (e: Event) => {
                    L.DomEvent.preventDefault(e);
                    this.zoomToMyLocation();
                });
                return container;
            },
        });
        this.control = new ZoomToLocationControl();
        this.control.addTo(this.map);
    }

    private zoomToMyLocation(): void {
        if (!navigator || !navigator.geolocation) {
            return;
        }
        navigator.geolocation.getCurrentPosition(
            (pos) => {
                const lat = pos.coords.latitude;
                const lon = pos.coords.longitude;
                this.map.setView([lat, lon], 16);
                this.updateMarker(lat, lon);
            },
            (err) => {
                console.warn("Geolocation error", err);
            },
            { enableHighAccuracy: true }
        );
    }

    private updateMarker(lat: number, lon: number): void {
        if (!this.map) return;
        if (this.marker) {
            this.marker.setLatLng([lat, lon]);
        } else {
            this.marker = L.marker([lat, lon], {
                icon: L.icon({
                    iconUrl: "assets/main/map-icons/marker-icon-blue.png",
                    iconSize: [25, 41],
                    iconAnchor: [12, 41],
                    popupAnchor: [1, -34],
                    shadowUrl: "assets/main/map-icons/marker-shadow.png",
                    shadowSize: [41, 41],
                }),
            });
            this.marker.addTo(this.map);
        }
    }

    ngOnDestroy(): void {
        if (this.marker && this.map) {
            try {
                this.map.removeLayer(this.marker);
            } catch {}
            this.marker = null;
        }
        if (this.control && this.map) {
            try {
                this.map.removeControl(this.control);
            } catch {}
            this.control = null;
        }
    }
}
