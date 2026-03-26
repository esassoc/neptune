import L, { Control, Layer, Map, ControlOptions, DomUtil, DomEvent } from "leaflet";

export interface GroupedLayersOptions extends ControlOptions {
    sortLayers?: boolean;
    sortGroups?: boolean;
    sortBaseLayers?: boolean;
    collapsed?: boolean;
    autoZIndex?: boolean;
    exclusiveGroups?: string[];
    groupCheckboxes?: boolean;
    groupsCollapsable?: boolean;
    groupsExpandedClass?: string;
    groupsCollapsedClass?: string;
    sortFunction?: (layerA: Layer, layerB: Layer, nameA: string, nameB: string) => number;
}

interface GroupInfo {
    name: string;
    id: number;
    exclusive: boolean;
}

interface LayerEntry {
    layer: Layer;
    name: string;
    overlay?: boolean;
    sortOrder?: number;
    group?: GroupInfo;
}

export class GroupedLayers extends Control {
    options: GroupedLayersOptions;
    private _layers: LayerEntry[] = [];
    private _lastZIndex = 0;
    private _handlingClick = false;
    private _groupList: string[] = [];
    private _domGroups: HTMLElement[] = [];
    private _container!: HTMLElement;
    private _form!: HTMLFormElement;
    private _baseLayersList!: HTMLElement;
    private _separator!: HTMLElement;
    private _overlaysList!: HTMLElement;
    private _layersLink!: HTMLAnchorElement;
    private _map!: Map;

    constructor(baseLayers: { [name: string]: Layer }, groupedOverlays: { [group: string]: { [name: string]: Layer } }, options?: GroupedLayersOptions) {
        super(options);
        L.Util.setOptions(this, options);
        for (const i in baseLayers) {
            this._addLayer(baseLayers[i], i);
        }
        for (const i in groupedOverlays) {
            for (const j in groupedOverlays[i]) {
                this._addLayer(groupedOverlays[i][j], j, i, true);
            }
        }
    }

    onAdd(map: Map): HTMLElement {
        this._map = map;
        this._initLayout();
        this._addCloseButton();
        this._update();
        map.on("layeradd", this._onLayerChange, this).on("layerremove", this._onLayerChange, this);
        return this._container;
    }

    addTo(map: Map): this {
        super.addTo(map);
        this._expandIfNotCollapsed();
        return this;
    }

    onRemove(map: Map): void {
        map.off("layeradd", this._onLayerChange, this).off("layerremove", this._onLayerChange, this);
    }

    addBaseLayer(layer: Layer, name: string): this {
        this._addLayer(layer, name);
        this._update();
        return this;
    }

    addOverlay(layer: Layer, name: string, group?: string): this {
        this._addLayer(layer, name, group, true);
        this._update();
        return this;
    }

    removeLayer(layer: Layer): this {
        const id = L.Util.stamp(layer);
        const _layer = this._getLayer(id);
        if (_layer) {
            this._layers.splice(this._layers.indexOf(_layer), 1);
        }
        this._update();
        return this;
    }

    /**
     * Returns all LayerEntry objects managed by this control.
     */
    public getLayers(): LayerEntry[] {
        return [...this._layers];
    }

    private _getLayer(id: number): LayerEntry | undefined {
        return this._layers.find((l) => l && L.stamp(l.layer) === id);
    }

    private _initLayout(): void {
        const className = "leaflet-control-layers";
        const container = (this._container = DomUtil.create("div", className));
        const collapsed = this.options.collapsed;
        container.setAttribute("aria-haspopup", "true");
        DomEvent.disableClickPropagation(container);
        DomEvent.disableScrollPropagation(container);
        const form = (this._form = DomUtil.create("form", className + "-list") as HTMLFormElement);
        if (collapsed) {
            this._map.on("click", this._collapse, this);
            if (!(L as any).Browser.android) {
                DomEvent.on(
                    container,
                    {
                        mouseenter: this._expand,
                        mouseleave: this._collapse,
                    },
                    this
                );
            }
        }
        const link = (this._layersLink = DomUtil.create("a", className + "-toggle", container) as HTMLAnchorElement);
        link.href = "#";
        link.title = "Layers";
        if ((L as any).Browser.touch) {
            DomEvent.on(link, "click", DomEvent.stop);
            DomEvent.on(link, "click", this._expand, this);
        } else {
            DomEvent.on(link, "focus", this._expand, this);
        }
        if (!collapsed) {
            this._expand();
        }
        this._baseLayersList = DomUtil.create("div", className + "-base", form);
        this._separator = DomUtil.create("div", className + "-separator", form);
        this._overlaysList = DomUtil.create("div", className + "-overlays", form);
        container.appendChild(form);
    }

    private _addLayer(layer: Layer, name: string, group?: string, overlay?: boolean): void {
        const _layer: LayerEntry = {
            layer,
            name,
            overlay,
            sortOrder: (layer as any).sortOrder || 1,
        };
        this._layers.push(_layer);
        group = group || "";
        let groupId = this._indexOf(this._groupList, group);
        if (groupId === -1) {
            groupId = this._groupList.push(group) - 1;
        }
        const exclusive = this._indexOf(this.options.exclusiveGroups || [], group) !== -1;
        _layer.group = {
            name: group,
            id: groupId,
            exclusive,
        };
        if (this.options.autoZIndex && (layer as any).setZIndex) {
            this._lastZIndex++;
            (layer as any).setZIndex(this._lastZIndex);
        }
        // Sorting logic omitted for brevity; can be added as needed
        this._expandIfNotCollapsed();
    }

    private _update = (): void => {
        if (!this._container) return;
        this._baseLayersList.innerHTML = "";
        this._overlaysList.innerHTML = "";
        this._domGroups.length = 0;
        let baseLayersPresent = false,
            overlaysPresent = false;
        for (const obj of this._layers) {
            this._addItem(obj);
            overlaysPresent = overlaysPresent || !!obj.overlay;
            baseLayersPresent = baseLayersPresent || !obj.overlay;
        }
        if (this.options.groupCheckboxes) {
            this._refreshGroupsCheckStates();
        }
        this._separator.style.display = overlaysPresent && baseLayersPresent ? "" : "none";
    };

    private _onLayerChange = (e: any): void => {
        const obj = this._getLayer(L.Util.stamp(e.layer));
        if (!obj) return;
        if (!this._handlingClick) {
            this._update();
        }
        let type: string | null;
        if (obj.overlay) {
            type = e.type === "layeradd" ? "overlayadd" : "overlayremove";
        } else {
            type = e.type === "layeradd" ? "baselayerchange" : null;
        }
        if (type) {
            this._map.fire(type, obj);
        }
    };

    private _createRadioElement(name: string, checked: boolean): HTMLInputElement {
        const radio = DomUtil.create("input") as HTMLInputElement;
        radio.type = "radio";
        radio.name = name;
        radio.className = "leaflet-control-layers-selector";
        radio.checked = checked;
        return radio;
    }

    private _addItem(obj: LayerEntry): HTMLElement {
        const label = DomUtil.create("label");
        let input: HTMLInputElement;
        const checked = this._map.hasLayer(obj.layer);
        let container: HTMLElement;
        let groupRadioName: string;
        if (obj.overlay) {
            if (obj.group!.exclusive) {
                groupRadioName = "leaflet-exclusive-group-layer-" + obj.group!.id;
                input = this._createRadioElement(groupRadioName, checked);
            } else {
                input = DomUtil.create("input") as HTMLInputElement;
                input.type = "checkbox";
                input.className = "leaflet-control-layers-selector";
                input.defaultChecked = checked;
            }
        } else {
            input = this._createRadioElement("leaflet-base-layers", checked);
        }
        (input as any).layerId = L.Util.stamp(obj.layer);
        (input as any).groupID = obj.group!.id;
        DomEvent.on(input, "click", this._onInputClick, this);
        const name = DomUtil.create("span");
        name.innerHTML = " " + obj.name;
        label.appendChild(input);
        label.appendChild(name);
        if (obj.overlay) {
            container = this._overlaysList;
            let groupContainer = this._domGroups[obj.group!.id];
            if (!groupContainer) {
                groupContainer = DomUtil.create("div");
                groupContainer.className = "leaflet-control-layers-group";
                groupContainer.id = "leaflet-control-layers-group-" + obj.group!.id;
                const groupLabel = DomUtil.create("label");
                groupLabel.className = "leaflet-control-layers-group-label";
                if (obj.group!.name !== "" && !obj.group!.exclusive) {
                    if (this.options.groupCheckboxes) {
                        const groupInput = DomUtil.create("input") as HTMLInputElement;
                        groupInput.type = "checkbox";
                        groupInput.className = "leaflet-control-layers-group-selector";
                        (groupInput as any).groupID = obj.group!.id;
                        (groupInput as any).legend = this;
                        DomEvent.on(groupInput, "click", this._onGroupInputClick, groupInput);
                        groupLabel.appendChild(groupInput);
                    }
                }
                if (this.options.groupsCollapsable) {
                    groupContainer.classList.add("group-collapsable");
                    groupContainer.classList.add("collapsed");
                    const groupMin = DomUtil.create("span");
                    groupMin.className = "leaflet-control-layers-group-collapse " + (this.options.groupsExpandedClass || "");
                    groupLabel.appendChild(groupMin);
                    const groupMax = DomUtil.create("span");
                    groupMax.className = "leaflet-control-layers-group-expand " + (this.options.groupsCollapsedClass || "");
                    groupLabel.appendChild(groupMax);
                    DomEvent.on(groupLabel, "click", this._onGroupCollapseToggle, groupContainer);
                }
                const groupName = DomUtil.create("span");
                groupName.className = "leaflet-control-layers-group-name";
                groupName.innerHTML = obj.group!.name;
                groupLabel.appendChild(groupName);
                groupContainer.appendChild(groupLabel);
                container.appendChild(groupContainer);
                this._domGroups[obj.group!.id] = groupContainer;
            }
            container = groupContainer;
        } else {
            container = this._baseLayersList;
        }
        container.appendChild(label);
        return label;
    }

    private _onGroupCollapseToggle = (event: Event): void => {
        DomEvent.stopPropagation(event);
        DomEvent.preventDefault(event);
        const groupContainer = event.currentTarget as HTMLElement;
        if (groupContainer.classList.contains("group-collapsable") && groupContainer.classList.contains("collapsed")) {
            groupContainer.classList.remove("collapsed");
        } else if (groupContainer.classList.contains("group-collapsable") && !groupContainer.classList.contains("collapsed")) {
            groupContainer.classList.add("collapsed");
        }
    };

    private _onGroupInputClick = function (this: HTMLInputElement, event: Event): void {
        DomEvent.stopPropagation(event);
        const this_legend = (this as any).legend as GroupedLayers;
        this_legend._handlingClick = true;
        const inputs = this_legend._form.getElementsByTagName("input");
        for (const input of Array.from(inputs)) {
            if ((input as any).groupID === (this as any).groupID && input.className === "leaflet-control-layers-selector") {
                (input as HTMLInputElement).checked = (this as HTMLInputElement).checked;
                const obj = this_legend._getLayer((input as any).layerId);
                if ((input as HTMLInputElement).checked && !this_legend._map.hasLayer(obj!.layer)) {
                    this_legend._map.addLayer(obj!.layer);
                } else if (!(input as HTMLInputElement).checked && this_legend._map.hasLayer(obj!.layer)) {
                    this_legend._map.removeLayer(obj!.layer);
                }
            }
        }
        this_legend._handlingClick = false;
    };

    private _onInputClick = (): void => {
        let obj: LayerEntry | undefined;
        const inputs = this._form.getElementsByClassName("leaflet-control-layers-selector");
        let toBeRemoved: Layer | undefined;
        let toBeAdded: Layer | undefined;
        this._handlingClick = true;
        for (const input of Array.from(inputs) as HTMLInputElement[]) {
            obj = this._getLayer((input as any).layerId);
            if (input.checked && !this._map.hasLayer(obj!.layer)) {
                toBeAdded = obj!.layer;
            } else if (!input.checked && this._map.hasLayer(obj!.layer)) {
                toBeRemoved = obj!.layer;
            }
        }
        if (toBeRemoved !== undefined) {
            this._map.removeLayer(toBeRemoved);
        }
        if (toBeAdded !== undefined) {
            this._map.addLayer(toBeAdded);
            // When switching base layers, the newly added tile layer renders on top of
            // existing overlay layers because it's appended last in the DOM.  Force a
            // low z-index so it stays behind overlays (fixes base-layer-on-top bug).
            if ((toBeAdded as any).setZIndex && !this._getLayer(L.Util.stamp(toBeAdded))?.overlay) {
                (toBeAdded as any).setZIndex(0);
            }
        }
        if (this.options.groupCheckboxes) {
            this._refreshGroupsCheckStates();
        }
        this._handlingClick = false;
    };

    private _refreshGroupsCheckStates(): void {
        for (let i = 0; i < this._domGroups.length; i++) {
            const groupContainer = this._domGroups[i];
            if (groupContainer) {
                const groupInput = groupContainer.getElementsByClassName("leaflet-control-layers-group-selector")[0] as HTMLInputElement;
                const groupItemInputs = groupContainer.querySelectorAll("input.leaflet-control-layers-selector");
                const checkedGroupItemInputs = groupContainer.querySelectorAll("input.leaflet-control-layers-selector:checked");
                if (groupInput) {
                    groupInput.indeterminate = false;
                    if (checkedGroupItemInputs.length === groupItemInputs.length) {
                        groupInput.checked = true;
                    } else if (checkedGroupItemInputs.length === 0) {
                        groupInput.checked = false;
                    } else {
                        groupInput.indeterminate = true;
                    }
                }
            }
        }
    }

    private _expand = (): this => {
        DomUtil.addClass(this._container, "leaflet-control-layers-expanded");
        this._form.style.height = "";
        const acceptableHeight = this._map.getSize().y - (this._container.offsetTop + 50);
        if (acceptableHeight < this._form.clientHeight) {
            DomUtil.addClass(this._form, "leaflet-control-layers-scrollbar");
            this._form.style.height = acceptableHeight + "px";
        } else {
            DomUtil.removeClass(this._form, "leaflet-control-layers-scrollbar");
        }
        return this;
    };

    private _expandIfNotCollapsed = (): this => {
        if (this._map && !this.options.collapsed) {
            this._expand();
        }
        return this;
    };

    private _collapse = (): void => {
        this._container.className = this._container.className.replace(" leaflet-control-layers-expanded", "");
    };

    private _indexOf(arr: any[], obj: any): number {
        for (let i = 0, j = arr.length; i < j; i++) {
            if (arr[i] === obj) {
                return i;
            }
        }
        return -1;
    }

    private _addCloseButton = (): void => {
        const elements = this._container.getElementsByClassName("leaflet-control-layers-list");
        const closeButtonDiv = DomUtil.create("div");
        closeButtonDiv.className = "leaflet-control-layers-close-button-wrapper";
        const button = DomUtil.create("a", "leaflet-control-layers-close-button", closeButtonDiv);
        (button as any).role = "button";
        button.title = "Collapse layer control";
        button.innerHTML = "Close";
        button.addEventListener("click", () => this._collapse());
        if (elements[0]) {
            elements[0].appendChild(closeButtonDiv);
        }
    };
}

export function groupedLayers(
    baseLayers: { [name: string]: Layer },
    groupedOverlays: { [group: string]: { [name: string]: Layer } },
    options?: GroupedLayersOptions
): GroupedLayers {
    return new GroupedLayers(baseLayers, groupedOverlays, options);
}
