using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Neptune.EFModels.Entities;

[Keyless]
public partial class vPowerBIWaterQualityManagementPlanOAndMVerification
{
    public int PrimaryKey { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string? WQMPName { get; set; }

    [StringLength(200)]
    [Unicode(false)]
    public string Jurisdiction { get; set; } = null!;

    public DateOnly VerificationDate { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime LastEditedDate { get; set; }

    [StringLength(201)]
    [Unicode(false)]
    public string? LastEditedBy { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string? TypeOfVerification { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string? VisitStatus { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string? VerificationStatus { get; set; }

    [StringLength(1000)]
    [Unicode(false)]
    public string? SourceControlCondition { get; set; }

    [StringLength(1000)]
    [Unicode(false)]
    public string? EnforcementOrFollowupActions { get; set; }

    [StringLength(9)]
    [Unicode(false)]
    public string DraftOrFinalized { get; set; } = null!;
}
