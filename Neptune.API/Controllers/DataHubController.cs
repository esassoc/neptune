using System;
using System.Collections.Generic;
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
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
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

    // Header text of the pre-populated columns in the "Field Visits" sheet. Resolved by name at
    // generation time (see DownloadTrashScreenUploadTemplate) so the generator doesn't couple to
    // column letters and stays aligned with the importer, which reads back by header name. (NPT-1114)
    private const string BMPNameHeader = "BMP Name";
    private const string JurisdictionHeader = "Jurisdiction";
    private const string YearBuiltHeader = "Year Built";
    private const string NotesHeader = "BMP Notes";
    private const string InletScreensHeader = "# of inlet screens";
    private const string TrashBasketsHeader = "# of trash baskets";
    private const string ConnectorPipeScreensHeader = "# of connector pipe screens";

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
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
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

        // NPT-1114: resolve target columns by header text instead of hard-coded letters, so the
        // generated cells stay aligned with the importer (which reads by header name) even if the
        // template's column order changes. BMP Name + Jurisdiction are the importer's lookup keys, so
        // the file is unusable on re-upload if either is missing — guard those explicitly.
        var headerColumns = BuildHeaderColumnMap(worksheet);
        if (!headerColumns.TryGetValue(BMPNameHeader, out var bmpNameColumn) ||
            !headerColumns.TryGetValue(JurisdictionHeader, out var jurisdictionColumn))
        {
            return Problem(
                $"The Trash Screen upload template is missing the '{BMPNameHeader}' or '{JurisdictionHeader}' column.",
                statusCode: StatusCodes.Status500InternalServerError);
        }

        int? ColumnFor(string header) => headerColumns.TryGetValue(header, out var c) ? c : null;
        var yearBuiltColumn = ColumnFor(YearBuiltHeader);
        var notesColumn = ColumnFor(NotesHeader);
        var inletScreensColumn = ColumnFor(InletScreensHeader);
        var trashBasketsColumn = ColumnFor(TrashBasketsHeader);
        var connectorPipeScreensColumn = ColumnFor(ConnectorPipeScreensHeader);

        var row = 2;
        foreach (var bmp in trashScreens)
        {
            worksheet.Cell(row, bmpNameColumn).Value = bmp.TreatmentBMPName;
            worksheet.Cell(row, jurisdictionColumn).Value = bmp.StormwaterJurisdictionName;
            if (yearBuiltColumn.HasValue && bmp.YearBuilt.HasValue)
            {
                worksheet.Cell(row, yearBuiltColumn.Value).Value = bmp.YearBuilt.Value;
            }
            if (notesColumn.HasValue && !string.IsNullOrWhiteSpace(bmp.Notes))
            {
                worksheet.Cell(row, notesColumn.Value).Value = bmp.Notes;
            }
            SetIntAttributeCell(worksheet, bmp, CustomAttributeTypes.CustomAttributeTypeIDNumberOfInletScreens, inletScreensColumn, row);
            SetIntAttributeCell(worksheet, bmp, CustomAttributeTypes.CustomAttributeTypeIDNumberOfTrashBaskets, trashBasketsColumn, row);
            SetIntAttributeCell(worksheet, bmp, CustomAttributeTypes.CustomAttributeTypeIDNumberOfConnectorPipeScreens, connectorPipeScreensColumn, row);
            row++;
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var downloadFileName = $"TrashScreenBulkUploadTemplate_{CallingUser.LastName}{CallingUser.FirstName}.xlsx";
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", downloadFileName);
    }

    // Writes a custom-attribute count into the given column only when the column exists in the
    // template (non-null) and the stored value parses as an int — matches the legacy
    // SetCellValueFromCustomAttribute (numeric cell, blank otherwise).
    private static void SetIntAttributeCell(IXLWorksheet worksheet, TreatmentBMPByTypeGridDto bmp, int customAttributeTypeID, int? column, int row)
    {
        if (column.HasValue
            && bmp.CustomAttributeValues.TryGetValue(customAttributeTypeID, out var raw)
            && int.TryParse(raw, out var value))
        {
            worksheet.Cell(row, column.Value).Value = value;
        }
    }

    // NPT-1114: maps each non-empty header cell in row 1 to its 1-based column number
    // (case-insensitive, trimmed; first occurrence wins) so the generator can address columns by
    // header text rather than assuming a fixed A/B/C… order.
    private static Dictionary<string, int> BuildHeaderColumnMap(IXLWorksheet worksheet)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var cell in worksheet.Row(1).CellsUsed())
        {
            var header = cell.GetString().Trim();
            if (!string.IsNullOrEmpty(header) && !map.ContainsKey(header))
            {
                map[header] = cell.Address.ColumnNumber;
            }
        }
        return map;
    }
}
