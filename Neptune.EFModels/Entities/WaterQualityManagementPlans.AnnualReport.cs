namespace Neptune.EFModels.Entities;

public static partial class WaterQualityManagementPlans
{
    public const int AnnualReportMinimumReportingYear = 2022;

    public static int GetCurrentReportingYear()
    {
        var now = DateTime.UtcNow;
        // WQMP fiscal year runs Jul 1 -> Jun 30; if we're in Jul-Dec the report period
        // ends next June, so we are still reporting the upcoming reporting year.
        return now.Month >= 7 ? now.Year + 1 : now.Year;
    }

    public static DateTime GetAnnualReportPeriodStart(int reportingYear) => new(reportingYear - 1, 7, 1);

    public static DateTime GetAnnualReportPeriodEnd(int reportingYear) =>
        new(reportingYear, 6, DateTime.DaysInMonth(reportingYear, 6));

    public static List<int> GetSelectableAnnualReportYears()
    {
        var current = GetCurrentReportingYear();
        return Enumerable.Range(AnnualReportMinimumReportingYear, (current - AnnualReportMinimumReportingYear) + 1)
            .OrderByDescending(x => x).ToList();
    }
}
