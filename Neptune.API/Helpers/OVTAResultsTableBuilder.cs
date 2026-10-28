using System;
using System.Collections.Generic;
using System.Linq;
using Neptune.EFModels.Entities;
using Neptune.Models.DataTransferObjects;

namespace Neptune.API.Helpers;

// NPT-1126: builds the OVTA-Based Results table rows (one per land use type) from two inputs:
// Phase I MS4 Land Use Block acres (the "Total Mapped Land Use" denominator) and Phase I MS4 TGU acres
// attributed to an OVTA Area's scores and completed assessment count. Kept free of the DbContext so the
// bucketing can be unit tested.
public static class OVTAResultsTableBuilder
{
    public const string UnspecifiedLandUseTypeName = "Unspecified";

    public record LandUseAcres(int? PriorityLandUseTypeID, string LandUseForTGR, double Acres);

    public record TrashGeneratingUnitAcres(int? PriorityLandUseTypeID, string LandUseForTGR, int? BaselineScoreID, int? ProgressScoreID,
        int CompletedAssessmentCount, double Acres);

    public static List<OVTAResultsLandUseRowDto> BuildRows(IEnumerable<LandUseAcres> landUseAcres, IEnumerable<TrashGeneratingUnitAcres> trashGeneratingUnitAcres)
    {
        var rowsByKey = new Dictionary<(bool IsPriorityLandUse, string LandUseTypeName), OVTAResultsLandUseRowDto>();

        OVTAResultsLandUseRowDto GetOrAddRow(int? priorityLandUseTypeID, string landUseForTGR)
        {
            var key = GetRowKey(priorityLandUseTypeID, landUseForTGR);
            if (!rowsByKey.TryGetValue(key, out var row))
            {
                row = new OVTAResultsLandUseRowDto { IsPriorityLandUse = key.IsPriorityLandUse, LandUseTypeName = key.LandUseTypeName };
                rowsByKey.Add(key, row);
            }
            return row;
        }

        // Every land use type in the jurisdiction's blocks gets a row, including types with no assessed area.
        foreach (var landUse in landUseAcres)
        {
            GetOrAddRow(landUse.PriorityLandUseTypeID, landUse.LandUseForTGR).TotalMappedAcres += landUse.Acres;
        }

        foreach (var tgu in trashGeneratingUnitAcres)
        {
            var row = GetOrAddRow(tgu.PriorityLandUseTypeID, tgu.LandUseForTGR);
            AddScoreAcres(row, tgu.BaselineScoreID, tgu.Acres, isProgress: false);
            AddScoreAcres(row, tgu.ProgressScoreID, tgu.Acres, isProgress: true);
            switch (tgu.CompletedAssessmentCount)
            {
                case <= 0:
                    break;
                case 1:
                    row.AssessedOnceAcres += tgu.Acres;
                    break;
                case 2:
                    row.AssessedTwiceAcres += tgu.Acres;
                    break;
                default:
                    row.AssessedThreeOrMoreAcres += tgu.Acres;
                    break;
            }
        }

        foreach (var row in rowsByKey.Values)
        {
            // TGU and Land Use Block geometry can differ slightly, so clamp rather than show a negative area.
            // The caller logs rows where the assessed area exceeds the mapped total.
            row.NotYetAssessedAcres = Math.Max(0, row.TotalMappedAcres - AssessedAcres(row));
        }

        var rows = rowsByKey.Values
            .OrderByDescending(x => x.IsPriorityLandUse)
            .ThenByDescending(x => x.TotalMappedAcres)
            .ThenBy(x => x.LandUseTypeName, StringComparer.OrdinalIgnoreCase)
            .ToList();
        for (var i = 0; i < rows.Count; i++)
        {
            rows[i].SortOrder = i + 1;
        }
        return rows;
    }

    public static double AssessedAcres(OVTAResultsLandUseRowDto row)
    {
        return row.AssessedOnceAcres + row.AssessedTwiceAcres + row.AssessedThreeOrMoreAcres;
    }

    // PLU rows are keyed by Priority Land Use Type; ALU rows by the block's Land Use for TGR. A block with no
    // Priority Land Use Type counts as PLU, matching TrashGeneratingUnitHelper.IsPLU.
    private static (bool IsPriorityLandUse, string LandUseTypeName) GetRowKey(int? priorityLandUseTypeID, string landUseForTGR)
    {
        if (priorityLandUseTypeID == (int)PriorityLandUseTypeEnum.ALU)
        {
            var name = string.IsNullOrWhiteSpace(landUseForTGR) ? UnspecifiedLandUseTypeName : landUseForTGR.Trim();
            return (false, name);
        }

        var priorityLandUseTypeName = priorityLandUseTypeID.HasValue && PriorityLandUseType.AllLookupDictionary.TryGetValue(priorityLandUseTypeID.Value, out var priorityLandUseType)
            ? priorityLandUseType.PriorityLandUseTypeDisplayName
            : UnspecifiedLandUseTypeName;
        return (true, priorityLandUseTypeName);
    }

    private static void AddScoreAcres(OVTAResultsLandUseRowDto row, int? scoreID, double acres, bool isProgress)
    {
        switch (scoreID)
        {
            case (int)OnlandVisualTrashAssessmentScoreEnum.A:
                if (isProgress) row.ProgressAcresA += acres; else row.BaselineAcresA += acres;
                break;
            case (int)OnlandVisualTrashAssessmentScoreEnum.B:
                if (isProgress) row.ProgressAcresB += acres; else row.BaselineAcresB += acres;
                break;
            case (int)OnlandVisualTrashAssessmentScoreEnum.C:
                if (isProgress) row.ProgressAcresC += acres; else row.BaselineAcresC += acres;
                break;
            case (int)OnlandVisualTrashAssessmentScoreEnum.D:
                if (isProgress) row.ProgressAcresD += acres; else row.BaselineAcresD += acres;
                break;
        }
    }
}
