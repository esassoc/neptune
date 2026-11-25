using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace Neptune.EFModels.Entities;

[Table("TrashGeneratingUnitStaging")]
public partial class TrashGeneratingUnitStaging
{
    [Key]
    public int TrashGeneratingUnitStagingID { get; set; }

    public int StormwaterJurisdictionID { get; set; }

    public int? OnlandVisualTrashAssessmentAreaID { get; set; }

    public int? LandUseBlockID { get; set; }

    public int? DelineationID { get; set; }

    public int? WaterQualityManagementPlanID { get; set; }

    [Column(TypeName = "geometry")]
    public Geometry TrashGeneratingUnitGeometry { get; set; } = null!;

    [Column(TypeName = "geometry")]
    public Geometry TrashGeneratingUnitGeometry4326 { get; set; } = null!;

    [Column(TypeName = "datetime")]
    public DateTime LastUpdateDate { get; set; }
}
