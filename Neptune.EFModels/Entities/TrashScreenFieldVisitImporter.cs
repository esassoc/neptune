using System.Data;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Neptune.Models.DataTransferObjects;
using Neptune.WebMvc.Common;

namespace Neptune.EFModels.Entities;

// Ported wholesale from the legacy Neptune.WebMvc.Controllers.FieldVisitController.BulkUploadTrashScreenVisit
// flow so the SPA Data Hub can drive the same upload from the API. Behavior is intended to match the
// legacy controller exactly until PR 5 retires the MVC entry point.
public static class TrashScreenFieldVisitImporter
{
    private const int InletAndTrashScreenTreatmentBMPTypeID = 35;

    private const string INLET = "Inlet Condition";
    private const string OUTLET = "Outlet Condition";
    private const string OPERABILITY = "Device Operability";
    private const string NUISANCE = "Significant Nuisance Conditions";
    private const string ACCUMULATION = "Material Accumulation as Percent of Total System Volume";

    private const string GREEN_WASTE = "Percent Green Waste";
    private const string MECHANICAL_REPAIR = "Mechanical Repair Conducted";
    private const string SEDIMENT = "Percent Sediment";
    private const string STRUCTURAL_REPAIR = "Structural Repair Conducted";
    private const string TRASH = "Percent Trash";
    private const string VOLUME_CUFT = "Total Material Volume Removed (cu-ft)";
    private const string VOLUME_GAL = "Total Material Volume Removed (gal)";

    public static async Task<TrashScreenFieldVisitUploadResultDto> BulkUploadAsync(NeptuneDbContext dbContext, Stream xlsxStream, Person currentPerson)
    {
        var result = new TrashScreenFieldVisitUploadResultDto();

        DataTable dataTable;
        try
        {
            dataTable = ReadDataTableFromExcel(xlsxStream, "Field Visits");
        }
        catch (Exception e)
        {
            result.Errors.Add(e.Message.Contains("column", StringComparison.OrdinalIgnoreCase)
                ? e.Message
                : "Unexpected error parsing Excel Spreadsheet upload. Make sure the file matches the provided template and try again.");
            return result;
        }

        var stormwaterJurisdictionsPersonCanView = StormwaterJurisdictions.ListViewableByPersonForBMPs(dbContext, currentPerson);
        if (!currentPerson.IsAdministrator())
        {
            foreach (DataRow row in dataTable.Rows)
            {
                var rowJurisdiction = row["Jurisdiction"].ToString();
                if (!stormwaterJurisdictionsPersonCanView.Select(x => x.Organization.OrganizationName).Contains(rowJurisdiction))
                {
                    result.Errors.Add($"You attempted to upload a spreadsheet containing BMPs in Jurisdiction {rowJurisdiction}, which you do not have permission to manage.");
                    return result;
                }
            }
        }

        var treatmentBMPTypeAssessmentObservationTypes = dbContext.TreatmentBMPTypeAssessmentObservationTypes
            .Include(x => x.TreatmentBMPAssessmentObservationType)
            .Where(x => x.TreatmentBMPTypeID == InletAndTrashScreenTreatmentBMPTypeID)
            .ToList();

        var treatmentBMPTypeCustomAttributeTypes = dbContext.TreatmentBMPTypeCustomAttributeTypes
            .Include(x => x.CustomAttributeType)
            .Where(x => x.TreatmentBMPTypeID == InletAndTrashScreenTreatmentBMPTypeID
                        && x.CustomAttributeType.CustomAttributeTypePurposeID == (int)CustomAttributeTypePurposeEnum.Maintenance)
            .ToList();

        var caredAboutAssessmentObservationTypeNames = new[] { INLET, OUTLET, OPERABILITY, NUISANCE, ACCUMULATION };
        var caredAboutCustomAttributeTypeNames = new[] { GREEN_WASTE, MECHANICAL_REPAIR, SEDIMENT, STRUCTURAL_REPAIR, TRASH, VOLUME_CUFT, VOLUME_GAL };

        var treatmentBMPTypeCustomAttributeTypeDictionary = caredAboutCustomAttributeTypeNames
            .ToDictionary(name => name, name => treatmentBMPTypeCustomAttributeTypes.Single(x => x.CustomAttributeType.CustomAttributeTypeName == name));

        var treatmentBMPAssessmentObservationTypeDictionary = caredAboutAssessmentObservationTypeNames
            .ToDictionary(name => name, name => treatmentBMPTypeAssessmentObservationTypes.Select(x => x.TreatmentBMPAssessmentObservationType).Single(x => x.TreatmentBMPAssessmentObservationTypeName == name));

        var numRows = dataTable.Rows.Count;
        var numColumns = dataTable.Columns.Count;
        var errors = new List<string>();

        try
        {
            for (var i = 0; i < numRows; i++)
            {
                try
                {
                    var row = dataTable.Rows[i];
                    var rowEmpty = true;
                    for (var j = 0; j < numColumns; j++)
                    {
                        rowEmpty = string.IsNullOrWhiteSpace(row[j].ToString());
                        if (!rowEmpty) break;
                    }
                    if (rowEmpty) continue;

                    var treatmentBMPName = row["BMP Name"].ToString()?.Trim();
                    var jurisdictionName = row["Jurisdiction"].ToString()?.Trim();

                    var treatmentBMP = dbContext.TreatmentBMPs
                        .Include(x => x.TreatmentBMPBenchmarkAndThresholds)
                        .Include(x => x.StormwaterJurisdiction).ThenInclude(x => x.Organization)
                        .SingleOrDefault(x => x.TreatmentBMPName == treatmentBMPName
                                              && x.StormwaterJurisdiction.Organization.OrganizationName == jurisdictionName);
                    if (treatmentBMP == null)
                    {
                        throw new InvalidOperationException($"Invalid BMP Name or Jurisdiction at row {i + 2}");
                    }

                    var rawFieldVisitType = row["Field Visit Type"].ToString()?.ToLower();
                    var fieldVisitType = FieldVisitType.All.SingleOrDefault(x => x.FieldVisitTypeDisplayName.ToLower() == rawFieldVisitType);
                    if (fieldVisitType == null)
                    {
                        throw new InvalidOperationException($"Invalid Field Visit Type at row {i + 2}");
                    }

                    var rawFieldVisitDate = row["Field Visit Date"].ToString();
                    if (!DateTime.TryParse(rawFieldVisitDate, out var fieldVisitDate))
                    {
                        throw new InvalidOperationException($"Invalid Field Visit Date at row {i + 2}");
                    }

                    var fieldVisit = dbContext.FieldVisits
                        .Include(x => x.MaintenanceRecord).ThenInclude(x => x.MaintenanceRecordObservations).ThenInclude(x => x.MaintenanceRecordObservationValues)
                        .Include(x => x.TreatmentBMPAssessments).ThenInclude(x => x.TreatmentBMPObservations)
                        .SingleOrDefault(x => x.TreatmentBMPID == treatmentBMP.TreatmentBMPID && x.VisitDate.Date == fieldVisitDate.Date);

                    if (fieldVisit == null)
                    {
                        fieldVisit = new FieldVisit
                        {
                            TreatmentBMP = treatmentBMP,
                            FieldVisitStatusID = FieldVisitStatus.Complete.FieldVisitStatusID,
                            PerformedByPersonID = currentPerson.PersonID,
                            VisitDate = fieldVisitDate.ConvertTimeFromPSTToUTC(),
                            FieldVisitTypeID = fieldVisitType.FieldVisitTypeID,
                            InventoryUpdated = false,
                            IsFieldVisitVerified = true,
                        };
                        await dbContext.FieldVisits.AddAsync(fieldVisit);
                    }

                    var treatmentBMPType = TreatmentBMPTypes.GetByIDWithChangeTracking(dbContext, treatmentBMP.TreatmentBMPTypeID);

                    if (InitialAssessmentFieldsPopulated(row, i))
                    {
                        var initialAssessment = fieldVisit.GetInitialAssessment();
                        if (initialAssessment == null)
                        {
                            initialAssessment = new TreatmentBMPAssessment
                            {
                                TreatmentBMP = treatmentBMP,
                                TreatmentBMPType = treatmentBMPType,
                                FieldVisit = fieldVisit,
                                TreatmentBMPAssessmentTypeID = (int)TreatmentBMPAssessmentTypeEnum.Initial,
                                IsAssessmentComplete = true,
                            };
                            await dbContext.TreatmentBMPAssessments.AddAsync(initialAssessment);
                        }

                        errors = await UpdateOrCreateSingleValueObservationFromDataTableRow(row, treatmentBMPAssessmentObservationTypeDictionary, i, initialAssessment, treatmentBMPType, INLET, ObservationTypeDataTypeEnum.PassFail, false, dbContext, errors);
                        errors = await UpdateOrCreateSingleValueObservationFromDataTableRow(row, treatmentBMPAssessmentObservationTypeDictionary, i, initialAssessment, treatmentBMPType, OUTLET, ObservationTypeDataTypeEnum.PassFail, false, dbContext, errors);
                        errors = await UpdateOrCreateSingleValueObservationFromDataTableRow(row, treatmentBMPAssessmentObservationTypeDictionary, i, initialAssessment, treatmentBMPType, OPERABILITY, ObservationTypeDataTypeEnum.PassFail, false, dbContext, errors);
                        errors = await UpdateOrCreateSingleValueObservationFromDataTableRow(row, treatmentBMPAssessmentObservationTypeDictionary, i, initialAssessment, treatmentBMPType, NUISANCE, ObservationTypeDataTypeEnum.PassFail, false, dbContext, errors);
                        errors = await UpdateOrCreateSingleValueObservationFromDataTableRow(row, treatmentBMPAssessmentObservationTypeDictionary, i, initialAssessment, treatmentBMPType, ACCUMULATION, ObservationTypeDataTypeEnum.Numeric, false, dbContext, errors);

                        try
                        {
                            initialAssessment.CalculateAssessmentScore(treatmentBMPType, treatmentBMP);
                        }
                        catch (Exception ex)
                        {
                            errors.Add(ex.Message + $" on row {i + 2}");
                        }
                    }

                    if (MaintenanceRecordFieldsPopulated(row))
                    {
                        var maintenanceRecord = fieldVisit.MaintenanceRecord;
                        if (maintenanceRecord == null)
                        {
                            maintenanceRecord = new MaintenanceRecord
                            {
                                TreatmentBMP = treatmentBMP,
                                TreatmentBMPType = treatmentBMPType,
                                FieldVisit = fieldVisit,
                            };
                            await dbContext.MaintenanceRecords.AddAsync(maintenanceRecord);
                        }

                        var rawMaintenanceType = row["Maintenance Type"].ToString()?.ToLower();
                        var rawDescription = row["Description"].ToString();
                        var maintenanceRecordType = MaintenanceRecordType.All.SingleOrDefault(x => x.MaintenanceRecordTypeDisplayName.ToLower() == rawMaintenanceType);
                        if (maintenanceRecordType == null)
                        {
                            throw new InvalidOperationException($"Invalid Maintenance type at row {i + 2}");
                        }

                        maintenanceRecord.MaintenanceRecordTypeID = maintenanceRecordType.MaintenanceRecordTypeID;
                        maintenanceRecord.MaintenanceRecordDescription = rawDescription;

                        errors = await UpdateOrCreateMaintenanceRecordObservationFromDataTableRow(dbContext, row, treatmentBMPTypeCustomAttributeTypeDictionary, maintenanceRecord, STRUCTURAL_REPAIR, i, errors);
                        errors = await UpdateOrCreateMaintenanceRecordObservationFromDataTableRow(dbContext, row, treatmentBMPTypeCustomAttributeTypeDictionary, maintenanceRecord, MECHANICAL_REPAIR, i, errors);
                        errors = await UpdateOrCreateMaintenanceRecordObservationFromDataTableRow(dbContext, row, treatmentBMPTypeCustomAttributeTypeDictionary, maintenanceRecord, VOLUME_CUFT, i, errors);
                        errors = await UpdateOrCreateMaintenanceRecordObservationFromDataTableRow(dbContext, row, treatmentBMPTypeCustomAttributeTypeDictionary, maintenanceRecord, VOLUME_GAL, i, errors);
                        errors = await UpdateOrCreateMaintenanceRecordObservationFromDataTableRow(dbContext, row, treatmentBMPTypeCustomAttributeTypeDictionary, maintenanceRecord, TRASH, i, errors);
                        errors = await UpdateOrCreateMaintenanceRecordObservationFromDataTableRow(dbContext, row, treatmentBMPTypeCustomAttributeTypeDictionary, maintenanceRecord, GREEN_WASTE, i, errors);
                        errors = await UpdateOrCreateMaintenanceRecordObservationFromDataTableRow(dbContext, row, treatmentBMPTypeCustomAttributeTypeDictionary, maintenanceRecord, SEDIMENT, i, errors);
                    }

                    if (PostMaintenanceAssessmentFieldsPopulated(row, i))
                    {
                        var postMaintenanceAssessment = fieldVisit.GetPostMaintenanceAssessment();
                        if (postMaintenanceAssessment == null)
                        {
                            postMaintenanceAssessment = new TreatmentBMPAssessment
                            {
                                TreatmentBMP = treatmentBMP,
                                TreatmentBMPType = treatmentBMPType,
                                FieldVisit = fieldVisit,
                                TreatmentBMPAssessmentTypeID = (int)TreatmentBMPAssessmentTypeEnum.PostMaintenance,
                                IsAssessmentComplete = true,
                            };
                            await dbContext.TreatmentBMPAssessments.AddAsync(postMaintenanceAssessment);
                        }

                        errors = await UpdateOrCreateSingleValueObservationFromDataTableRow(row, treatmentBMPAssessmentObservationTypeDictionary, i, postMaintenanceAssessment, treatmentBMPType, INLET, ObservationTypeDataTypeEnum.PassFail, true, dbContext, errors);
                        errors = await UpdateOrCreateSingleValueObservationFromDataTableRow(row, treatmentBMPAssessmentObservationTypeDictionary, i, postMaintenanceAssessment, treatmentBMPType, OUTLET, ObservationTypeDataTypeEnum.PassFail, true, dbContext, errors);
                        errors = await UpdateOrCreateSingleValueObservationFromDataTableRow(row, treatmentBMPAssessmentObservationTypeDictionary, i, postMaintenanceAssessment, treatmentBMPType, OPERABILITY, ObservationTypeDataTypeEnum.PassFail, true, dbContext, errors);
                        errors = await UpdateOrCreateSingleValueObservationFromDataTableRow(row, treatmentBMPAssessmentObservationTypeDictionary, i, postMaintenanceAssessment, treatmentBMPType, NUISANCE, ObservationTypeDataTypeEnum.PassFail, true, dbContext, errors);
                        errors = await UpdateOrCreateSingleValueObservationFromDataTableRow(row, treatmentBMPAssessmentObservationTypeDictionary, i, postMaintenanceAssessment, treatmentBMPType, ACCUMULATION, ObservationTypeDataTypeEnum.Numeric, true, dbContext, errors);

                        if (errors.Count == 0)
                        {
                            postMaintenanceAssessment.CalculateAssessmentScore(treatmentBMPType, treatmentBMP);
                        }
                    }

                    result.RowsProcessed++;
                }
                catch (InvalidOperationException ioe)
                {
                    errors.Add(ioe.Message);
                }
            }
        }
        catch (Exception)
        {
            result.Errors.Add("Unexpected error parsing Excel Spreadsheet upload. Make sure the file matches the provided template and try again.");
            return result;
        }

        if (errors.Count > 0)
        {
            result.Errors = errors;
            return result;
        }

        await dbContext.SaveChangesAsync();
        return result;
    }

    private static bool PostMaintenanceAssessmentFieldsPopulated(DataRow row, int index)
    {
        var startIndex = row.Table.Columns.IndexOf($"{INLET} (Post-Maintenance)");
        var endIndex = row.Table.Columns.IndexOf($"{ACCUMULATION} Notes (Post-Maintenance)");

        // Allow a completely blank post-maintenance section, but require all-or-nothing once any field is filled.
        var allowBlank = true;
        for (var i = startIndex; i <= endIndex; i++)
        {
            if (row.Table.Columns[i].ColumnName.Trim().EndsWith("Notes (Post-Maintenance)"))
            {
                continue;
            }
            if (!string.IsNullOrWhiteSpace(row[i].ToString()))
            {
                allowBlank = false;
            }
            else if (!allowBlank)
            {
                throw new InvalidOperationException($"Post-Maintenance Assessment at row {index + 2} must be completely filled out or left completely blank.");
            }
        }
        return !allowBlank;
    }

    private static bool MaintenanceRecordFieldsPopulated(DataRow row)
    {
        var startIndex = row.Table.Columns.IndexOf("Maintenance Type");
        var endIndex = row.Table.Columns.IndexOf(SEDIMENT);
        for (var i = startIndex; i <= endIndex; i++)
        {
            if (!string.IsNullOrWhiteSpace(row[i].ToString()))
            {
                return true;
            }
        }
        return false;
    }

    private static bool InitialAssessmentFieldsPopulated(DataRow row, int index)
    {
        var startIndex = row.Table.Columns.IndexOf(INLET);
        var endIndex = row.Table.Columns.IndexOf($"{ACCUMULATION} Notes");

        var allowBlank = true;
        for (var i = startIndex; i <= endIndex; i++)
        {
            if (row.Table.Columns[i].ColumnName.Trim().EndsWith("Notes"))
            {
                continue;
            }
            if (!string.IsNullOrWhiteSpace(row[i].ToString()))
            {
                allowBlank = false;
            }
            else if (!allowBlank)
            {
                throw new InvalidOperationException($"Initial Assessment at row {index + 2} must be completely filled out or left completely blank.");
            }
        }
        return !allowBlank;
    }

    private static async Task<List<string>> UpdateOrCreateMaintenanceRecordObservationFromDataTableRow(NeptuneDbContext dbContext,
        DataRow row,
        IReadOnlyDictionary<string, TreatmentBMPTypeCustomAttributeType> treatmentBMPTypeCustomAttributeTypeDictionary,
        MaintenanceRecord maintenanceRecord, string observationName, int rowNumber, List<string> errors)
    {
        var rawObservation = row[observationName].ToString();
        var treatmentBMPTypeCustomAttributeType = treatmentBMPTypeCustomAttributeTypeDictionary[observationName];

        var maintenanceRecordObservation = maintenanceRecord.MaintenanceRecordObservations
            .SingleOrDefault(x => x.CustomAttributeType.CustomAttributeTypeName.Equals(observationName, StringComparison.OrdinalIgnoreCase));

        string valueParsedForDataType;
        try
        {
            valueParsedForDataType = treatmentBMPTypeCustomAttributeType.CustomAttributeType.CustomAttributeDataType.ValueParsedForDataType(rawObservation);
        }
        catch (Exception)
        {
            errors.Add($"Invalid {observationName} at row {rowNumber + 2}");
            return errors;
        }

        // NPT-1071: range-check the numeric maintenance-record columns. Blank cells parse to empty
        // string above and skip this check entirely; only non-empty out-of-range values are flagged.
        var rangeError = ValidateMaintenanceRecordNumericRange(observationName, valueParsedForDataType, rowNumber);
        if (rangeError != null)
        {
            errors.Add(rangeError);
            return errors;
        }

        if (maintenanceRecordObservation != null)
        {
            var maintenanceRecordObservationValue = maintenanceRecordObservation.MaintenanceRecordObservationValues.SingleOrDefault();
            if (maintenanceRecordObservationValue != null)
            {
                maintenanceRecordObservationValue.ObservationValue = valueParsedForDataType;
            }
            else
            {
                maintenanceRecordObservationValue = new MaintenanceRecordObservationValue
                {
                    MaintenanceRecordObservation = maintenanceRecordObservation,
                    ObservationValue = valueParsedForDataType,
                };
                await dbContext.MaintenanceRecordObservationValues.AddAsync(maintenanceRecordObservationValue);
            }
        }
        else
        {
            maintenanceRecordObservation = new MaintenanceRecordObservation
            {
                MaintenanceRecord = maintenanceRecord,
                TreatmentBMPTypeCustomAttributeTypeID = treatmentBMPTypeCustomAttributeType.TreatmentBMPTypeCustomAttributeTypeID,
                TreatmentBMPTypeID = treatmentBMPTypeCustomAttributeType.TreatmentBMPTypeID,
                CustomAttributeType = treatmentBMPTypeCustomAttributeType.CustomAttributeType,
            };
            await dbContext.MaintenanceRecordObservations.AddAsync(maintenanceRecordObservation);
            var maintenanceRecordObservationValue = new MaintenanceRecordObservationValue
            {
                MaintenanceRecordObservation = maintenanceRecordObservation,
                ObservationValue = valueParsedForDataType,
            };
            await dbContext.MaintenanceRecordObservationValues.AddAsync(maintenanceRecordObservationValue);
        }

        return errors;
    }

    private static async Task<List<string>> UpdateOrCreateSingleValueObservationFromDataTableRow(DataRow row,
        Dictionary<string, TreatmentBMPAssessmentObservationType> treatmentBMPAssessmentObservationTypeDictionary,
        int rowNumber, TreatmentBMPAssessment assessment, TreatmentBMPType treatmentBMPType,
        string observationTypeName, ObservationTypeDataTypeEnum dataType, bool isPostMaintenance, NeptuneDbContext dbContext,
        List<string> errors)
    {
        var suffix = isPostMaintenance ? " (Post-Maintenance)" : "";
        var rawValidationResult = dataType.ValidateAndParse(row[$"{observationTypeName}{suffix}"].ToString());
        var rawNotes = row[$"{observationTypeName} Notes{suffix}"].ToString();

        if (!rawValidationResult.IsValid)
        {
            errors.Add($"Invalid {observationTypeName}{suffix} at row {rowNumber + 2}. Message: {rawValidationResult.ErrorMessage}");
            return errors;
        }

        // NPT-1071: Material Accumulation % must be in [0, 100]. Other observation types in this
        // method are Pass/Fail and don't need a numeric bound.
        if (dataType == ObservationTypeDataTypeEnum.Numeric && observationTypeName == ACCUMULATION)
        {
            var accumRangeError = ValidatePercentRange($"{observationTypeName}{suffix}", rawValidationResult.ParsedValue?.ToString(), rowNumber);
            if (accumRangeError != null)
            {
                errors.Add(accumRangeError);
                return errors;
            }
        }

        var conditionBoxed = new
        {
            SingleValueObservations = new[]
            {
                new
                {
                    PropertyObserved = observationTypeName,
                    ObservationValue = rawValidationResult.ParsedValue,
                    Notes = rawNotes,
                },
            },
        };

        var conditionJson = Common.GeoSpatial.GeoJsonSerializer.Serialize(conditionBoxed);
        var validateObservationDataJson = treatmentBMPAssessmentObservationTypeDictionary[observationTypeName]
            .ObservationTypeSpecification.ObservationTypeCollectionMethod
            .ValidateObservationDataJson(treatmentBMPAssessmentObservationTypeDictionary[observationTypeName], conditionJson);
        if (validateObservationDataJson.Count > 0)
        {
            errors.Add($"Invalid {observationTypeName} at row {rowNumber + 2}");
            return errors;
        }

        try
        {
            var observation = await GetExistingTreatmentBMPObservationOrCreateNew(assessment,
                treatmentBMPAssessmentObservationTypeDictionary[observationTypeName], treatmentBMPType, dbContext);
            observation.ObservationData = conditionJson;
        }
        catch (Exception e)
        {
            errors.Add($"{e.Message} on row {rowNumber + 2}");
        }

        return errors;
    }

    /// <summary>
    /// NPT-1071: range-check for the numeric maintenance-record columns. Volumes must be ≥ 0;
    /// percent columns must be in [0, 100]. Returns the user-facing error string or null when the
    /// value is in range (or blank / unparseable — unparseable values are caught upstream).
    /// Exposed for unit testing.
    /// </summary>
    public static string? ValidateMaintenanceRecordNumericRange(string observationName, string? parsedValue, int rowNumber)
    {
        if (!decimal.TryParse(parsedValue, out var value))
        {
            return null;
        }
        return observationName switch
        {
            TRASH or GREEN_WASTE or SEDIMENT when value < 0 || value > 100
                => $"{observationName} '{value}' must be between 0 and 100 at row {rowNumber + 2}",
            VOLUME_CUFT or VOLUME_GAL when value < 0
                => $"{observationName} '{value}' must be 0 or greater at row {rowNumber + 2}",
            _ => null,
        };
    }

    /// <summary>
    /// NPT-1071: range-check for percentage observations on the assessment blocks (currently just
    /// Material Accumulation %). Same shape as ValidateMaintenanceRecordNumericRange so the
    /// user-facing error wording stays consistent across the two parse paths. Exposed for unit
    /// testing.
    /// </summary>
    public static string? ValidatePercentRange(string fieldName, string? parsedValue, int rowNumber)
    {
        if (!decimal.TryParse(parsedValue, out var value))
        {
            return null;
        }
        if (value < 0 || value > 100)
        {
            return $"{fieldName} '{value}' must be between 0 and 100 at row {rowNumber + 2}";
        }
        return null;
    }

    private static async Task<TreatmentBMPObservation> GetExistingTreatmentBMPObservationOrCreateNew(
        TreatmentBMPAssessment treatmentBMPAssessment,
        TreatmentBMPAssessmentObservationType treatmentBMPAssessmentObservationType,
        TreatmentBMPType treatmentBMPType,
        NeptuneDbContext dbContext)
    {
        var observation = treatmentBMPAssessment.TreatmentBMPObservations.ToList()
            .Find(x => x.TreatmentBMPAssessmentObservationTypeID == treatmentBMPAssessmentObservationType.TreatmentBMPAssessmentObservationTypeID);
        if (observation == null)
        {
            var treatmentBMPTypeAssessmentObservationType = treatmentBMPType.TreatmentBMPTypeAssessmentObservationTypes
                .SingleOrDefault(x => x.TreatmentBMPAssessmentObservationTypeID == treatmentBMPAssessmentObservationType.TreatmentBMPAssessmentObservationTypeID);
            if (treatmentBMPTypeAssessmentObservationType == null)
            {
                throw new NullReferenceException($"Not a valid Observation Type {treatmentBMPAssessmentObservationType.TreatmentBMPAssessmentObservationTypeName} for Treatment BMP Type {treatmentBMPType.TreatmentBMPTypeName}");
            }
            observation = new TreatmentBMPObservation
            {
                TreatmentBMPAssessment = treatmentBMPAssessment,
                TreatmentBMPTypeAssessmentObservationType = treatmentBMPTypeAssessmentObservationType,
                TreatmentBMPType = treatmentBMPType,
                TreatmentBMPAssessmentObservationType = treatmentBMPAssessmentObservationType,
            };
            await dbContext.TreatmentBMPObservations.AddAsync(observation);
        }
        return observation;
    }

    private static DataTable ReadDataTableFromExcel(Stream inputStream, string worksheetName)
    {
        var dataTable = new DataTable();
        using var workbook = new XLWorkbook(inputStream);
        var worksheet = workbook.Worksheet(worksheetName);

        var firstRow = true;
        foreach (var row in worksheet.Rows())
        {
            if (firstRow)
            {
                foreach (var cell in row.Cells())
                {
                    if (!string.IsNullOrEmpty(cell.Value.ToString()))
                    {
                        dataTable.Columns.Add(cell.Value.ToString());
                    }
                    else
                    {
                        break;
                    }
                }
                firstRow = false;
            }
            else
            {
                var i = 0;
                var toInsert = dataTable.NewRow();
                foreach (var cell in row.Cells(1, dataTable.Columns.Count))
                {
                    toInsert[i] = cell.Value.ToString();
                    i++;
                }
                dataTable.Rows.Add(toInsert);
            }
        }
        return dataTable;
    }
}
