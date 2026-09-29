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
        records.AddRange(await ListOnlandVisualTrashAssessmentsAsync(dbContext, stormwaterJurisdictionIDs));
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

    // An OVTA has no name of its own: it reads as its Area's name, or the draft name before an Area exists
    // (same fallback as OnlandVisualTrashAssessmentExtensionMethods.AsReviewAndFinalizeDto). Several OVTAs
    // share an Area, so the date is the datum that tells them apart.
    private static async Task<List<SearchRecordDto>> ListOnlandVisualTrashAssessmentsAsync(NeptuneDbContext dbContext, List<int> stormwaterJurisdictionIDs)
    {
        var rows = await dbContext.OnlandVisualTrashAssessments.AsNoTracking()
            .Where(x => stormwaterJurisdictionIDs.Contains(x.StormwaterJurisdictionID))
            .Select(x => new
            {
                x.OnlandVisualTrashAssessmentID,
                AreaName = x.OnlandVisualTrashAssessmentArea != null ? x.OnlandVisualTrashAssessmentArea.OnlandVisualTrashAssessmentAreaName : x.DraftAreaName,
                x.OnlandVisualTrashAssessmentStatusID,
                x.CompletedDate,
                x.CreatedDate
            })
            .ToListAsync();

        return rows
            .Where(x => !string.IsNullOrWhiteSpace(x.AreaName))
            .OrderBy(x => x.AreaName).ThenByDescending(x => x.CompletedDate ?? DateOnly.FromDateTime(x.CreatedDate))
            .Select(x => new SearchRecordDto
            {
                Scope = SearchRecordDto.OnlandVisualTrashAssessmentScope,
                ID = x.OnlandVisualTrashAssessmentID,
                Title = x.AreaName!,
                Subtitle = OnlandVisualTrashAssessmentStatus.AllLookupDictionary.TryGetValue(x.OnlandVisualTrashAssessmentStatusID, out var status)
                    ? status.OnlandVisualTrashAssessmentStatusDisplayName
                    : null,
                Meta = (x.CompletedDate ?? DateOnly.FromDateTime(x.CreatedDate)).ToString("MMM d, yyyy")
            }).ToList();
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
