namespace Neptune.API.Services.Authorization
{
    /// <summary>
    /// Read gate for assessment-routed endpoints. Reads need the same check as edits: a staff role plus a
    /// jurisdiction match against the routed assessment's BMP. Unassigned (self sign-up) is refused.
    /// </summary>
    public class TreatmentBMPAssessmentViewFeature : TreatmentBMPAssessmentEditFeature;
}
