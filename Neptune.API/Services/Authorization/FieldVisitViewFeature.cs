namespace Neptune.API.Services.Authorization
{
    /// <summary>
    /// Read gate for field visit-routed endpoints. Reads need the same check as edits: a staff role plus a
    /// jurisdiction match against the routed field visit's BMP. Unassigned (self sign-up) is refused.
    /// </summary>
    public class FieldVisitViewFeature : FieldVisitEditFeature;
}
