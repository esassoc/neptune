using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Neptune.EFModels.Entities;

[Keyless]
public partial class vMaintenanceRecordDetailed
{
    public int MaintenanceRecordID { get; set; }

    [StringLength(500)]
    [Unicode(false)]
    public string? MaintenanceRecordDescription { get; set; }

    public int? MaintenanceRecordTypeID { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string? MaintenanceRecordTypeDisplayName { get; set; }

    public int TreatmentBMPID { get; set; }

    public int TreatmentBMPTypeID { get; set; }

    [StringLength(200)]
    [Unicode(false)]
    public string? TreatmentBMPName { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime VisitDate { get; set; }

    public bool IsFieldVisitVerified { get; set; }

    public int FieldVisitID { get; set; }

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

    [Column("Structural Repair Conducted")]
    [StringLength(8000)]
    [Unicode(false)]
    public string Structural_Repair_Conducted { get; set; } = null!;

    [Column("Mechanical Repair Conducted")]
    [StringLength(8000)]
    [Unicode(false)]
    public string Mechanical_Repair_Conducted { get; set; } = null!;

    [Column("Infiltration Surface Restored")]
    [StringLength(8000)]
    [Unicode(false)]
    public string Infiltration_Surface_Restored { get; set; } = null!;

    [Column("Filtration Surface Restored")]
    [StringLength(8000)]
    [Unicode(false)]
    public string Filtration_Surface_Restored { get; set; } = null!;

    [Column("Media Replaced")]
    [StringLength(8000)]
    [Unicode(false)]
    public string Media_Replaced { get; set; } = null!;

    [Column("Mulch Added")]
    [StringLength(8000)]
    [Unicode(false)]
    public string Mulch_Added { get; set; } = null!;

    [Column("Percent Trash")]
    [StringLength(8000)]
    [Unicode(false)]
    public string Percent_Trash { get; set; } = null!;

    [Column("Percent Green Waste")]
    [StringLength(8000)]
    [Unicode(false)]
    public string Percent_Green_Waste { get; set; } = null!;

    [Column("Percent Sediment")]
    [StringLength(8000)]
    [Unicode(false)]
    public string Percent_Sediment { get; set; } = null!;

    [Column("Area Reseeded")]
    [StringLength(8000)]
    [Unicode(false)]
    public string Area_Reseeded { get; set; } = null!;

    [Column("Vegetation Planted")]
    [StringLength(8000)]
    [Unicode(false)]
    public string Vegetation_Planted { get; set; } = null!;

    [Column("Surface and Bank Erosion Repaired")]
    [StringLength(8000)]
    [Unicode(false)]
    public string Surface_and_Bank_Erosion_Repaired { get; set; } = null!;

    [Column("Total Material Volume Removed (cu-ft)")]
    [StringLength(8000)]
    [Unicode(false)]
    public string Total_Material_Volume_Removed__cu_ft_ { get; set; } = null!;

    [Column("Total Material Volume Removed (gal)")]
    [StringLength(8000)]
    [Unicode(false)]
    public string Total_Material_Volume_Removed__gal_ { get; set; } = null!;
}
