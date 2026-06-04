using System.Data;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Neptune.Common;
using Neptune.Common.Services;
using Neptune.EFModels.Entities;
using Neptune.WebMvc.Common;
using Neptune.WebMvc.Security;
using Neptune.WebMvc.Services;
using Neptune.WebMvc.Views.OnlandVisualTrashAssessment;

namespace Neptune.WebMvc.Controllers
{
    public class OnlandVisualTrashAssessmentController(
        NeptuneDbContext dbContext,
        ILogger<OnlandVisualTrashAssessmentController> logger,
        IOptions<WebConfiguration> webConfiguration,
        LinkGenerator linkGenerator,
        AzureBlobStorageService azureBlobStorageService)
        : NeptuneBaseController<OnlandVisualTrashAssessmentController>(dbContext, logger, linkGenerator,
            webConfiguration)
    {
        [HttpGet]
        [JurisdictionManageFeature]
        public ViewResult BulkUploadOTVAs()
        {
            var bulkUploadTrashScreenVisitViewModel = new BulkUploadOVTAsViewModel();

            return ViewBulkUploadOTVAs(bulkUploadTrashScreenVisitViewModel);
        }

        private ViewResult ViewBulkUploadOTVAs(
            BulkUploadOVTAsViewModel bulkUploadTrashScreenVisitViewModel)
        {
            var neptunePage = NeptunePages.GetNeptunePageByPageType(_dbContext, NeptunePageType.UploadOVTAs);
            var bulkUploadTrashScreenVisitViewData = new BulkUploadOVTAsViewData(HttpContext, _linkGenerator, _webConfiguration, CurrentPerson, neptunePage, StormwaterJurisdictions.ListViewableByPersonForBMPs(_dbContext, CurrentPerson));

            return RazorView<BulkUploadOVTAs, BulkUploadOVTAsViewData,
                BulkUploadOVTAsViewModel>(bulkUploadTrashScreenVisitViewData,
                bulkUploadTrashScreenVisitViewModel);
        }

        [HttpPost]
        [JurisdictionManageFeature]
        [RequestSizeLimit(100_000_000_000)]
        [RequestFormLimits(MultipartBodyLengthLimit = 100_000_000_000)]
        public async Task<IActionResult> BulkUploadOTVAs(BulkUploadOVTAsViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                return ViewBulkUploadOTVAs(viewModel);
            }

            var uploadXlsxInputStream = viewModel.UploadXLSX.OpenReadStream();

            DataTable dataTableFromExcel;

            try
            {
                dataTableFromExcel = GetDataTableFromExcel(uploadXlsxInputStream, "OVTA Assessments");
            }
            catch (Exception)
            {
                SetErrorForDisplay("Unexpected error parsing Excel Spreadsheet upload. Make sure the file matches the provided template and try again.");
                return ViewBulkUploadOTVAs(viewModel);
            }

            var numRows = dataTableFromExcel.Rows.Count;

            var stormwaterJurisdictionsPersonCanView = StormwaterJurisdictions.ListViewableByPersonForBMPs(_dbContext, CurrentPerson);
            var ovtaAreas = _dbContext.OnlandVisualTrashAssessmentAreas.ToList();
            var users = _dbContext.People.ToList();
            if (!CurrentPerson.IsAdministrator())
            {
                foreach (DataRow row in dataTableFromExcel.Rows)
                {
                    var rowJurisdiction = row["Jurisdiction Name"].ToString();
                    if (!string.IsNullOrWhiteSpace(rowJurisdiction) && !stormwaterJurisdictionsPersonCanView.Select(x => x.Organization.OrganizationName)
                            .Contains(rowJurisdiction))
                    {
                        SetErrorForDisplay(
                            $"You attempted to upload a spreadsheet containing OVTAs in Jurisdiction {rowJurisdiction}, which you do not have permission to manage.");
                        return ViewBulkUploadOTVAs(viewModel);
                    }
                }
            }

            var numColumns = dataTableFromExcel.Columns.Count;

            var errors = new List<string>();

            var ovtaAreaIDsForScoreRecalculation = new List<int?>();

            try
            {
                for (var i = 0; i < numRows; i++)
                {
                    try
                    {
                        var row = dataTableFromExcel.Rows[i];
                        var rowEmpty = true;
                        for (var j = 0; j < numColumns; j++)
                        {
                            rowEmpty = string.IsNullOrWhiteSpace(row[j].ToString());
                            if (!rowEmpty)
                            {
                                break;
                            }
                        }

                        if (rowEmpty)
                        {
                            continue;
                        }

                        var areaName = ovtaAreas.SingleOrDefault(x => x.OnlandVisualTrashAssessmentAreaName == row["Area Name"].ToString())?.OnlandVisualTrashAssessmentAreaID;
                        var createdByPersonID = users.SingleOrDefault(x => x.Email == row["Created By Person"].ToString().Trim())?.PersonID;

                        var newErrors = CheckDataFromRow(areaName, i, createdByPersonID, row);

                        if (newErrors.Count > 0)
                        {
                            errors.AddRange(newErrors);
                            continue;
                        }
                        ovtaAreaIDsForScoreRecalculation.Add(areaName);

                        var categories = PreliminarySourceIdentificationCategory.All.Select(x =>
                            x.PreliminarySourceIdentificationCategoryDisplayName).ToList();
                        var assessmentPreliminarySourceIdentificationTypes = new List<OnlandVisualTrashAssessmentPreliminarySourceIdentificationType>();
                        foreach (var category in categories)
                        {
                            if (!string.IsNullOrWhiteSpace(row[category].ToString()))
                            {
                                var identificationTypes = row[category].ToString().Trim().Split(',');
                                foreach (var identificationType in identificationTypes)
                                {
                                    if (identificationType.ToLower().Contains("other"))
                                    {
                                        errors.Add($"Cannot use {identificationType.Trim()} in row {i+1} as bulk uploader does not allow for Other as a preliminary type.");
                                        continue;
                                    }
                                    var id = PreliminarySourceIdentificationType.All.SingleOrDefault(x =>
                                        x.PreliminarySourceIdentificationTypeDisplayName.Trim() == identificationType
                                        && x.PreliminarySourceIdentificationCategory.PreliminarySourceIdentificationCategoryDisplayName.Trim() == category);
                                    if (id == null)
                                    {
                                        errors.Add($"{identificationType} is not a valid Preliminary Source Identification Type for {category} in row {i + 1}");
                                        continue;
                                    }
                                    assessmentPreliminarySourceIdentificationTypes.Add(new OnlandVisualTrashAssessmentPreliminarySourceIdentificationType()
                                    {
                                        PreliminarySourceIdentificationTypeID = id.PreliminarySourceIdentificationTypeID
                                    });
                                }
                            }

                        }


                        // check to make sure values are valid
                        var onlandTrashVisualAssessment = new OnlandVisualTrashAssessment()
                        {
                            OnlandVisualTrashAssessmentAreaID = areaName,
                            CreatedByPersonID = (int)createdByPersonID,
                            StormwaterJurisdictionID = stormwaterJurisdictionsPersonCanView.Single(x => x.Organization.OrganizationName == row["Jurisdiction Name"].ToString() || x.Organization.OrganizationShortName == row["Jurisdiction Name"].ToString()).StormwaterJurisdictionID,
                            OnlandVisualTrashAssessmentStatusID = row["Status"].ToString().Trim() == "Finalized" ? (int)OnlandVisualTrashAssessmentStatusEnum.Complete : (int)OnlandVisualTrashAssessmentStatusEnum.InProgress,
                            CreatedDate = DateTime.UtcNow,
                            CompletedDate = DateOnly.FromDateTime(DateTime.Parse(row["Completed Date"].ToString().Trim())),
                            OnlandVisualTrashAssessmentScoreID = OnlandVisualTrashAssessmentScore.All.Single(x => x.OnlandVisualTrashAssessmentScoreDisplayName == row["Score"].ToString().Trim()).OnlandVisualTrashAssessmentScoreID,
                            IsProgressAssessment = row["Is Progress Assessment"].ToString().Trim() == "Yes",
                            OnlandVisualTrashAssessmentPreliminarySourceIdentificationTypes = assessmentPreliminarySourceIdentificationTypes,
                            IsTransectBackingAssessment = false
                        };

                        _dbContext.Add(onlandTrashVisualAssessment);

                    }
                    catch (InvalidOperationException ioe)
                    {
                        errors.Add(ioe.Message + $" (row {i})");
                    }
                }
            }
            catch (Exception e)
            {
                SetErrorForDisplay("Unexpected error parsing Excel Spreadsheet upload. Make sure the file matches the provided template and try again.");
                return ViewBulkUploadOTVAs(viewModel);
            }

            if (errors.Count > 0)
            {
                SetErrorForDisplay(string.Join("<br/>", errors));
                return ViewBulkUploadOTVAs(viewModel);
            }

            await _dbContext.SaveChangesAsync();

            // Recalculate scores for all OVTAs in the areas that were uploaded
            foreach (var ovtaAreaID in ovtaAreaIDsForScoreRecalculation)
            {
                var ovtaArea = ovtaAreas.SingleOrDefault(x => x.OnlandVisualTrashAssessmentAreaID == ovtaAreaID);
                if (ovtaArea != null)
                {
                    var assessments = _dbContext.OnlandVisualTrashAssessments
                        .Where(x => x.OnlandVisualTrashAssessmentAreaID == ovtaAreaID).ToList();
                    ovtaArea.OnlandVisualTrashAssessmentBaselineScoreID = OnlandVisualTrashAssessmentAreas.CalculateBaselineScoreFromBackingData(assessments)?.OnlandVisualTrashAssessmentScoreID;
                    ovtaArea.OnlandVisualTrashAssessmentProgressScoreID = OnlandVisualTrashAssessments.CalculateProgressScore(assessments)?.OnlandVisualTrashAssessmentScoreID;
                }
            }
            await _dbContext.SaveChangesAsync();

            SetMessageForDisplay("Successfully bulk uploaded OVTAs");

            return new RedirectResult($"{_webConfiguration.TrashModuleBaseUrl}/onland-visual-trash-assessments");
        }

        private static List<string> CheckDataFromRow(int? areaName, int i, int? createdByPersonID, DataRow row)
        {
            var errors = new List<string>();
            if (areaName == null)
            {
                errors.Add($"Cannot find OVTA area name in row {i + 1}");
            }

            if (createdByPersonID == null)
            {
                errors.Add($"Cannot find Person in row {i + 1}");
            }

            if (string.IsNullOrWhiteSpace(row["Is Progress Assessment"].ToString()) ||
                (row["Is Progress Assessment"].ToString().Trim() != "Yes" && row["Is Progress Assessment"].ToString().Trim() != "No"))
            {
                errors.Add($"Is Progress Assessment is not a valid value in row {i + 1}. It must be either Yes or No.");
            }

            if (string.IsNullOrWhiteSpace(row["Status"].ToString()) ||
                (row["Status"].ToString().Trim() != "Finalized" && row["Status"].ToString().Trim() != "Draft"))
            {
                errors.Add($"Status is not a valid value in row {i + 1}. It must be either Finalized or Draft.");
            }

            if (string.IsNullOrWhiteSpace(row["Score"].ToString()) ||
                !(OnlandVisualTrashAssessmentScore.All
                    .Select(x => x.OnlandVisualTrashAssessmentScoreDisplayName).ToList()
                    .Contains(row["Score"].ToString().Trim())))
            {
                errors.Add($"Score is not a valid value in row {i + 1}. It must be one of the following A, B, C or D.");
            }

            return errors;
        }

        [HttpGet]
        [JurisdictionManageFeature]
        public async Task<FileResult> OVTABulkUploadTemplate()
        {
            using var disposableTempFile = DisposableTempFile.MakeDisposableTempFileEndingIn(".xlsx");
            await azureBlobStorageService.DownloadBlobToFileAsync(_webConfiguration.PathToOVTAUploadTemplate, disposableTempFile.FileInfo.FullName);
            using var workbook = new XLWorkbook(disposableTempFile.FileInfo.FullName);

            var row = 2;
            var worksheet = workbook.Worksheet("OVTA Assessments");
            var stormwaterJurisdictionsPersonCanEdit =StormwaterJurisdictions.ListViewableByPersonForBMPs(_dbContext, CurrentPerson).Select(x => x.StormwaterJurisdictionID);
            var ovtaAreas = _dbContext.OnlandVisualTrashAssessmentAreas
                .Include(x => x.StormwaterJurisdiction)
                .ThenInclude(x => x.Organization)
                .Where(x => stormwaterJurisdictionsPersonCanEdit.Contains(x.StormwaterJurisdictionID)).ToList();
            

            foreach (var ovtaArea in ovtaAreas)
            {
                worksheet.Cells($"A{row}").Value = ovtaArea.OnlandVisualTrashAssessmentAreaName;
                worksheet.Cells($"B{row}").Value = ovtaArea.StormwaterJurisdiction.GetOrganizationDisplayName();
                row++;
            }

            using var stream2 = new MemoryStream();
            workbook.SaveAs(stream2);
            return File(stream2.ToArray(), @"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"OVTAsBulkUploadTemplate_{CurrentPerson.LastName}{CurrentPerson.FirstName}.xlsx");
        }


        private static DataTable GetDataTableFromExcel(Stream inputStream, dynamic worksheet)
        {
            var dataTable = new DataTable();
            using var workBook = new XLWorkbook(inputStream);
            IXLWorksheet workSheet = workBook.Worksheet(worksheet);

            //Loop through the Worksheet rows.
            var firstRow = true;
            foreach (var row in workSheet.Rows())
            {
                //Use the first row to add columns to DataTable.
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
}
