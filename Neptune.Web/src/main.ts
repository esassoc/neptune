// NPT-998: Eagerly init leaflet then leaflet.markercluster so window.L permanently has the
// plugin attached. When the delineation-map lazy chunk loads, esbuild's QA-optimized build
// reorders the lazy CJS inits inside that chunk so markercluster runs BEFORE leaflet npm;
// leaflet npm's body then overwrites window.L with a fresh exports object that has no
// markerClusterGroup. Importing here forces both CJS init bodies to run eagerly during main
// bundle evaluation in dependency order, and the cached wrappers no-op when the lazy chunk
// later asks for them — window.L stays valid.
import "leaflet";
import "leaflet.markercluster";

import { AppComponent } from "./app/app.component";
import { createApplication } from "@angular/platform-browser";
import { appConfig } from "./app/app.config";

(async () => {
    const app = createApplication(appConfig);
    (await app).bootstrap(AppComponent);

    // todo: example of creating a custom popup in leaflet
    // const wriaPopupComponent = createCustomElement(WaterResourceInventoryAreaPopupComponent, {
    //     injector: (await app).injector,
    // });
    // customElements.define("water-resource-inventory-area-popup-custom-element", wriaPopupComponent);
})();
