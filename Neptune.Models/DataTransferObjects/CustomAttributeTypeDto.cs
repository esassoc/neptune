namespace Neptune.Models.DataTransferObjects;

public class CustomAttributeTypeDto
{
    public int CustomAttributeTypeID { get; set; }
    public string CustomAttributeTypeName { get; set; }
    public int CustomAttributeDataTypeID { get; set; }
    public int? MeasurementUnitTypeID { get; set; }
    public bool IsRequired { get; set; }
    public string CustomAttributeTypeDescription { get; set; }
    public int CustomAttributeTypePurposeID { get; set; }
    public string CustomAttributeTypeOptionsSchema { get; set; }
    public string DataTypeName { get; set; }
    public string DataTypeDisplayName { get; set; }
    public string MeasurementUnitDisplayName { get; set; }
    public string Purpose { get; set; }
    public int? CustomAttributeTypeSortOrder { get; set; }
    public string CustomAttributeTypeDefaultValue { get; set; }
    public List<string> TreatmentBMPTypeNames { get; set; }
}

public class CustomAttributeTypeWithTreatmentBMPTypeIDsDto : CustomAttributeTypeDto
{
    public List<int> TreatmentBMPTypeIDs { get; set; }

}