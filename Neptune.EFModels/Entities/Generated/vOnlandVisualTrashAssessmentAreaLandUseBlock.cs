using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Neptune.EFModels.Entities;

[Keyless]
public partial class vOnlandVisualTrashAssessmentAreaLandUseBlock
{
    public long? PrimaryKey { get; set; }

    public int StormwaterJurisdictionID { get; set; }

    public int OnlandVisualTrashAssessmentAreaID { get; set; }

    public int LandUseBlockID { get; set; }

    public int? PriorityLandUseTypeID { get; set; }
}
