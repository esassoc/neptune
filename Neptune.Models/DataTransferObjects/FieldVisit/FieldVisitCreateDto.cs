using System;

namespace Neptune.Models.DataTransferObjects;

public class FieldVisitCreateDto
{
    public int TreatmentBMPID { get; set; }
    public DateTime VisitDate { get; set; }
    public int FieldVisitTypeID { get; set; }

    /// <summary>
    /// When an in-progress visit already exists for the BMP, set Continue=true to resume it,
    /// Continue=false to discard the in-progress visit (mark Unresolved) and start fresh,
    /// or null when there is no existing in-progress visit.
    /// </summary>
    public bool? ContinueExistingInProgress { get; set; }
}
