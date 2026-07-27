using System.IO;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neptune.API.Services;
using Neptune.Common;
using Neptune.Common.Services;
using Neptune.API.Services.Authorization;
using Neptune.EFModels.Entities;
using Neptune.Models.DataTransferObjects;

namespace Neptune.API.Controllers;

[ApiController]
[Route("data-hub")]
public class DataHubController(
    NeptuneDbContext dbContext,
    ILogger<DataHubController> logger,
    IOptions<NeptuneConfiguration> neptuneConfiguration,
    AzureBlobStorageService azureBlobStorageService)
    : SitkaController<DataHubController>(dbContext, logger, neptuneConfiguration)
{
    [HttpGet("last-updated")]
    [JurisdictionEditFeature]
    public async Task<ActionResult<DataHubLastUpdatedDto>> GetLastUpdated()
    {
        var dto = new DataHubLastUpdatedDto
        {
            Parcels = await Parcels.GetLatestUpdateAsync(DbContext),
            RegionalSubbasins = await RegionalSubbasins.GetLatestUpdateAsync(DbContext),
            HRUCharacteristics = await HRUCharacteristics.GetLatestUpdateAsync(DbContext),
            ModelBasins = await ModelBasins.GetLatestUpdateAsync(DbContext),
            PrecipitationZones = await PrecipitationZones.GetLatestUpdateAsync(DbContext),
            OCTAPrioritizations = await OCTAPrioritizations.GetLatestUpdateAsync(DbContext),
        };
        return Ok(dto);
    }

    /// <summary>
    /// NPT-998: streams a static Data Hub bulk-upload template stored in Azure Blob Storage.
    /// One endpoint covers the static templates (WQMP, Simplified BMP, WQMP Locations, OVTA) so
    /// the SPA upload pages can offer a "Download Template" button without bouncing users back to
    /// the MVC site. Each template's blob path is configured on NeptuneConfiguration with values
    /// that match the legacy MVC's WebConfiguration so both surfaces can serve the same files
    /// during the retirement transition. The Trash Screen Field Visit template is served instead
    /// by the dedicated <see cref="DownloadTrashScreenUploadTemplate"/> action, which pre-populates
    /// BMP rows.
    /// </summary>
    [HttpGet("upload-templates/{templateKey}")]
    [JurisdictionEditFeature]
    public async Task<IActionResult> DownloadUploadTemplate([FromRoute] string templateKey)
    {
        var config = neptuneConfiguration.Value;
        var (blobPath, downloadFileName) = templateKey switch
        {
            "wqmp" => (config.PathToBulkUploadWQMPTemplate, $"UploadWQMPTemplate_{CallingUser.LastName}{CallingUser.FirstName}.xlsx"),
            "simplified-bmp" => (config.PathToSimplifiedBMPTemplate, $"SimplifiedBMPBulkUploadTemplate_{CallingUser.LastName}{CallingUser.FirstName}.xlsx"),
            "wqmp-locations" => (config.PathToUploadWQMPBoundaryTemplate, $"UploadWQMPBoundaryTemplate_{CallingUser.LastName}{CallingUser.FirstName}.csv"),
            "ovta" => (config.PathToOVTAUploadTemplate, $"OVTABulkUploadTemplate_{CallingUser.LastName}{CallingUser.FirstName}.xlsx"),
            _ => (null, null),
        };

        if (blobPath == null)
        {
            return NotFound($"Unknown upload template key '{templateKey}'.");
        }

        if (string.IsNullOrWhiteSpace(blobPath))
        {
            return Problem($"Upload template path for '{templateKey}' is not configured.", statusCode: StatusCodes.Status500InternalServerError);
        }

        var contentType = downloadFileName!.EndsWith(".csv")
            ? "text/csv"
            : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        var download = await azureBlobStorageService.DownloadBlobFromBlobStorageAsStream(blobPath);
        return File(download.Content, contentType, downloadFileName);
    }

    // TreatmentBMPType "Inlet and Trash Screen" — the trash-screen field-visit workflow keys off
    // this fixed type ID (mirrors TrashScreenFieldVisitImporter.InletAndTrashScreenTreatmentBMPTypeID).
    private const int InletAndTrashScreenTreatmentBMPTypeID = 35;

    // The worksheet the field-visit importer reads back (TrashScreenFieldVisitImporter); the
    // pre-populated rows must land on this exact tab.
    private const string TrashScreenTemplateWorksheetName = "Field Visits";

    /// <summary>
    /// NPT-1114: regenerates the Trash Screen Field Visit upload template with one pre-populated
    /// row per trash-screen BMP (TreatmentBMPType "Inlet and Trash Screen") the caller can access,
    /// filtered to their viewable jurisdictions. Restores the dynamic pre-population the legacy MVC
    /// FieldVisitController.TrashScreenBulkUploadTemplate provided before the SPA migration reduced
    /// this template to a static blank blob. Assessment/maintenance columns stay blank for the user
    /// to fill in; the generic <see cref="DownloadUploadTemplate"/> endpoint still serves the other
    /// (static) templates unchanged.
    /// </summary>
    [HttpGet("upload-templates/trash-screen")]
    [JurisdictionEditFeature]
    public async Task<IActionResult> DownloadTrashScreenUploadTemplate()
    {
        var config = neptuneConfiguration.Value;
        if (string.IsNullOrWhiteSpace(config.PathToFieldVisitUploadTemplate))
        {
            return Problem("Trash Screen upload template path is not configured.", statusCode: StatusCodes.Status500InternalServerError);
        }

        var stormwaterJurisdictionIDsPersonCanView =
            await StormwaterJurisdictionPeople.ListViewableStormwaterJurisdictionIDsByPersonIDForBMPsAsync(DbContext, CallingUser.PersonID);
        // Ordered by TreatmentBMPName; CustomAttributeValues is keyed by CustomAttributeTypeID.
        var trashScreens = await TreatmentBMPs.ListByTypeAsGridDtoForJurisdictionsAsync(
            DbContext, InletAndTrashScreenTreatmentBMPTypeID, stormwaterJurisdictionIDsPersonCanView);

        using var tempFile = DisposableTempFile.MakeDisposableTempFileEndingIn(".xlsx");
        await azureBlobStorageService.DownloadBlobToFileAsync(config.PathToFieldVisitUploadTemplate, tempFile.FileInfo.FullName);

        using var workbook = new XLWorkbook(tempFile.FileInfo.FullName);
        // The base template must expose the "Field Visits" sheet the importer reads back; guard
        // rather than let ClosedXML throw an opaque ArgumentException if the configured blob's tab
        // is renamed. TryGetWorksheet avoids the throw entirely.
        if (!workbook.TryGetWorksheet(TrashScreenTemplateWorksheetName, out var worksheet))
        {
            return Problem(
                $"The Trash Screen upload template is missing the '{TrashScreenTemplateWorksheetName}' worksheet.",
                statusCode: StatusCodes.Status500InternalServerError);
        }

        var row = 2;
        foreach (var bmp in trashScreens)
        {
            worksheet.Cell($"A{row}").Value = bmp.TreatmentBMPName;
            worksheet.Cell($"B{row}").Value = bmp.StormwaterJurisdictionName;
            if (bmp.YearBuilt.HasValue)
            {
                worksheet.Cell($"C{row}").Value = bmp.YearBuilt.Value;
            }
            if (!string.IsNullOrWhiteSpace(bmp.Notes))
            {
                worksheet.Cell($"D{row}").Value = bmp.Notes;
            }
            SetIntAttributeCell(worksheet, bmp, CustomAttributeTypes.CustomAttributeTypeIDNumberOfInletScreens, "E", row);
            SetIntAttributeCell(worksheet, bmp, CustomAttributeTypes.CustomAttributeTypeIDNumberOfTrashBaskets, "F", row);
            SetIntAttributeCell(worksheet, bmp, CustomAttributeTypes.CustomAttributeTypeIDNumberOfConnectorPipeScreens, "G", row);
            row++;
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var downloadFileName = $"TrashScreenBulkUploadTemplate_{CallingUser.LastName}{CallingUser.FirstName}.xlsx";
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", downloadFileName);
    }

    // Writes a custom-attribute count into the given column only when the stored value parses as an
    // int — matches the legacy SetCellValueFromCustomAttribute (numeric cell, blank otherwise).
    private static void SetIntAttributeCell(IXLWorksheet worksheet, TreatmentBMPByTypeGridDto bmp, int customAttributeTypeID, string column, int row)
    {
        if (bmp.CustomAttributeValues.TryGetValue(customAttributeTypeID, out var raw) && int.TryParse(raw, out var value))
        {
            worksheet.Cell($"{column}{row}").Value = value;
        }
    }
}
