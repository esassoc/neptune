using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Neptune.EFModels.Entities;

[Table("Person")]
public partial class Person
{
    [Key]
    public int PersonID { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string? FirstName { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string? LastName { get; set; }

    [StringLength(255)]
    [Unicode(false)]
    public string? Email { get; set; }

    [StringLength(30)]
    [Unicode(false)]
    public string? Phone { get; set; }

    public int RoleID { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime CreateDate { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? UpdateDate { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? LastActivityDate { get; set; }

    public bool IsActive { get; set; }

    public int OrganizationID { get; set; }

    public bool ReceiveSupportEmails { get; set; }

    public bool ReceiveRSBRevisionRequestEmails { get; set; }

    public Guid? WebServiceAccessToken { get; set; }

    public bool IsOCTAGrantReviewer { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string? GlobalID { get; set; }

    public int? ImpersonatedPersonID { get; set; }

    [InverseProperty("Person")]
    public virtual ICollection<AITokenUsage> AITokenUsages { get; set; } = new List<AITokenUsage>();

    [InverseProperty("UploadedByPerson")]
    public virtual ICollection<DelineationStaging> DelineationStagings { get; set; } = new List<DelineationStaging>();

    [InverseProperty("VerifiedByPerson")]
    public virtual ICollection<Delineation> Delineations { get; set; } = new List<Delineation>();

    [InverseProperty("PerformedByPerson")]
    public virtual ICollection<FieldVisit> FieldVisits { get; set; } = new List<FieldVisit>();

    [InverseProperty("CreatePerson")]
    public virtual ICollection<FileResource> FileResources { get; set; } = new List<FileResource>();

    [ForeignKey("ImpersonatedPersonID")]
    [InverseProperty("InverseImpersonatedPerson")]
    public virtual Person? ImpersonatedPerson { get; set; }

    [InverseProperty("ImpersonatedPerson")]
    public virtual ICollection<Person> InverseImpersonatedPerson { get; set; } = new List<Person>();

    [InverseProperty("UploadedByPerson")]
    public virtual ICollection<LandUseBlockStaging> LandUseBlockStagings { get; set; } = new List<LandUseBlockStaging>();

    [InverseProperty("UpdatePerson")]
    public virtual ICollection<LandUseBlock> LandUseBlocks { get; set; } = new List<LandUseBlock>();

    [InverseProperty("Person")]
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    [InverseProperty("UploadedByPerson")]
    public virtual ICollection<OnlandVisualTrashAssessmentAreaStaging> OnlandVisualTrashAssessmentAreaStagings { get; set; } = new List<OnlandVisualTrashAssessmentAreaStaging>();

    [InverseProperty("CreatedByPerson")]
    public virtual ICollection<OnlandVisualTrashAssessment> OnlandVisualTrashAssessments { get; set; } = new List<OnlandVisualTrashAssessment>();

    [ForeignKey("OrganizationID")]
    [InverseProperty("People")]
    public virtual Organization Organization { get; set; } = null!;

    [InverseProperty("PrimaryContactPerson")]
    public virtual ICollection<Organization> Organizations { get; set; } = new List<Organization>();

    [InverseProperty("CreatePerson")]
    public virtual ICollection<Project> ProjectCreatePeople { get; set; } = new List<Project>();

    [InverseProperty("RequestedByPerson")]
    public virtual ICollection<ProjectNetworkSolveHistory> ProjectNetworkSolveHistories { get; set; } = new List<ProjectNetworkSolveHistory>();

    [InverseProperty("PrimaryContactPerson")]
    public virtual ICollection<Project> ProjectPrimaryContactPeople { get; set; } = new List<Project>();

    [InverseProperty("UpdatePerson")]
    public virtual ICollection<Project> ProjectUpdatePeople { get; set; } = new List<Project>();

    [InverseProperty("ClosedByPerson")]
    public virtual ICollection<RegionalSubbasinRevisionRequest> RegionalSubbasinRevisionRequestClosedByPeople { get; set; } = new List<RegionalSubbasinRevisionRequest>();

    [InverseProperty("RequestPerson")]
    public virtual ICollection<RegionalSubbasinRevisionRequest> RegionalSubbasinRevisionRequestRequestPeople { get; set; } = new List<RegionalSubbasinRevisionRequest>();

    [InverseProperty("Person")]
    public virtual ICollection<StormwaterJurisdictionPerson> StormwaterJurisdictionPeople { get; set; } = new List<StormwaterJurisdictionPerson>();

    [InverseProperty("RequestPerson")]
    public virtual ICollection<SupportRequestLog> SupportRequestLogs { get; set; } = new List<SupportRequestLog>();

    [InverseProperty("AdjustedByPerson")]
    public virtual ICollection<TrashGeneratingUnitAdjustment> TrashGeneratingUnitAdjustments { get; set; } = new List<TrashGeneratingUnitAdjustment>();

    [InverseProperty("InventoryVerifiedByPerson")]
    public virtual ICollection<TreatmentBMP> TreatmentBMPs { get; set; } = new List<TreatmentBMP>();

    [InverseProperty("LastEditedByPerson")]
    public virtual ICollection<WaterQualityManagementPlanVerify> WaterQualityManagementPlanVerifies { get; set; } = new List<WaterQualityManagementPlanVerify>();
}
