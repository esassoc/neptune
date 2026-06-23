using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Neptune.EFModels.Entities;

[Keyless]
public partial class vTreatmentBMPDetailed
{
    public int PrimaryKey { get; set; }

    public int TreatmentBMPID { get; set; }

    [StringLength(200)]
    [Unicode(false)]
    public string? TreatmentBMPName { get; set; }

    public int TreatmentBMPTypeID { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string? TreatmentBMPTypeName { get; set; }

    public int StormwaterJurisdictionID { get; set; }

    [StringLength(200)]
    [Unicode(false)]
    public string OrganizationName { get; set; } = null!;

    public int? WaterQualityManagementPlanID { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string? WaterQualityManagementPlanName { get; set; }

    public int? RequiredFieldVisitsPerYear { get; set; }

    public int? RequiredPostStormFieldVisitsPerYear { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? TreatmentBMPLifespanEndDate { get; set; }

    public int? YearBuilt { get; set; }

    [StringLength(2000)]
    [Unicode(false)]
    public string? Notes { get; set; }

    public bool InventoryIsVerified { get; set; }

    public int OwnerOrganizationID { get; set; }

    [StringLength(200)]
    [Unicode(false)]
    public string OwnerOrganizationName { get; set; } = null!;

    public int? TreatmentBMPLifespanTypeID { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string? TreatmentBMPLifespanTypeDisplayName { get; set; }

    public int TrashCaptureStatusTypeID { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string? TrashCaptureStatusTypeDisplayName { get; set; }

    public int SizingBasisTypeID { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string? SizingBasisTypeDisplayName { get; set; }

    public int? DelineationTypeID { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string? DelineationTypeDisplayName { get; set; }

    public long NumberOfAssessments { get; set; }

    public int? LatestTreatmentBMPAssessmentID { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? LatestAssessmentDate { get; set; }

    public double? LatestAssessmentScore { get; set; }

    public long NumberOfMaintenanceRecords { get; set; }

    public int? LatestMaintenanceRecordID { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? LatestMaintenanceDate { get; set; }

    public int? NumberOfBenchmarkAndThresholds { get; set; }

    public int NumberOfBenchmarkAndThresholdsEntered { get; set; }

    public int? TrashCaptureEffectiveness { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }
}
