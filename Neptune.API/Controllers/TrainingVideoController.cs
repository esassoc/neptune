using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neptune.API.Services;
using Neptune.EFModels.Entities;
using Neptune.Models.DataTransferObjects;

namespace Neptune.API.Controllers
{
    [ApiController]
    [Route("training-videos")]
    public class TrainingVideoController(
        NeptuneDbContext dbContext,
        ILogger<TrainingVideoController> logger,
        IOptions<NeptuneConfiguration> neptuneConfiguration)
        : SitkaController<TrainingVideoController>(dbContext, logger, neptuneConfiguration)
    {
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<List<TrainingVideoDto>>> List()
        {
            var videos = await TrainingVideos.ListAsDtoAsync(DbContext);
            return Ok(videos);
        }
    }
}
