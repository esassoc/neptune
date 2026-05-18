using System.ComponentModel.DataAnnotations;

namespace Neptune.Models.DataTransferObjects;

public interface IHaveTreatmentBMPBasicInfo
{
    string? TreatmentBMPName { get; set; }
    int? OwnerOrganizationID { get; set; }
    int? YearBuilt { get; set; }
    string? SystemOfRecordID { get; set; }
    int? WaterQualityManagementPlanID { get; set; }
    int? TreatmentBMPLifespanTypeID { get; set; }
    DateTime? TreatmentBMPLifespanEndDate { get; set; }
    int SizingBasisTypeID { get; set; }
    int TrashCaptureStatusTypeID { get; set; }
    int? TrashCaptureEffectiveness { get; set; }
    int? RequiredFieldVisitsPerYear { get; set; }
    int? RequiredPostStormFieldVisitsPerYear { get; set; }
    string? Notes { get; set; }
}

public class TreatmentBMPCreateDto : IHaveTreatmentBMPBasicInfo
{
    [Required]
    public string? TreatmentBMPName { get; set; }

    [Required]
    public int TreatmentBMPTypeID { get; set; }

    [Required]
    public int StormwaterJurisdictionID { get; set; }

    public int? OwnerOrganizationID { get; set; }

    public int? YearBuilt { get; set; }

    public string? SystemOfRecordID { get; set; }

    public int? WaterQualityManagementPlanID { get; set; }

    public int? TreatmentBMPLifespanTypeID { get; set; }

    public DateTime? TreatmentBMPLifespanEndDate { get; set; }

    [Required]
    public int SizingBasisTypeID { get; set; }

    [Required]
    public int TrashCaptureStatusTypeID { get; set; }

    [Range(1, 99, ErrorMessage = "The Trash Effectiveness must be between 1 and 99, if the score is 100 please select Full")]
    public int? TrashCaptureEffectiveness { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Required Field Visits Per Year cannot be negative.")]
    public int? RequiredFieldVisitsPerYear { get; set; }

    [Range(0, Int32.MaxValue, ErrorMessage = "Required Post Storm Field Visits Per Year cannot be negative.")]
    public int? RequiredPostStormFieldVisitsPerYear { get; set; }

    [StringLength(2000)]
    public string? Notes { get; set; }

    [Required]
    public double? Latitude { get; set; }

    [Required]
    public double? Longitude { get; set; }
}

public class TreatmentBMPBasicInfoUpdateDto : IHaveTreatmentBMPBasicInfo
{
    [Required]
    public string? TreatmentBMPName { get; set; }

    public int? OwnerOrganizationID { get; set; }

    public int? YearBuilt { get; set; }

    public string? SystemOfRecordID { get; set; }

    public int? WaterQualityManagementPlanID { get; set; }

    public int? TreatmentBMPLifespanTypeID { get; set; }

    public DateTime? TreatmentBMPLifespanEndDate { get; set; }

    [Required]
    public int SizingBasisTypeID { get; set; }

    [Required]
    public int TrashCaptureStatusTypeID { get; set; }

    [Range(1, 99, ErrorMessage = "The Trash Effectiveness must be between 1 and 99, if the score is 100 please select Full")]
    public int? TrashCaptureEffectiveness { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Required Field Visits Per Year cannot be negative.")]
    public int? RequiredFieldVisitsPerYear { get; set; }

    [Range(0, Int32.MaxValue, ErrorMessage = "Required Post Storm Field Visits Per Year cannot be negative.")]
    public int? RequiredPostStormFieldVisitsPerYear { get; set; }

    [StringLength(2000)]
    public string? Notes { get; set; }
}

public class TreatmentBMPLocationUpdateDto
{
    [Required]
    public double? Latitude { get; set; }

    [Required]
    public double? Longitude { get; set; }
}

public class TreatmentBMPTypeUpdateDto
{
    [Required]
    public int TreatmentBMPTypeID { get; set; }
}

public class TreatmentBMPUpstreamBMPUpdateDto
{
    public int? UpstreamBMPID { get; set; }
}