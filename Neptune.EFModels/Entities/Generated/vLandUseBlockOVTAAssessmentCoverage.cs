using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Neptune.EFModels.Entities;

[Keyless]
public partial class vLandUseBlockOVTAAssessmentCoverage
{
    public int LandUseBlockID { get; set; }

    public int StormwaterJurisdictionID { get; set; }

    public int? PriorityLandUseTypeID { get; set; }

    [StringLength(80)]
    [Unicode(false)]
    public string? LandUseForTGR { get; set; }

    public int PermitTypeID { get; set; }

    public double AreaAssessedOnceOrMore { get; set; }

    public double AreaAssessedTwiceOrMore { get; set; }

    public double AreaAssessedThreeOrMore { get; set; }
}
