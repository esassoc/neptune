using Microsoft.EntityFrameworkCore;
using Neptune.Models.DataTransferObjects.Search;

namespace Neptune.EFModels.Entities;

// Header record search index (NPT-1125). Every query is a projection-only read scoped to the caller's
// viewable jurisdictions; lookup-table names are resolved in memory from the generated dictionaries.
public static class SearchRecords
{
    public static async Task<List<SearchRecordDto>> ListForJurisdictionsAsync(NeptuneDbContext dbContext, List<int> stormwaterJurisdictionIDs)
    {
        var records = new List<SearchRecordDto>();
        records.AddRange(await ListTreatmentBMPsAsync(dbContext, stormwaterJurisdictionIDs));
        records.AddRange(await ListWaterQualityManagementPlansAsync(dbContext, stormwaterJurisdictionIDs));
        records.AddRange(await ListOnlandVisualTrashAssessmentAreasAsync(dbContext, stormwaterJurisdictionIDs));
        records.AddRange(await ListProjectsAsync(dbContext, stormwaterJurisdictionIDs));
        return records;
    }

    // Inventoried BMPs only; planning-module BMPs (ProjectID set) are reached through their Project
    private static async Task<List<SearchRecordDto>> ListTreatmentBMPsAsync(NeptuneDbContext dbContext, List<int> stormwaterJurisdictionIDs)
    {
        var rows = await dbContext.TreatmentBMPs.AsNoTracking()
            .Where(x => x.ProjectID == null
                        && stormwaterJurisdictionIDs.Contains(x.StormwaterJurisdictionID)
                        && x.TreatmentBMPName != null && x.TreatmentBMPName.Trim() != "")
            .OrderBy(x => x.TreatmentBMPName)
            .Select(x => new
            {
                x.TreatmentBMPID,
                x.TreatmentBMPName,
                x.TreatmentBMPType.TreatmentBMPTypeName,
                JurisdictionName = x.StormwaterJurisdiction.Organization.OrganizationShortName ?? x.StormwaterJurisdiction.Organization.OrganizationName
            })
            .ToListAsync();

        return rows.Select(x => new SearchRecordDto
        {
            Scope = SearchRecordDto.TreatmentBMPScope,
            ID = x.TreatmentBMPID,
            Title = x.TreatmentBMPName!,
            Subtitle = x.TreatmentBMPTypeName,
            Meta = x.JurisdictionName
        }).ToList();
    }

    private static async Task<List<SearchRecordDto>> ListWaterQualityManagementPlansAsync(NeptuneDbContext dbContext, List<int> stormwaterJurisdictionIDs)
    {
        var rows = await dbContext.WaterQualityManagementPlans.AsNoTracking()
            .Where(x => stormwaterJurisdictionIDs.Contains(x.StormwaterJurisdictionID)
                        && x.WaterQualityManagementPlanName != null && x.WaterQualityManagementPlanName.Trim() != "")
            .OrderBy(x => x.WaterQualityManagementPlanName)
            .Select(x => new
            {
                x.WaterQualityManagementPlanID,
                x.WaterQualityManagementPlanName,
                x.WaterQualityManagementPlanStatusID,
                JurisdictionName = x.StormwaterJurisdiction.Organization.OrganizationShortName ?? x.StormwaterJurisdiction.Organization.OrganizationName
            })
            .ToListAsync();

        return rows.Select(x => new SearchRecordDto
        {
            Scope = SearchRecordDto.WaterQualityManagementPlanScope,
            ID = x.WaterQualityManagementPlanID,
            Title = x.WaterQualityManagementPlanName!,
            Subtitle = x.WaterQualityManagementPlanStatusID.HasValue
                       && WaterQualityManagementPlanStatus.AllLookupDictionary.TryGetValue(x.WaterQualityManagementPlanStatusID.Value, out var status)
                ? status.WaterQualityManagementPlanStatusDisplayName
                : null,
            Meta = x.JurisdictionName
        }).ToList();
    }

    // The OVTA scope lists Assessment Areas, not individual assessments (NPT-1125 rework): one row per area,
    // opening the area detail page, which lists that area's assessments. Draft assessments with no area yet
    // aren't searchable; they stay reachable from the OVTA list.
    private static async Task<List<SearchRecordDto>> ListOnlandVisualTrashAssessmentAreasAsync(NeptuneDbContext dbContext, List<int> stormwaterJurisdictionIDs)
    {
        var rows = await dbContext.OnlandVisualTrashAssessmentAreas.AsNoTracking()
            .Where(x => stormwaterJurisdictionIDs.Contains(x.StormwaterJurisdictionID)
                        && x.OnlandVisualTrashAssessmentAreaName != null && x.OnlandVisualTrashAssessmentAreaName.Trim() != "")
            .OrderBy(x => x.OnlandVisualTrashAssessmentAreaName)
            .Select(x => new
            {
                x.OnlandVisualTrashAssessmentAreaID,
                x.OnlandVisualTrashAssessmentAreaName,
                x.OnlandVisualTrashAssessmentBaselineScoreID,
                x.OnlandVisualTrashAssessmentProgressScoreID,
                JurisdictionName = x.StormwaterJurisdiction.Organization.OrganizationShortName ?? x.StormwaterJurisdiction.Organization.OrganizationName
            })
            .ToListAsync();

        return rows.Select(x => new SearchRecordDto
        {
            Scope = SearchRecordDto.OnlandVisualTrashAssessmentScope,
            ID = x.OnlandVisualTrashAssessmentAreaID,
            Title = x.OnlandVisualTrashAssessmentAreaName!,
            Subtitle = DescribeAreaScores(x.OnlandVisualTrashAssessmentBaselineScoreID, x.OnlandVisualTrashAssessmentProgressScoreID),
            Meta = x.JurisdictionName
        }).ToList();
    }

    private static string DescribeAreaScores(int? baselineScoreID, int? progressScoreID)
    {
        var parts = new List<string>();
        if (baselineScoreID.HasValue && OnlandVisualTrashAssessmentScore.AllLookupDictionary.TryGetValue(baselineScoreID.Value, out var baseline))
        {
            parts.Add($"Baseline {baseline.OnlandVisualTrashAssessmentScoreDisplayName}");
        }
        if (progressScoreID.HasValue && OnlandVisualTrashAssessmentScore.AllLookupDictionary.TryGetValue(progressScoreID.Value, out var progress))
        {
            parts.Add($"Progress {progress.OnlandVisualTrashAssessmentScoreDisplayName}");
        }
        return parts.Count > 0 ? string.Join(" · ", parts) : "Not assessed";
    }

    private static async Task<List<SearchRecordDto>> ListProjectsAsync(NeptuneDbContext dbContext, List<int> stormwaterJurisdictionIDs)
    {
        var rows = await dbContext.Projects.AsNoTracking()
            .Where(x => stormwaterJurisdictionIDs.Contains(x.StormwaterJurisdictionID)
                        && x.ProjectName != null && x.ProjectName.Trim() != "")
            .OrderBy(x => x.ProjectName)
            .Select(x => new
            {
                x.ProjectID,
                x.ProjectName,
                x.ProjectStatusID,
                JurisdictionName = x.StormwaterJurisdiction.Organization.OrganizationShortName ?? x.StormwaterJurisdiction.Organization.OrganizationName
            })
            .ToListAsync();

        return rows.Select(x => new SearchRecordDto
        {
            Scope = SearchRecordDto.ProjectScope,
            ID = x.ProjectID,
            Title = x.ProjectName!,
            Subtitle = ProjectStatus.AllLookupDictionary.TryGetValue(x.ProjectStatusID, out var status)
                ? status.ProjectStatusDisplayName
                : null,
            Meta = x.JurisdictionName
        }).ToList();
    }
}
