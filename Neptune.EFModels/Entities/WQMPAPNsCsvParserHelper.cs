using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic.FileIO;
using Neptune.Common;
using Neptune.Common.GeoSpatial;
using NetTopologySuite.Geometries;

namespace Neptune.EFModels.Entities;

public static class WQMPAPNsCsvParserHelper
{
    public static List<WaterQualityManagementPlanBoundary> CSVUpload(NeptuneDbContext dbContext, Stream fileStream,
        int stormwaterJurisdictionID,
        out List<string> errorList, out List<string> missingApnList, out List<Geometry> oldBoundaries)
    {
        var streamReader = new StreamReader(fileStream);
        var parser = new TextFieldParser(streamReader);
        return ParseWqmpRowsFromCsv(dbContext, parser, stormwaterJurisdictionID, out errorList, out missingApnList, out oldBoundaries);
    }
    public static List<WaterQualityManagementPlanBoundary> ParseWqmpRowsFromCsv(NeptuneDbContext dbContext, 
        TextFieldParser parser, int stormwaterJurisdictionID, out List<string> errorList, out List<string> missingApnList, 
        out List<Geometry> oldBoundaries)
    {
        parser.SetDelimiters(",");
        errorList = new List<string>();
        missingApnList = new List<string>();
        oldBoundaries = new List<Geometry>();
        var wqmpBoundariesToUpload = new List<WaterQualityManagementPlanBoundary>();
        var fieldsDict = new Dictionary<string, int>();
        var wqmpsInCSV = new List<string>();

        var requiredFields = new List<string> { "WQMP", "APNs", "WQMP Boundary Notes" };

        try
        {
            var header = parser.ReadFields();
            fieldsDict = ValidateHeader(header, requiredFields, out errorList);
            if (errorList.Any())
            {
                return null;
            }
        }
        catch
        {
            errorList.Add("Unable to read file and/or parse header");
        }

        // if the fields don't match throw an exception
        var rowCount = 1;
        while (!parser.EndOfData)
        {
            var currentRow = parser.ReadFields();

            var currentWQMPBoundary = ParseRequiredFieldsAndCreateWQMPBoundaries(dbContext, currentRow, fieldsDict,
                rowCount, stormwaterJurisdictionID, out var currentErrorList, out var currentMissingApnList,
                out var oldBoundary, wqmpsInCSV);
            if (currentWQMPBoundary != null)
            {
                wqmpBoundariesToUpload.Add(currentWQMPBoundary);
            }

            errorList.AddRange(currentErrorList);
            missingApnList.AddRange(currentMissingApnList);
            if(oldBoundary != null)
            {
                oldBoundaries.Add(oldBoundary);
            }
            rowCount++;
        }

        return wqmpBoundariesToUpload;
    }

    private static Dictionary<string, int> ValidateHeader(string[] row, List<string> requiredFields, out List<string> errorList)
    {
        errorList = new List<string>();
        var fieldsDict = new Dictionary<string, int>();

        for (var fieldIndex = 0; fieldIndex < row.Length; fieldIndex++)
        {
            var temp = row[fieldIndex].Trim();
            if (!string.IsNullOrWhiteSpace(temp))
            {
                fieldsDict.Add(temp, fieldIndex);
            }
        }

        var headers = fieldsDict.Keys.ToList();
        var requiredFieldDifference = requiredFields.Except(headers).ToList();

        if (requiredFieldDifference.Any())
        {
            errorList.Add("One or more required headers have not been provided. Required Fields are: " +
                          string.Join(", ", requiredFieldDifference));
        }

        return fieldsDict;
    }

    private static WaterQualityManagementPlanBoundary ParseRequiredFieldsAndCreateWQMPBoundaries(NeptuneDbContext dbContext, string[] row,
            Dictionary<string, int> fieldsDict, int rowNumber, int stormwaterJurisdictionID, 
            out List<string> errorList, out List<string> missingApnList, out Geometry? oldBoundary,
            List<string> wqmpList)
    {
        errorList = new List<string>();
        missingApnList = new List<string>();
        oldBoundary = null;

        var wqmpName = SetStringValue(row, fieldsDict, rowNumber, errorList, "WQMP", WaterQualityManagementPlan.FieldLengths.WaterQualityManagementPlanName, true);
        if (string.IsNullOrWhiteSpace(wqmpName))
        {
            // no point in going further if there is no wqmp name
            return null;
        }
        else if (wqmpList.Contains(wqmpName))
        {
            errorList.Add($"WQMP with name {wqmpName} is in csv multiple times.");
            return null;
        }
        wqmpList.Add(wqmpName);
        var wqmp = dbContext.WaterQualityManagementPlans.SingleOrDefault(x =>
            x.WaterQualityManagementPlanName == wqmpName && x.StormwaterJurisdictionID == stormwaterJurisdictionID);

        if (wqmp == null)
        {
            errorList.Add($"The WQMP with name '{wqmpName}' in row {rowNumber} was not found.");
            return null;
        }
        var wqmpBoundary = dbContext.WaterQualityManagementPlanBoundaries
            .Include(
                waterQualityManagementPlanBoundary => waterQualityManagementPlanBoundary.WaterQualityManagementPlan)
            .SingleOrDefault(x => x.WaterQualityManagementPlanID == wqmp.WaterQualityManagementPlanID);
        if (wqmpBoundary == null)
        {
            wqmpBoundary = new WaterQualityManagementPlanBoundary() { WaterQualityManagementPlanID = wqmp.WaterQualityManagementPlanID };
        }

        var apns = GetStringListFieldValue(row, fieldsDict, rowNumber, errorList, "APNs", true);

        var parcels = dbContext.Parcels.Where(x => !string.IsNullOrEmpty(x.ParcelNumber) && apns.Contains(x.ParcelNumber)).ToList();

        if (parcels.Count != apns.Count)
        {
            var missingParcels = apns.Except(parcels.Select(x => x.ParcelNumber)).ToList();
            missingApnList.AddRange(missingParcels);
        }
        if(parcels.Count == 0)
        {
            // do not proceed if there are no parcels to get the geometry from
            return null;
        }
        var parcelIDs = parcels.Select(x => x.ParcelID).ToList();

        oldBoundary = wqmpBoundary.GeometryNative;
        var newBoundary = dbContext.ParcelGeometries.Where(x => parcelIDs.Contains(x.ParcelID)).Select(x => x.GeometryNative).ToList().UnionListGeometries();
        
        wqmpBoundary.GeometryNative = newBoundary;
        wqmpBoundary.Geometry4326 = newBoundary?.ProjectTo4326();


        var waterQualityManagementPlanParcels = WaterQualityManagementPlanParcels.ListByWaterQualityManagementPlanIDWithChangeTracking(dbContext, wqmp.WaterQualityManagementPlanID);

        var newWaterQualityManagementPlanParcels = parcelIDs?.Select(x => new WaterQualityManagementPlanParcel
        {
            WaterQualityManagementPlanID = wqmp.WaterQualityManagementPlanID,
            ParcelID = x
        }).ToList() ?? new List<WaterQualityManagementPlanParcel>();

        waterQualityManagementPlanParcels.Merge(
            newWaterQualityManagementPlanParcels,
            dbContext.WaterQualityManagementPlanParcels,
            (x, y) => x.WaterQualityManagementPlanID == y.WaterQualityManagementPlanID && x.ParcelID == y.ParcelID);

        var wqmpBoundaryNotes = SetStringValue(row, fieldsDict, rowNumber, errorList, "WQMP Boundary Notes", 500, false);

        wqmp.WaterQualityManagementPlanBoundaryNotes = wqmpBoundaryNotes;

        return wqmpBoundary;
    }

    private static string SetStringValue(string[] row, Dictionary<string, int> fieldsDict, int rowNumber, List<string> errorList, string fieldName,
            int fieldLength, bool requireNotEmpty)
    {
        if (fieldsDict.TryGetValue(fieldName, out var value))
        {
            var fieldValue = row[value];
            if (!string.IsNullOrWhiteSpace(fieldValue))
            {
                if (fieldValue.Length > fieldLength)
                {
                    errorList.Add($"{fieldName} is too long at row: {rowNumber}. It must be {fieldLength} characters or less. Current Length is {fieldValue.Length}.");
                }
                else
                {
                    return fieldValue;
                }
            }
            else
            {
                if (requireNotEmpty)
                {
                    errorList.Add($"{fieldName} is null, empty, or just whitespaces for row: {rowNumber}");
                }
            }
        }
        return null;
    }

    private static List<string> GetStringListFieldValue(string[] row, Dictionary<string, int> fieldsDict, int rowNumber, List<string> errorList, string fieldName, bool requireNotEmpty)
    {
        if (fieldsDict.TryGetValue(fieldName, out var value))
        {
            var fieldValue = row[value];
            if (!string.IsNullOrWhiteSpace(fieldValue))
            {
                return fieldValue.Split(',').ToList().Select(x => x.Trim()).ToList();
            }
            
            if (requireNotEmpty)
            {
                errorList.Add($"{fieldName} is null, empty, or just whitespaces for row: {rowNumber}");
            }
            
        }
        return [];
    }



}