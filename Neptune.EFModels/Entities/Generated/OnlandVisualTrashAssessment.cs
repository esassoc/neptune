using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace Neptune.EFModels.Entities;

[Table("OnlandVisualTrashAssessment")]
[Index("DraftGeometry", Name = "SPATIAL_OnlandVisualTrashAssessment_DraftGeometry")]
public partial class OnlandVisualTrashAssessment
{
    [Key]
    public int OnlandVisualTrashAssessmentID { get; set; }

    public int CreatedByPersonID { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime CreatedDate { get; set; }

    public int? OnlandVisualTrashAssessmentAreaID { get; set; }

    [StringLength(500)]
    [Unicode(false)]
    public string? Notes { get; set; }

    public int StormwaterJurisdictionID { get; set; }

    public bool? AssessingNewArea { get; set; }

    public int OnlandVisualTrashAssessmentStatusID { get; set; }

    [Column(TypeName = "geometry")]
    public Geometry? DraftGeometry { get; set; }

    public bool? IsDraftGeometryManuallyRefined { get; set; }

    public int? OnlandVisualTrashAssessmentScoreID { get; set; }

    public DateOnly? CompletedDate { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string? DraftAreaName { get; set; }

    [StringLength(500)]
    [Unicode(false)]
    public string? DraftAreaDescription { get; set; }

    public bool IsTransectBackingAssessment { get; set; }

    public bool IsProgressAssessment { get; set; }

    [StringLength(200)]
    [Unicode(false)]
    public string? SecondAssessorName { get; set; }

    public int? OvtaAreaSourceTypeID { get; set; }

    [ForeignKey("CreatedByPersonID")]
    [InverseProperty("OnlandVisualTrashAssessments")]
    public virtual Person CreatedByPerson { get; set; } = null!;

    [ForeignKey("OnlandVisualTrashAssessmentAreaID")]
    [InverseProperty("OnlandVisualTrashAssessments")]
    public virtual OnlandVisualTrashAssessmentArea? OnlandVisualTrashAssessmentArea { get; set; }

    [InverseProperty("OnlandVisualTrashAssessment")]
    public virtual ICollection<OnlandVisualTrashAssessmentObservationPhotoStaging> OnlandVisualTrashAssessmentObservationPhotoStagings { get; set; } = new List<OnlandVisualTrashAssessmentObservationPhotoStaging>();

    [InverseProperty("OnlandVisualTrashAssessment")]
    public virtual ICollection<OnlandVisualTrashAssessmentObservation> OnlandVisualTrashAssessmentObservations { get; set; } = new List<OnlandVisualTrashAssessmentObservation>();

    [InverseProperty("OnlandVisualTrashAssessment")]
    public virtual ICollection<OnlandVisualTrashAssessmentPreliminarySourceIdentificationType> OnlandVisualTrashAssessmentPreliminarySourceIdentificationTypes { get; set; } = new List<OnlandVisualTrashAssessmentPreliminarySourceIdentificationType>();

    [ForeignKey("StormwaterJurisdictionID")]
    [InverseProperty("OnlandVisualTrashAssessments")]
    public virtual StormwaterJurisdiction StormwaterJurisdiction { get; set; } = null!;
}
