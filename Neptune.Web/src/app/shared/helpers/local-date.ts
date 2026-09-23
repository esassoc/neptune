/**
 * Today's local wall-clock date as `yyyy-MM-dd`, for `<input type="date">` defaults.
 * `new Date().toISOString()` returns UTC, which can show tomorrow's date for users
 * east of UTC late in the day (the same UTC-shift bug the visit sidebar surfaces).
 */
export function todayLocalDateString(): string {
    const d = new Date();
    const yyyy = d.getFullYear();
    const mm = String(d.getMonth() + 1).padStart(2, "0");
    const dd = String(d.getDate()).padStart(2, "0");
    return `${yyyy}-${mm}-${dd}`;
}
