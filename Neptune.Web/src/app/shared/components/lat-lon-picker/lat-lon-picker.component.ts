import { Component, EventEmitter, Input, OnDestroy, OnInit, Output, TemplateRef } from "@angular/core";
import { CommonModule } from "@angular/common";
import { FormControl, FormsModule, ReactiveFormsModule } from "@angular/forms";
import { FormFieldComponent, FormFieldType } from "src/app/shared/components/forms/form-field/form-field.component";
import { NeptuneMapComponent, NeptuneMapInitEvent } from "src/app/shared/components/leaflet/neptune-map/neptune-map.component";
import { IconComponent } from "src/app/shared/components/icon/icon.component";
import * as L from "leaflet";
import { merge, Subscription } from "rxjs";
import { BoundingBoxDto } from "src/app/shared/generated/model/bounding-box-dto";

@Component({
    selector: "lat-lon-picker",
    standalone: true,
    imports: [CommonModule, FormsModule, ReactiveFormsModule, FormFieldComponent, NeptuneMapComponent, IconComponent],
    templateUrl: "./lat-lon-picker.component.html",
    styleUrls: ["./lat-lon-picker.component.scss"],
})
export class LatLonPickerComponent implements OnInit, OnDestroy {
    public FormFieldType = FormFieldType;
    @Input() latControl: FormControl;
    @Input() lonControl: FormControl;
    /** Optional template to render custom instructions or help UI. If not provided, projected content with attribute [lat-lon-instructions] will be rendered. */
    @Input() instructionsTemplate?: TemplateRef<any>;
    /** Optional initial extent for the map. Read once when the map initializes. */
    @Input() boundingBox?: BoundingBoxDto;
    @Input() mapHeight: string = "400px";

    // emits when user selects a location (either from map or geolocation)
    @Output() locationSelected = new EventEmitter<{ lat: number; lon: number }>();
    // emits once the underlying map is ready, so callers can add their own layers (projected as content)
    @Output() mapLoad = new EventEmitter<NeptuneMapInitEvent>();

    private map: L.Map | null = null;
    private clickHandler: any;
    private marker: L.Marker | null = null;
    private controlValueSubscription: Subscription | null = null;

    ngOnInit(): void {}

    onMapLoad(event: NeptuneMapInitEvent) {
        this.map = event.map as L.Map;

        // attach click handler
        this.clickHandler = (e: any) => {
            const lat = e.latlng?.lat ?? e.latitude ?? null;
            const lon = e.latlng?.lng ?? e.longitude ?? null;
            if (lat != null && lon != null) {
                this.setLatLon(lat, lon);
            }
        };

        if (this.map) {
            this.map.on("click", this.clickHandler);

            // if controls already have values, show marker
            const latVal = this.latControl?.value;
            const lonVal = this.lonControl?.value;
            if (latVal != null && lonVal != null) {
                this.updateMarker(latVal, lonVal);
                try {
                    this.map.setView([latVal, lonVal], 15);
                } catch {}
            }

            // keep the marker in sync when coordinates are typed directly
            if (this.latControl && this.lonControl) {
                this.controlValueSubscription = merge(this.latControl.valueChanges, this.lonControl.valueChanges).subscribe(() => this.syncMarkerToControls());
            }
        }

        this.mapLoad.emit(event);
    }

    private syncMarkerToControls() {
        const lat = this.latControl?.value;
        const lon = this.lonControl?.value;
        const isValidLat = lat != null && lat !== "" && Number.isFinite(+lat) && +lat >= -90 && +lat <= 90;
        const isValidLon = lon != null && lon !== "" && Number.isFinite(+lon) && +lon >= -180 && +lon <= 180;
        if (isValidLat && isValidLon) {
            this.updateMarker(+lat, +lon);
        } else {
            this.removeMarker();
        }
    }

    useCurrentLocation() {
        if (!navigator || !navigator.geolocation) {
            return;
        }

        navigator.geolocation.getCurrentPosition(
            (pos) => {
                const lat = pos.coords.latitude;
                const lon = pos.coords.longitude;
                this.setLatLon(lat, lon);

                // center map if available
                if (this.map) {
                    this.map.setView([lat, lon], 15);
                }
            },
            (err) => {
                console.warn("Geolocation error", err);
            },
            { enableHighAccuracy: true }
        );
    }

    private setLatLon(lat: number, lon: number) {
        //Round to 5 decimal places
        lat = Math.round(lat * 100000) / 100000;
        lon = Math.round(lon * 100000) / 100000;

        if (this.latControl) {
            this.latControl.setValue(lat);
            try {
                this.latControl.markAsDirty();
                this.latControl.markAsTouched();
            } catch {}
        }
        if (this.lonControl) {
            this.lonControl.setValue(lon);
            try {
                this.lonControl.markAsDirty();
                this.lonControl.markAsTouched();
            } catch {}
        }
        this.locationSelected.emit({ lat, lon });

        // update marker on the map
        this.updateMarker(lat, lon);
    }

    private updateMarker(lat: number, lon: number) {
        if (!this.map) return;
        if (this.marker) {
            this.marker.setLatLng([lat, lon]);
        } else {
            this.marker = L.marker([lat, lon], {
                icon: L.icon({
                    iconUrl: "assets/main/map-icons/marker-icon-blue.png",
                    iconSize: [28, 40],
                    iconAnchor: [14, 40],
                    shadowUrl: "",
                }),
            });
            this.marker.addTo(this.map);
        }
    }

    public clearLocation() {
        if (this.latControl) this.latControl.reset();
        if (this.lonControl) this.lonControl.reset();

        this.removeMarker();
    }

    private removeMarker() {
        if (this.marker && this.map) {
            try {
                this.map.removeLayer(this.marker);
            } catch {}
            this.marker = null;
        }
    }

    ngOnDestroy(): void {
        this.controlValueSubscription?.unsubscribe();

        if (this.map && this.clickHandler) {
            this.map.off("click", this.clickHandler);
        }

        this.removeMarker();
    }
}
