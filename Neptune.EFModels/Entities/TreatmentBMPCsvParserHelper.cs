using Microsoft.VisualBasic.FileIO;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Neptune.Common.GeoSpatial;
using NetTopologySuite.Geometries;
using Neptune.Models.DataTransferObjects;

namespace Neptune.EFModels.Entities
{
    public static class TreatmentBMPCsvParserHelper
    {
        // Inlined from the legacy Neptune.WebMvc.Common.Models.ModelObjectHelpers so this helper
        // can live in Neptune.EFModels without depending on the WebMvc project.
        private static bool IsRealPrimaryKeyValue(int? primaryKeyValueToCheck)
        {
            return primaryKeyValueToCheck.HasValue && primaryKeyValueToCheck.Value > 0;
        }

        public static List<TreatmentBMP> CSVUpload(NeptuneDbContext dbContext, Stream fileStream, TreatmentBMPType treatmentBMPType,
            out List<string> errorList, out List<CustomAttribute> customAttributes,
            out List<CustomAttributeValue> customAttributeValues)
        {
            var streamReader = new StreamReader(fileStream);
            var parser = new TextFieldParser(streamReader);
            return ParseBmpRowsFromCsv(dbContext, parser, out errorList, out customAttributes, out customAttributeValues, treatmentBMPType);
        }

        public static List<TreatmentBMP> CSVUpload(NeptuneDbContext dbContext, string fileStream, int treatmentBMPTypeID, out List<string> errorList, out List<CustomAttribute> customAttributes, out List<CustomAttributeValue> customAttributeValues)
        {
            var stringReader = new StringReader(fileStream);
            var parser = new TextFieldParser(stringReader);
            var treatmentBMPType = TreatmentBMPTypes.GetByID(dbContext, treatmentBMPTypeID);
            return ParseBmpRowsFromCsv(dbContext, parser, out errorList, out customAttributes, out customAttributeValues, treatmentBMPType);
        }

        public static List<TreatmentBMP> ParseBmpRowsFromCsv(NeptuneDbContext dbContext, TextFieldParser parser,
            out List<string> errorList, out List<CustomAttribute> customAttributes,
            out List<CustomAttributeValue> customAttributeValues, TreatmentBMPType treatmentBMPType)
        {
            parser.SetDelimiters(",");
            errorList = new List<string>();
            customAttributes = new List<CustomAttribute>();
            customAttributeValues = new List<CustomAttributeValue>();
            var treatmentBMPsToUpload = new List<TreatmentBMP>();
            var fieldsDict = new Dictionary<string, int>();

            var requiredFields = new List<string> { "Jurisdiction", "BMP Name", "Latitude", "Longitude", "Sizing Basis", "Trash Capture Status", "Owner" };
            var optionalFields = new List<string> {"Year Built or Installed","Asset ID in System of Record", "Required Lifespan of Installation",
                "Allowable End Date of Installation (if applicable)", "Required Field Visits Per Year", "Required Post-Storm Field Visits Per Year","Notes"};
            var customAttributeTypes = treatmentBMPType.TreatmentBMPTypeCustomAttributeTypes.Select(x => x.CustomAttributeType).ToList();

            var headerFieldCount = 0;
            try
            {
                var header = parser.ReadFields();
                headerFieldCount = header.Length;
                var customAttributeNames = customAttributeTypes.Select(x => x.CustomAttributeTypeName).ToList();
                fieldsDict = ValidateHeader(header, requiredFields, optionalFields, customAttributeNames, out errorList, treatmentBMPType);
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
            var organizations = dbContext.Organizations.AsNoTracking().ToList();
            var stormwaterJurisdictions = StormwaterJurisdictions.List(dbContext).ToList();
            var treatmentBMPNamesInCsv = new List<string>();
            while (!parser.EndOfData)
            {
                var currentRow = parser.ReadFields();

                // More fields than the header means an unquoted cell contained commas and was split into
                // extra columns (e.g. a MultiSelect value typed as Gravel, InvalidMedia without quotes).
                // A trailing comma yields a harmless empty extra field; only reject when an extra field
                // carries real data, which would otherwise be silently dropped — hiding the invalid entry
                // and letting the row commit. Tell the user to quote multi-value cells.
                if (currentRow.Length > headerFieldCount &&
                    currentRow.Skip(headerFieldCount).Any(x => !string.IsNullOrWhiteSpace(x)))
                {
                    errorList.Add(
                        $"Row {rowCount} has more columns ({currentRow.Length}) than the header ({headerFieldCount}). If a value contains commas (e.g. a multi-select entry like \"Gravel, Sand\"), wrap it in double quotes so it is not read as additional columns. Row: {rowCount}");
                    rowCount++;
                    continue;
                }

                var currentTreatmentBMP = ParseRequiredAndOptionalFieldAndCreateBMP(dbContext, currentRow, fieldsDict, rowCount, out var currentErrorList, treatmentBMPType, organizations, stormwaterJurisdictions, treatmentBMPNamesInCsv);
                if (currentTreatmentBMP != null)
                {
                    treatmentBMPsToUpload.Add(currentTreatmentBMP);
                    errorList.AddRange(currentErrorList);

                    customAttributes.AddRange(ParseCustomAttributes(dbContext, currentTreatmentBMP, treatmentBMPType, currentRow, fieldsDict,
                        customAttributeTypes, rowCount, out currentErrorList,
                        out var currentCustomAttributeValues));
                    customAttributeValues.AddRange(currentCustomAttributeValues);
                }

                errorList.AddRange(currentErrorList);
                rowCount++;
            }

            return treatmentBMPsToUpload;
        }

        private static TreatmentBMP ParseRequiredAndOptionalFieldAndCreateBMP(NeptuneDbContext dbContext,
            string[] row,
            Dictionary<string, int> fieldsDict, int rowNumber, out List<string> errorList,
            TreatmentBMPType treatmentBMPType, List<Organization> organizations,
            List<StormwaterJurisdiction> stormwaterJurisdictions, List<string> treatmentBMPNamesInCsv)
        {
            errorList = new List<string>();

            var treatmentBMPName = SetStringValue(row, fieldsDict, rowNumber, errorList, "BMP Name", TreatmentBMP.FieldLengths.TreatmentBMPName, true);
            var stormwaterJurisdictionID = FindLookupValue(row, fieldsDict, "Jurisdiction", rowNumber, errorList, stormwaterJurisdictions, x => x.Organization.OrganizationName, x => x.StormwaterJurisdictionID, false, true);

            if (!stormwaterJurisdictionID.HasValue || string.IsNullOrWhiteSpace(treatmentBMPName))
            {
                // no point in going further if we don't have a name and jurisdiction
                return null;
            }

            if (!string.IsNullOrWhiteSpace(treatmentBMPName))
            {
                if (treatmentBMPNamesInCsv.Contains(treatmentBMPName))
                {
                    errorList.Add(
                        $"The BMP with Name '{treatmentBMPName}' was already added in this upload, duplicate name is found at row: {rowNumber}");
                }
                treatmentBMPNamesInCsv.Add(treatmentBMPName);
            }

            // Fetch every existing BMP sharing this name once, then derive both checks in memory
            // (one query per row instead of two).
            var existingBMPsWithName = dbContext.TreatmentBMPs.Include(x => x.TreatmentBMPType)
                .Where(x => x.TreatmentBMPName == treatmentBMPName)
                .ToList();

            // A BMP name must map to a single type. Reject if any existing BMP — in any jurisdiction,
            // including planning-module (project) BMPs — already uses this name with a different type.
            // The jurisdiction-scoped update lookup below can't catch this (the existing BMP may be in
            // another jurisdiction or be a project BMP), which is how a cross-type duplicate slipped through.
            var conflictingTypeBMP = existingBMPsWithName.FirstOrDefault(x =>
                x.TreatmentBMPTypeID != treatmentBMPType.TreatmentBMPTypeID);
            if (conflictingTypeBMP != null)
            {
                errorList.Add(
                    $"BMP with name '{treatmentBMPName}' has a Type '{conflictingTypeBMP.TreatmentBMPType.TreatmentBMPTypeName}', which does not match the uploaded Type '{treatmentBMPType.TreatmentBMPTypeName}' for row: {rowNumber}");
            }

            // Match an existing inventory BMP (ProjectID == null) in this jurisdiction as the update target.
            // Per the AK_TreatmentBMP_StormwaterJurisdictionID_TreatmentBMPName unique index, inventory names
            // are unique per jurisdiction and project BMPs are never upload targets.
            var treatmentBMP = existingBMPsWithName.SingleOrDefault(x =>
                x.StormwaterJurisdictionID == stormwaterJurisdictionID.Value &&
                x.ProjectID == null);
            if (treatmentBMP == null)
            {
                treatmentBMP = new TreatmentBMP()
                {
                    TreatmentBMPName = treatmentBMPName,
                    TreatmentBMPTypeID = treatmentBMPType.TreatmentBMPTypeID,
                    StormwaterJurisdictionID = stormwaterJurisdictionID.Value,
                    InventoryIsVerified = false
                };
            }

            var isNew = !IsRealPrimaryKeyValue(treatmentBMP.TreatmentBMPID);
            var treatmentBMPLatitude = row[fieldsDict["Latitude"]];
            var treatmentBMPLongitude = row[fieldsDict["Longitude"]];
            var locationPoint4326 = ParseLocation(treatmentBMPLatitude, treatmentBMPLongitude, rowNumber, errorList,
                isNew);
            if (locationPoint4326 != null)
            {
                treatmentBMP.LocationPoint4326 = locationPoint4326;
                var locationPoint = locationPoint4326.ProjectTo2771();
                treatmentBMP.LocationPoint = locationPoint;

                treatmentBMP.SetTreatmentBMPPointInPolygonDataByLocationPoint(locationPoint, dbContext);
            }

            var ownerOrganizationID = FindLookupValue(row, fieldsDict, "Owner", rowNumber, errorList, organizations,
                x => x.OrganizationName, x => x.OrganizationID, false, isNew);
            if (ownerOrganizationID.HasValue)
            {
                treatmentBMP.OwnerOrganizationID = ownerOrganizationID.Value;
            }

            //start of Optional Fields
            var yearBuilt =
                GetOptionalIntFieldValue(row, fieldsDict, rowNumber, errorList, "Year Built or Installed");
            if (yearBuilt.HasValue)
            {
                treatmentBMP.YearBuilt = yearBuilt;
            }

            var assetIDInSystemOfRecord = SetStringValue(row, fieldsDict, rowNumber, errorList,
                "Asset ID in System of Record", TreatmentBMP.FieldLengths.SystemOfRecordID, false);
            if (!string.IsNullOrWhiteSpace(assetIDInSystemOfRecord))
            {
                treatmentBMP.SystemOfRecordID = assetIDInSystemOfRecord;
            }

            var notes = SetStringValue(row, fieldsDict, rowNumber, errorList, "Notes",
                TreatmentBMP.FieldLengths.Notes, false);
            if (!string.IsNullOrWhiteSpace(notes))
            {
                treatmentBMP.Notes = notes;
            }

            var fieldNameRequiredLifespanOfInstallation = "Required Lifespan of Installation";
            if (fieldsDict.ContainsKey(fieldNameRequiredLifespanOfInstallation))
            {
                var treatmentBMPLifespanTypeID = FindLookupValue(row, fieldsDict,
                    fieldNameRequiredLifespanOfInstallation, rowNumber, errorList, TreatmentBMPLifespanType.All,
                    x => x.TreatmentBMPLifespanTypeDisplayName, x => x.TreatmentBMPLifespanTypeID, true, false);
                if (treatmentBMPLifespanTypeID.HasValue)
                {
                    treatmentBMP.TreatmentBMPLifespanTypeID = treatmentBMPLifespanTypeID;
                }

                var fieldNameAllowableEndDateOfInstallationIfApplicable =
                    "Allowable End Date of Installation (if applicable)";
                if (fieldsDict.ContainsKey(fieldNameAllowableEndDateOfInstallationIfApplicable))
                {
                    var requiredLifespanOfInstallation = row[fieldsDict[fieldNameRequiredLifespanOfInstallation]];
                    var allowableEndDateOfInstallation =
                        row[fieldsDict[fieldNameAllowableEndDateOfInstallationIfApplicable]];
                    var isAllowableEndDateOfInstallationEmpty =
                        string.IsNullOrWhiteSpace(allowableEndDateOfInstallation);
                    if (isAllowableEndDateOfInstallationEmpty && treatmentBMPLifespanTypeID ==
                        TreatmentBMPLifespanType.FixedEndDate.TreatmentBMPLifespanTypeID)
                    {
                        errorList.Add(
                            $"An end date must be provided if the '{fieldNameRequiredLifespanOfInstallation}' field is set to fixed end date for row: {rowNumber}");
                    }

                    if (!isAllowableEndDateOfInstallationEmpty && treatmentBMPLifespanTypeID !=
                        TreatmentBMPLifespanType.FixedEndDate.TreatmentBMPLifespanTypeID)
                    {
                        errorList.Add(
                            $"An end date was provided when '{fieldNameRequiredLifespanOfInstallation}' field was set to {requiredLifespanOfInstallation} for row: {rowNumber}");
                    }

                    if (!treatmentBMPLifespanTypeID.HasValue && !isAllowableEndDateOfInstallationEmpty)
                    {
                        errorList.Add(
                            $"An end date was provided when '{fieldNameRequiredLifespanOfInstallation}' field was set to null for row: {rowNumber}");
                    }

                    if (!isAllowableEndDateOfInstallationEmpty)
                    {
                        if (!DateTime.TryParse(allowableEndDateOfInstallation,
                            out var allowableEndDateOfInstallationDateTime))
                        {
                            errorList.Add(
                                $"{fieldNameAllowableEndDateOfInstallationIfApplicable} can not be converted to Date Time format at row: {rowNumber}");
                        }
                        else
                        {
                            treatmentBMP.TreatmentBMPLifespanEndDate = allowableEndDateOfInstallationDateTime;
                        }
                    }
                }
            }

            var requiredFieldVisitsPerYear = GetOptionalIntFieldValue(row, fieldsDict, rowNumber, errorList,
                "Required Field Visits Per Year");
            if (requiredFieldVisitsPerYear.HasValue)
            {
                treatmentBMP.RequiredFieldVisitsPerYear = requiredFieldVisitsPerYear;
            }

            var requiredPostStormFieldVisitsPerYear = GetOptionalIntFieldValue(row, fieldsDict, rowNumber,
                errorList, "Required Post-Storm Field Visits Per Year");
            if (requiredPostStormFieldVisitsPerYear.HasValue)
            {
                treatmentBMP.RequiredPostStormFieldVisitsPerYear = requiredPostStormFieldVisitsPerYear;
            }

            //End of Optional Fields
            var trashCaptureStatusTypeID = FindLookupValue(row, fieldsDict, "Trash Capture Status", rowNumber,
                errorList, TrashCaptureStatusType.All, x => x.TrashCaptureStatusTypeDisplayName,
                x => x.TrashCaptureStatusTypeID, true, isNew);
            if (trashCaptureStatusTypeID.HasValue)
            {
                treatmentBMP.TrashCaptureStatusTypeID = trashCaptureStatusTypeID.Value;
            }

            var treatmentBMPSizingBasisTypeID = FindLookupValue(row, fieldsDict, "Sizing Basis", rowNumber,
                errorList, SizingBasisType.All, x => x.SizingBasisTypeDisplayName, x => x.SizingBasisTypeID, true,
                isNew);
            if (treatmentBMPSizingBasisTypeID.HasValue)
            {
                treatmentBMP.SizingBasisTypeID = treatmentBMPSizingBasisTypeID.Value;
            }

            return treatmentBMP;
        }

        private static string SetStringValue(string[] row, Dictionary<string, int> fieldsDict, int rowNumber, List<string> errorList, string fieldName,
            int fieldLength, bool requireNotEmpty)
        {
            if (fieldsDict.ContainsKey(fieldName))
            {
                var fieldValue = row[fieldsDict[fieldName]];
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

        private static int? FindLookupValue<T>(string[] row, Dictionary<string, int> fieldsDict, string fieldName, int rowNumber, List<string> errorList,
            List<T> lookupValues, Func<T, string> funcDisplayName, Func<T, int> funcID, bool showAvailableValuesInErrorMessage, bool requireNotEmpty)
        {
            if (fieldsDict.ContainsKey(fieldName))
            {
                var fieldValue = row[fieldsDict[fieldName]];
                if (!string.IsNullOrWhiteSpace(fieldValue))
                {
                    if (!lookupValues.Select(funcDisplayName.Invoke).Contains(fieldValue))
                    {
                        var errorMessage = $"No {fieldName} with the name '{fieldValue}' exists in our records, row: {rowNumber}.";
                        if (showAvailableValuesInErrorMessage)
                        {
                            errorMessage += $" Acceptable Values Are: {string.Join(", ", lookupValues.Select(funcDisplayName.Invoke))}";
                        }
                        errorList.Add(errorMessage);
                    }
                    else
                    {
                        var entity = lookupValues.Single(x => funcDisplayName.Invoke(x) == fieldValue);
                        return funcID.Invoke(entity);
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

        private static int? GetOptionalIntFieldValue(string[] row, Dictionary<string, int> fieldsDict, int rowNumber, List<string> errorList, string fieldName)
        {
            if (fieldsDict.ContainsKey(fieldName))
            {
                var fieldValue = row[fieldsDict[fieldName]];
                if (!string.IsNullOrWhiteSpace(fieldValue))
                {
                    if (!int.TryParse(fieldValue, out var fieldValueAsInt))
                    {
                        errorList.Add($"{fieldName} can not be converted to Int at row: {rowNumber}");
                    }
                    else
                    {
                        return fieldValueAsInt;
                    }
                }
            }

            return null;
        }

        private static Geometry ParseLocation(string treatmentBMPLatitude, string treatmentBMPLongitude, int rowNumber, List<string> errorList, bool isNew)
        {
            var locationErrorList = new List<string>();
            if (string.IsNullOrWhiteSpace(treatmentBMPLatitude) || string.IsNullOrWhiteSpace(treatmentBMPLongitude))
            {
                if (!isNew)
                {
                    return null;
                }
                var missingLocationFields = new List<string>();
                if (string.IsNullOrWhiteSpace(treatmentBMPLatitude)) missingLocationFields.Add("Latitude");
                if (string.IsNullOrWhiteSpace(treatmentBMPLongitude)) missingLocationFields.Add("Longitude");
                errorList.Add(
                    $"{string.Join(" and ", missingLocationFields)} {(missingLocationFields.Count > 1 ? "are" : "is")} required for new Treatment BMPs at row: {rowNumber}");
                // Return early — otherwise the blank value(s) also trip the decimal-parse/range checks below,
                // producing two extra confusing "can not be converted to Decimal format" errors for the same row.
                return null;
            }
            if (!decimal.TryParse(treatmentBMPLatitude, out var treatmentBMPLatitudeDecimal))
            {
                locationErrorList.Add(
                    $"Treatment BMP Latitude can not be converted to Decimal format at row: {rowNumber}");
            }
            if (!(treatmentBMPLatitudeDecimal <= 90 && treatmentBMPLatitudeDecimal >= -90))
            {
                locationErrorList.Add(
                    $"Treatment BMP Latitude {treatmentBMPLatitudeDecimal} is less than -90 or greater than 90 at row: {rowNumber}");
            }
            if (!decimal.TryParse(treatmentBMPLongitude, out var treatmentBMPLongitudeDecimal))
            {
                locationErrorList.Add(
                    $"Treatment BMP Longitude can not be converted to Decimal format at row: {rowNumber}");
            }
            if (!(treatmentBMPLongitudeDecimal <= 180 && treatmentBMPLongitudeDecimal >= -180))
            {
                locationErrorList.Add(
                    $"Treatment BMP Longitude {treatmentBMPLongitudeDecimal} is less than -180 or greater than 180 at row: {rowNumber}");
            }

            if (locationErrorList.Any())
            {
                errorList.AddRange(locationErrorList);
                return null;
            }

            return GeometryHelper.CreateLocationPoint4326FromLatLong(double.Parse(treatmentBMPLatitude), double.Parse(treatmentBMPLongitude));
        }

        private static List<CustomAttribute> ParseCustomAttributes(NeptuneDbContext dbContext, TreatmentBMP treatmentBMP, TreatmentBMPType treatmentBMPType,
            string[] currentRow, Dictionary<string, int> fieldsDict,
            List<CustomAttributeType> customAttributeTypes, int rowNumber, out List<string> currentErrorList,
            out List<CustomAttributeValue> customAttributeValues)
        {
            currentErrorList = new List<string>();
            customAttributeValues = new List<CustomAttributeValue>();
            var customAttributes = new List<CustomAttribute>();
            var isNew = !IsRealPrimaryKeyValue(treatmentBMP.TreatmentBMPID);
            foreach (var customAttributeType in customAttributeTypes)
            {
                var treatmentBMPTypeCustomAttributeType = customAttributeType.TreatmentBMPTypeCustomAttributeTypes.Single(x => x.TreatmentBMPTypeID == treatmentBMPType.TreatmentBMPTypeID);
                var customAttribute =
                    treatmentBMP.CustomAttributes.SingleOrDefault(x =>
                        x.TreatmentBMPTypeCustomAttributeTypeID == treatmentBMPTypeCustomAttributeType
                            .TreatmentBMPTypeCustomAttributeTypeID) ?? new CustomAttribute()
                    {
                        TreatmentBMP = treatmentBMP,
                        TreatmentBMPTypeCustomAttributeTypeID = treatmentBMPTypeCustomAttributeType.TreatmentBMPTypeCustomAttributeTypeID,
                        TreatmentBMPTypeID = treatmentBMPTypeCustomAttributeType.TreatmentBMPTypeID,
                        CustomAttributeTypeID = treatmentBMPTypeCustomAttributeType.CustomAttributeTypeID
                    };
                if (fieldsDict.ContainsKey(customAttributeType.CustomAttributeTypeName))
                {
                    var value = currentRow[fieldsDict[customAttributeType.CustomAttributeTypeName]];

                    var customAttributeDataTypeEnum = customAttributeType.CustomAttributeDataType.ToEnum;

                    var customAttributeTypeAcceptableValues =
                        customAttributeType.CustomAttributeTypeOptionsSchema != null
                            ? System.Text.Json.JsonSerializer.Deserialize<List<string>>(
                                customAttributeType.CustomAttributeTypeOptionsSchema)
                            : null;

                    if (string.IsNullOrEmpty(value))
                    {
                        //Don't do anything with an empty value if we're updating, but add it if we're new
                        if (isNew)
                        {
                            customAttributeValues.Add(new CustomAttributeValue
                                { CustomAttribute = customAttribute, AttributeValue = value });
                        }
                    }
                    else if (!ValidateCustomAttributeValueEntry(
                        value, 
                        customAttributeDataTypeEnum,
                        customAttributeTypeAcceptableValues))
                    {
                        currentErrorList.Add(GetErrorForCustomAttributeType(
                            value, 
                            customAttributeDataTypeEnum, 
                            customAttributeType.CustomAttributeTypeName,
                            customAttributeType.CustomAttributeDataType.CustomAttributeDataTypeDisplayName, 
                            customAttributeTypeAcceptableValues, 
                            rowNumber));
                    }
                    else
                    {
                        dbContext.CustomAttributeValues.RemoveRange(customAttribute
                            .CustomAttributeValues);
                        customAttribute.CustomAttributeValues.Clear();

                        if (customAttributeType.CustomAttributeDataType == CustomAttributeDataType.MultiSelect)
                        {
                            var attributeValues = value.Split(new[] {','}).Select(x => x.Trim())
                                .Where(x => !string.IsNullOrEmpty(x))
                                .Select(x =>
                                new CustomAttributeValue { CustomAttribute = customAttribute, AttributeValue = x });
                            customAttributeValues.AddRange(attributeValues);
                        }
                        else
                        {
                            customAttributeValues.Add(new CustomAttributeValue
                            {
                                CustomAttribute = customAttribute, AttributeValue = value
                            });
                        }
                    }
                }
                customAttributes.Add(customAttribute);
            }
            return customAttributes;
        }

        private static string GetErrorForCustomAttributeType(string value,
            CustomAttributeDataTypeEnum customAttributeDataTypeEnum,
            string customAttributeTypeName, string customAttributeDataTypeDisplayName, List<string> customAttributeTypeAcceptableValues, int rowNumber)
        {
            switch (customAttributeDataTypeEnum)
            {
                case CustomAttributeDataTypeEnum.Integer:
                case CustomAttributeDataTypeEnum.Decimal:
                case CustomAttributeDataTypeEnum.DateTime:
                    return
                        $"{customAttributeTypeName} field can not be converted to {customAttributeDataTypeDisplayName} at row: {rowNumber}";
                case CustomAttributeDataTypeEnum.MultiSelect:
                case CustomAttributeDataTypeEnum.PickFromList:
                    // Identify the specific offending entry/entries rather than echoing the whole value, so a
                    // mixed multi-select like "Gravel, InvalidMedia" reports only "InvalidMedia".
                    var invalidEntries = value.Split(',').Select(x => x.Trim())
                        .Where(x => !string.IsNullOrEmpty(x))
                        .Where(x => customAttributeTypeAcceptableValues == null || !customAttributeTypeAcceptableValues.Contains(x))
                        .Distinct()
                        .ToList();
                    var invalidEntryDisplay = invalidEntries.Any() ? string.Join(", ", invalidEntries) : value;
                    return
                        $"'{invalidEntryDisplay}' is not a valid {customAttributeTypeName} entry at row: {rowNumber}. Acceptable values are: {string.Join(", ", customAttributeTypeAcceptableValues)}";
                default:
                    return
                        $"{customAttributeTypeName} entry at row: {rowNumber} experienced an unknown error. Please double check the sheet, and contact support with further questions.";
            }
        }

        private static bool ValidateCustomAttributeValueEntry(string value, CustomAttributeDataTypeEnum customAttributeDataType, List<string> customAttributeTypeAcceptableValues)
        {
            switch (customAttributeDataType)
            {
                case CustomAttributeDataTypeEnum.Integer:
                    return int.TryParse(value, out _);
                case CustomAttributeDataTypeEnum.Decimal:
                    return decimal.TryParse(value, out _);
                case CustomAttributeDataTypeEnum.DateTime:
                    return DateTime.TryParse(value, out _);
                case CustomAttributeDataTypeEnum.PickFromList:
                case CustomAttributeDataTypeEnum.MultiSelect:
                    // Reject if ANY entry is invalid. Filter blank fragments (from leading/trailing/double
                    // commas) so they neither trip a false rejection nor mask a genuine invalid entry.
                    var splitValues = value.Split(',').Select(x => x.Trim())
                        .Where(x => !string.IsNullOrEmpty(x))
                        .ToList();

                    return splitValues.Any() &&
                           splitValues.All(x => customAttributeTypeAcceptableValues != null && customAttributeTypeAcceptableValues.Contains(x));
                case CustomAttributeDataTypeEnum.String:
                    return true;
                default:
                    return false;
            }
        }


        private static Dictionary<string, int> ValidateHeader(string[] row, List<string> requiredFields, List<string> optionalFields, List<string> customAttributes, out List<string> errorList, TreatmentBMPType treatmentBMPType)
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
            var optionalFieldDifference = headers.Except(requiredFields).Except(optionalFields);
            var customAttributesDifference = optionalFieldDifference.Except(customAttributes).ToList();

            if (requiredFieldDifference.Any())
            {
                errorList.Add("One or more required headers have not been provided. Required Fields are: " +
                              string.Join(", ", requiredFieldDifference));
            }

            if (customAttributesDifference.Any())
            {
                errorList.Add($"The provided fields '{string.Join(", ", customAttributesDifference)}' did not match a property or custom attribute of the BMP type '{treatmentBMPType.TreatmentBMPTypeName}'");
            }

            return fieldsDict;
        }
    }

}