namespace Neptune.Models.DataTransferObjects;

public class WaterQualityManagementPlanAnnualReportOptionsDto
{
    public List<ReportingYearSimpleDto> ReportingYears { get; set; } = new();
    public List<StormwaterJurisdictionDisplayDto> StormwaterJurisdictions { get; set; } = new();
    public int DefaultReportingYear { get; set; }
}
