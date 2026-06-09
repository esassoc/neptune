using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Neptune.Common.Services;

namespace Neptune.ExternalAPI.Controllers;

[Authorize]
[ApiController]
[Tags("Model Results")]
[Route("model-results")]
public class ModelResultsController(AzureBlobStorageService blobStorageService) : ControllerBase
{
    [HttpGet("current")]
    [EndpointSummary("Model Results")]
    [EndpointDescription("Returns all pollutant runoff/reduction model results for all nodes in South Orange County. " +
                         "**Response is large (often >100 MB).** Use cURL, PowerBI, or another streaming client to consume it — " +
                         "Scalar's in-browser \"try it out\" cannot render responses this size and will throw an allocation error.")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public Task<IActionResult> Current()
    {
        return BlobFileDownloadHelper.DownloadIfExistsAsync(blobStorageService, BlobFileNames.ModelResults, Response);
    }

    [HttpGet("baseline")]
    [EndpointSummary("Baseline Model Results")]
    [EndpointDescription("Returns all pollutant runoff/reduction model results for all nodes in South Orange County in the Baseline Condition. " +
                         "**Response is large (often >100 MB).** Use cURL, PowerBI, or another streaming client to consume it — " +
                         "Scalar's in-browser \"try it out\" cannot render responses this size and will throw an allocation error.")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public Task<IActionResult> Baseline()
    {
        return BlobFileDownloadHelper.DownloadIfExistsAsync(blobStorageService, BlobFileNames.BaselineModelResults, Response);
    }
}
