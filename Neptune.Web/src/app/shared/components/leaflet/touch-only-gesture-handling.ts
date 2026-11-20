import * as L from "leaflet";
import GestureHandling from "leaflet-gesture-handling";

// NPT-1142: keep leaflet-gesture-handling's touch rule (one finger scrolls the page and shows
// "Use two fingers to move the map"; two fingers pan/pinch the map) but let the mouse wheel zoom
// without Ctrl/Cmd. The stock plugin blocks a plain wheel in _handleScroll and toggles
// scrollWheelZoom together with dragging in _enable/_disableInteractions, so override all three
// and leave scrollWheelZoom to Leaflet (it stays enabled, and its wheel listener is non-passive
// and stops the event, so Ctrl + wheel zooms the map rather than the browser page).
export const TouchOnlyGestureHandling = (GestureHandling as any).extend({
    _handleScroll: function () {},

    _disableInteractions: function () {
        this._map.dragging.disable();
        if (this._map.tap) {
            this._map.tap.disable();
        }
    },

    _enableInteractions: function () {
        this._map.dragging.enable();
        if (this._map.tap) {
            this._map.tap.enable();
        }
    },
}) as typeof L.Handler;
