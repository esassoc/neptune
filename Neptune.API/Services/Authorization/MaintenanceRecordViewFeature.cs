namespace Neptune.API.Services.Authorization
{
    /// <summary>
    /// Read gate for maintenance record-routed endpoints. Reads need the same check as edits: a staff role plus a
    /// jurisdiction match against the routed maintenance record's BMP. Unassigned (self sign-up) is refused.
    /// </summary>
    public class MaintenanceRecordViewFeature : MaintenanceRecordEditFeature;
}
