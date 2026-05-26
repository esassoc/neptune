import { Component, EventEmitter, Input, OnChanges, OnInit, Output, SimpleChanges } from "@angular/core";
import { NG_VALUE_ACCESSOR } from "@angular/forms";

import { IconComponent } from "src/app/shared/components/icon/icon.component";
@Component({
    selector: "btn-group-radio-input",
    templateUrl: "./btn-group-radio-input.component.html",
    styleUrls: ["./btn-group-radio-input.component.scss"],
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            useExisting: BtnGroupRadioInputComponent,
            multi: true,
        },
    ],
    imports: [IconComponent]
})
export class BtnGroupRadioInputComponent implements OnInit, OnChanges {
    public uniqueName: string = crypto.randomUUID();
    @Input() label: string;
    @Input() options: IBtnGroupRadioInputOption[] = [];
    @Output() change = new EventEmitter<string>();
    @Input() required: boolean = false;
    @Input() default: string;
    @Input() showIcons: boolean = false;

    public val: any;
    set value(val) {
        // this value is updated by programmatic changes if( val !== undefined && this.val !== val){
        this.val = val;
        this.change.emit(val);

        this.onChange(val);
        this.onTouch(val);
    }

    public isDisabled: boolean = false;

    onChange: any = () => {};
    onTouch: any = () => {};

    constructor() {}

    writeValue(value: any): void {
        this.val = value;
    }

    registerOnChange(fn: any): void {
        this.onChange = fn;
    }

    registerOnTouched(fn: any): void {
        this.onTouch = fn;
    }

    setDisabledState?(isDisabled: boolean): void {
        this.isDisabled = isDisabled;
    }

    ngOnInit(): void {
        this.applyDefault();
    }

    ngOnChanges(changes: SimpleChanges): void {
        // NPT-1056: react to `default` or `options` changing after the first render so the
        // button-group's selected state stays in sync with the host's reactive state (e.g., a
        // tab page that flips `activeTab` from a `?tab=...` query-param subscription firing
        // after ngOnInit). Without this, the tab content switched but the button-group
        // selection didn't.
        if (changes["default"] || changes["options"]) {
            this.applyDefault();
        }
    }

    private applyDefault(): void {
        // `default` matches against `label`. If a caller passes a value that no option's label
        // matches (e.g., during a stale render before options load), bail safely — the radio
        // group renders with no selection rather than throwing on undefined.value (NPT-984).
        if (!this.default) return;
        const match = this.options?.find((x) => x.label == this.default);
        if (match) this.val = match.value;
    }
}

export interface IBtnGroupRadioInputOption {
    label: string;
    value: any;
    disabled?: boolean;
}
