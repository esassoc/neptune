using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Neptune.EFModels.Entities;

[Table("WaterQualityManagementPlanExtractionResult")]
[Index("WaterQualityManagementPlanID", Name = "AK_WaterQualityManagementPlanExtractionResult_WaterQualityManagementPlanID", IsUnique = true)]
public partial class WaterQualityManagementPlanExtractionResult
{
    [Key]
    public int WaterQualityManagementPlanExtractionResultID { get; set; }

    public int WaterQualityManagementPlanID { get; set; }

    public int WaterQualityManagementPlanDocumentID { get; set; }

    public string ExtractionResultJson { get; set; } = null!;

    [Column(TypeName = "datetime")]
    public DateTime ExtractedAt { get; set; }

    public string? DraftOverlayJson { get; set; }

    public int? DraftUpdatedByPersonID { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? DraftUpdatedDate { get; set; }

    public int? ApprovedByPersonID { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? ApprovedDate { get; set; }

    [ForeignKey("ApprovedByPersonID")]
    [InverseProperty("WaterQualityManagementPlanExtractionResultApprovedByPeople")]
    public virtual Person? ApprovedByPerson { get; set; }

    [ForeignKey("DraftUpdatedByPersonID")]
    [InverseProperty("WaterQualityManagementPlanExtractionResultDraftUpdatedByPeople")]
    public virtual Person? DraftUpdatedByPerson { get; set; }

    [ForeignKey("WaterQualityManagementPlanID")]
    [InverseProperty("WaterQualityManagementPlanExtractionResult")]
    public virtual WaterQualityManagementPlan WaterQualityManagementPlan { get; set; } = null!;

    [ForeignKey("WaterQualityManagementPlanDocumentID")]
    [InverseProperty("WaterQualityManagementPlanExtractionResults")]
    public virtual WaterQualityManagementPlanDocument WaterQualityManagementPlanDocument { get; set; } = null!;
}
