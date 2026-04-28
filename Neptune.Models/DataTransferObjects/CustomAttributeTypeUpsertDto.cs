using System.ComponentModel.DataAnnotations;

namespace Neptune.Models.DataTransferObjects;

public class CustomAttributeTypeUpsertDto
{
    [Required]
    [MaxLength(100)]
    public string CustomAttributeTypeName { get; set; }
    [Required]
    public int CustomAttributeDataTypeID { get; set; }
    public int? MeasurementUnitTypeID { get; set; }
    [Required]
    public bool IsRequired { get; set; }
    [Required]
    public int CustomAttributeTypePurposeID { get; set; }
    [MaxLength(500)]
    public string CustomAttributeTypeDescription { get; set; }
    public string CustomAttributeTypeOptionsSchema { get; set; }
    [MaxLength(1000)]
    public string CustomAttributeTypeDefaultValue { get; set; }
}
