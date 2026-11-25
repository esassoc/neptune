using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Neptune.EFModels.Entities;

[Keyless]
public partial class vWaterQualityManagementPlanBoundaryOverlap
{
    public long? PrimaryKey { get; set; }

    public int WaterQualityManagementPlanID { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string? WaterQualityManagementPlanName { get; set; }

    public int StormwaterJurisdictionID { get; set; }

    [StringLength(200)]
    [Unicode(false)]
    public string StormwaterJurisdictionName { get; set; } = null!;

    public int OverlappingWaterQualityManagementPlanID { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string? OverlappingWaterQualityManagementPlanName { get; set; }

    public int OverlappingStormwaterJurisdictionID { get; set; }

    [StringLength(200)]
    [Unicode(false)]
    public string OverlappingStormwaterJurisdictionName { get; set; } = null!;

    public double? OverlapAreaInSquareMeters { get; set; }
}
