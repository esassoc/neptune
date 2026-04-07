namespace Neptune.Models.DataTransferObjects;

public class SourceControlBMPDto
{
    public int SourceControlBMPID { get; set; }
    public int SourceControlBMPAttributeID { get; set; }
    public string? SourceControlBMPAttributeName { get; set; }
    public string? SourceControlBMPAttributeCategoryName { get; set; }
    public int SourceControlBMPAttributeCategoryID { get; set; }
    public bool? IsPresent { get; set; }
    public string? SourceControlBMPNote { get; set; }
}
