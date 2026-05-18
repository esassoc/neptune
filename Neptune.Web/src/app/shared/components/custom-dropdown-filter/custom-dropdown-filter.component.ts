import { Component } from "@angular/core";
import { AgFilterComponent } from "ag-grid-angular";
import { IDoesFilterPassParams, RowNode } from "ag-grid-community";
import { FormsModule } from "@angular/forms";


@Component({
    selector: "qanat-custom-dropdown-filter",
    templateUrl: "./custom-dropdown-filter.component.html",
    styleUrls: ["./custom-dropdown-filter.component.scss"],
    imports: [FormsModule]
})
export class CustomDropdownFilterComponent implements AgFilterComponent {
    params;
    field: string;
    dropdownValues = [];
    columnContainsMultipleValues: boolean = false;

    state = {
        selectAll: true,
        deselectAll: false,
        strict: false,
        filterOptions: {},
    };

    agInit(params): void {
        this.params = params;

        if (params.colDef.filterParams) {
            this.field = params.colDef.filterParams.field;
            this.columnContainsMultipleValues = params.colDef.filterParams.columnContainsMultipleValues;
        }

        if (this.columnContainsMultipleValues) {
            this.initMultipleValueColumnFilter();
        } else {
            this.initSingleValueColumnFilter();
        }
    }

    initSingleValueColumnFilter(): void {
        this.params.api.forEachNode((rowNode, i) => {
            let columnValue = this.getNodeValue(rowNode);
            if (!this.dropdownValues.includes(columnValue)) {
                this.dropdownValues.push(columnValue);
            }
        });

        // Initialize the checked state for each option.
        this.dropdownValues.forEach((element) => {
            this.state.filterOptions[element] = true;
        });
    }

    initMultipleValueColumnFilter(): void {
        this.state.selectAll = false;
        this.state.deselectAll = true;

        this.params.api.forEachNode((rowNode, i) => {
            let columnValue = this.getNodeValue(rowNode);
            if (!Array.isArray(columnValue)) {
                throw "Value getter for multiple column filter needs to return an array";
            }
            columnValue.forEach((x) => {
                if (!this.dropdownValues.includes(x)) {
                    this.dropdownValues.push(x);
                }
            });
        });

        // Initialize the unchecked state for multiple value columns
        this.dropdownValues.forEach((element) => {
            this.state.filterOptions[element] = false;
        });
    }

    // If the filter is NOT active, the filter will pass for every row.
    // the filter paradigm for a multiple value column
    isFilterActive(): boolean {
        return this.columnContainsMultipleValues ? !this.state.deselectAll : !this.state.selectAll;
    }

    doesFilterPass(filterParams: IDoesFilterPassParams): boolean {
        if (this.columnContainsMultipleValues) {
            return this.doesfilterPassMultipleValues(filterParams);
        } else {
            return this.doesFilterPassSingleValue(filterParams);
        }
    }

    doesfilterPassMultipleValues(filterParams: IDoesFilterPassParams): boolean {
        let valueArray = this.getNodeValue(filterParams.node as RowNode<any>);
        let filterPasses;

        // if strict we need to compare all true filter options and see if the valueArray contains them all
        if (this.state.strict) {
            let checkedOptions = [];
            for (const [key, value] of Object.entries(this.state.filterOptions)) {
                if (value) {
                    checkedOptions.push(key);
                }
            }
            filterPasses = checkedOptions.every((name) => valueArray.includes(name));
        } else {
            // if not strict we can just see if the valueArray contains at least one of the selected filter options
            filterPasses = valueArray.some((name) => this.state.filterOptions[name]);
        }

        return filterPasses;
    }

    doesFilterPassSingleValue(filterParams: IDoesFilterPassParams): boolean {
        let value = this.getNodeValue(filterParams.node as RowNode<any>);
        if (this.state.filterOptions[value] == null) {
            return false;
        }
        return this.state.filterOptions[value] ? true : false;
    }

    private getNodeValue(rowNode: RowNode) {
        // NPT-984: honor `filterValueGetter` first so column defs can surface a different
        // dropdown value than the raw cell value — specifically, booleans configured by
        // `createBooleanColumnDef` need the filter chips to read "Yes"/"No" rather than
        // true/false. The previous version skipped `filterValueGetter` entirely and the
        // dropdown always reflected the raw field value, defeating the createBooleanColumnDef
        // fix on every column with UseCustomDropdownFilter: true.
        const colDef: any = this.params.colDef;
        if (typeof colDef.filterValueGetter === "function") {
            return colDef.filterValueGetter({
                data: rowNode.data,
                node: rowNode,
                colDef,
                column: this.params.column,
                api: this.params.api,
                context: this.params.context,
                getValue: (field: string) => rowNode.data?.[field],
            });
        }

        if (this.field) {
            return this.getPropertyValue(rowNode.data, this.field, "");
        } else if (colDef.valueGetter) {
            return colDef.valueGetter(rowNode);
        }

        return this.getPropertyValue(rowNode.data, this.field, "");
    }

    private getPropertyValue(object, path, defaultValue) {
        return path.split(".").reduce((o, p) => (o ? o[p] : defaultValue), object);
    }

    getModel() {
        return { filtersActive: this.state };
    }

    // one place this gets called is when the clear-grid-filters-button component clears the filter model
    setModel(model: any) {
        if (model === null) {
            // when we reset the model, for a multiple values filter we need to deselect all instead of selecting all.
            this.columnContainsMultipleValues ? this.onDeselectAll() : this.onSelectAll();
        } else {
            this.state = model.filtersActive;
        }
    }

    getDropdownValues() {
        // sort numbers numerically (not as strings). Check the first and last item of the array because there can be at most one "null" element
        if(this.dropdownValues.length > 0 && (typeof(this.dropdownValues[0]) == 'number' || typeof(this.dropdownValues[this.dropdownValues.length - 1]) == 'number')){
            return this.dropdownValues.sort(function(a, b) { 
                if(a != null && b != null){
                    return a - b; 
                }
                // sort the null/blank item first
                if(a == null){
                    return -1
                }
                return 1
            });
        }
        return this.dropdownValues.sort();
    }

    updateFilter() {
        this.state.selectAll = true;
        this.state.deselectAll = true;
        for (let element of this.dropdownValues) {
            if (this.state.filterOptions[element]) {
                this.state.deselectAll = false;
            } else {
                this.state.selectAll = false;
            }

            if (!this.state.selectAll && !this.state.deselectAll) {
                break;
            }
        }

        this.params.filterChangedCallback();
    }

    onSelectAll() {
        this.state.selectAll = true;
        this.state.deselectAll = false;

        this.updateFilterSelection();
    }

    onDeselectAll() {
        this.state.selectAll = false;
        this.state.deselectAll = true;

        this.updateFilterSelection();
    }

    onSelectStrict() {
        this.state.strict = true;
        this.params.filterChangedCallback();
    }

    onSelectLoose() {
        this.state.strict = false;
        this.params.filterChangedCallback();
    }

    private updateFilterSelection() {
        this.dropdownValues.forEach((element) => {
            this.state.filterOptions[element] = this.state.selectAll;
        });

        this.params.filterChangedCallback();
    }
}
