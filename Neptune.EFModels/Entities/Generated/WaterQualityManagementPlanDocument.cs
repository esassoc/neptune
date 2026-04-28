using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Neptune.EFModels.Entities;

[Table("WaterQualityManagementPlanDocument")]
[Index("DisplayName", "WaterQualityManagementPlanID", Name = "AK_WaterQualityManagementPlanDocument_DisplayName_WaterQualityManagementPlanID", IsUnique = true)]
public partial class WaterQualityManagementPlanDocument
{
    [Key]
    public int WaterQualityManagementPlanDocumentID { get; set; }

    public int WaterQualityManagementPlanID { get; set; }

    public int FileResourceID { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string? DisplayName { get; set; }

    [StringLength(1000)]
    [Unicode(false)]
    public string? Description { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime UploadDate { get; set; }

    public int WaterQualityManagementPlanDocumentTypeID { get; set; }

    [StringLength(64)]
    public string? AnthropicFileID { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? AnthropicFileUploadedDate { get; set; }

    [ForeignKey("FileResourceID")]
    [InverseProperty("WaterQualityManagementPlanDocuments")]
    public virtual FileResource FileResource { get; set; } = null!;

    [ForeignKey("WaterQualityManagementPlanID")]
    [InverseProperty("WaterQualityManagementPlanDocuments")]
    public virtual WaterQualityManagementPlan WaterQualityManagementPlan { get; set; } = null!;

    [InverseProperty("WaterQualityManagementPlanDocument")]
    public virtual ICollection<WaterQualityManagementPlanExtractionResult> WaterQualityManagementPlanExtractionResults { get; set; } = new List<WaterQualityManagementPlanExtractionResult>();
}
