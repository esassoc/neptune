import { SelectDropdownOption } from "src/app/shared/components/forms/form-field/form-field.component";

/**
 * Canonical list of US state / territory options for dropdown inputs.
 * Labels are full names for display; values are 2-letter USPS abbreviations
 * for storage / API round-trip.
 */
const STATE_TUPLES: [string, string][] = [
    ["Alabama", "AL"], ["Alaska", "AK"], ["Arizona", "AZ"], ["Arkansas", "AR"],
    ["California", "CA"], ["Colorado", "CO"], ["Connecticut", "CT"], ["Delaware", "DE"],
    ["District of Columbia", "DC"], ["Florida", "FL"], ["Georgia", "GA"], ["Hawaii", "HI"],
    ["Idaho", "ID"], ["Illinois", "IL"], ["Indiana", "IN"], ["Iowa", "IA"],
    ["Kansas", "KS"], ["Kentucky", "KY"], ["Louisiana", "LA"], ["Maine", "ME"],
    ["Maryland", "MD"], ["Massachusetts", "MA"], ["Michigan", "MI"], ["Minnesota", "MN"],
    ["Mississippi", "MS"], ["Missouri", "MO"], ["Montana", "MT"], ["Nebraska", "NE"],
    ["Nevada", "NV"], ["New Hampshire", "NH"], ["New Jersey", "NJ"], ["New Mexico", "NM"],
    ["New York", "NY"], ["North Carolina", "NC"], ["North Dakota", "ND"], ["Ohio", "OH"],
    ["Oklahoma", "OK"], ["Oregon", "OR"], ["Pennsylvania", "PA"], ["Puerto Rico", "PR"],
    ["Rhode Island", "RI"], ["South Carolina", "SC"], ["South Dakota", "SD"], ["Tennessee", "TN"],
    ["Texas", "TX"], ["Utah", "UT"], ["Vermont", "VT"], ["Virginia", "VA"],
    ["Washington", "WA"], ["West Virginia", "WV"], ["Wisconsin", "WI"], ["Wyoming", "WY"],
];

export const US_STATES: SelectDropdownOption[] = STATE_TUPLES.map(
    ([Label, Value]) => ({ Label, Value, disabled: false })
);
