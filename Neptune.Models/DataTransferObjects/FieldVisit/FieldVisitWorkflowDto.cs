using System;
using System.Collections.Generic;

namespace Neptune.Models.DataTransferObjects;

public class FieldVisitWorkflowDto
{
    public int FieldVisitID { get; set; }
    public DateTime VisitDate { get; set; }
    public int FieldVisitTypeID { get; set; }
    public string? FieldVisitTypeDisplayName { get; set; }
    public int FieldVisitStatusID { get; set; }
    public string? FieldVisitStatusDisplayName { get; set; }
    public bool IsFieldVisitVerified { get; set; }
    public bool InventoryUpdated { get; set; }

    public int TreatmentBMPID { get; set; }
    public string? TreatmentBMPName { get; set; }
    public int TreatmentBMPTypeID { get; set; }
    public string? TreatmentBMPTypeName { get; set; }
    public int StormwaterJurisdictionID { get; set; }

    public int PerformedByPersonID { get; set; }
    public string? PerformedByPersonName { get; set; }

    public int? InitialAssessmentID { get; set; }
    public bool InitialAssessmentComplete { get; set; }
    public double? InitialAssessmentScore { get; set; }

    public int? PostMaintenanceAssessmentID { get; set; }
    public bool PostMaintenanceAssessmentComplete { get; set; }
    public double? PostMaintenanceAssessmentScore { get; set; }

    public int? MaintenanceRecordID { get; set; }

    public int NumberOfRequiredAttributes { get; set; }
    public int NumberRequiredAttributesEntered { get; set; }
    public bool RequiredAttributesEntered => NumberRequiredAttributesEntered >= NumberOfRequiredAttributes;
}
