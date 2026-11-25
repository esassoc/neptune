using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace Neptune.EFModels.Entities;

[Table("LoadGeneratingUnitStaging")]
public partial class LoadGeneratingUnitStaging
{
    [Key]
    public int LoadGeneratingUnitStagingID { get; set; }

    public int? ModelBasinID { get; set; }

    public int? RegionalSubbasinID { get; set; }

    public int? DelineationID { get; set; }

    public int? WaterQualityManagementPlanID { get; set; }

    [Column(TypeName = "geometry")]
    public Geometry LoadGeneratingUnitGeometry { get; set; } = null!;

    [Column(TypeName = "geometry")]
    public Geometry LoadGeneratingUnitGeometry4326 { get; set; } = null!;
}
