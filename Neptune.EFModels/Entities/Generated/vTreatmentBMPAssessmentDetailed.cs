using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Neptune.EFModels.Entities;

[Keyless]
public partial class vTreatmentBMPAssessmentDetailed
{
    public int TreatmentBMPAssessmentID { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string? TreatmentBMPAssessmentTypeDisplayName { get; set; }

    public bool IsAssessmentComplete { get; set; }

    public double? AssessmentScore { get; set; }

    public int TreatmentBMPID { get; set; }

    [StringLength(200)]
    [Unicode(false)]
    public string? TreatmentBMPName { get; set; }

    public int TreatmentBMPTypeID { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string? TreatmentBMPTypeName { get; set; }

    public int FieldVisitID { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string? FieldVisitTypeDisplayName { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime VisitDate { get; set; }

    public bool IsFieldVisitVerified { get; set; }

    public int PerformedByPersonID { get; set; }

    [StringLength(201)]
    [Unicode(false)]
    public string? PerformedByPersonName { get; set; }

    public int StormwaterJurisdictionID { get; set; }

    [StringLength(200)]
    [Unicode(false)]
    public string StormwaterJurisdictionName { get; set; } = null!;

    public int StormwaterJurisdictionPublicWQMPVisibilityTypeID { get; set; }

    public int? WaterQualityManagementPlanID { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string? WaterQualityManagementPlanName { get; set; }
}
