using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using Hangfire;
using Neptune.Models.DataTransferObjects;
using Neptune.Common.Services;
using Neptune.API.Services;
using Neptune.Common.Services;
using Neptune.API.Services.Authorization;
using Neptune.Common.Services;
using Neptune.EFModels.Entities;
using Neptune.Common.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neptune.EFModels.Workflows;
using Neptune.Common.Services;
using Neptune.Jobs.Hangfire;
using Neptune.Common.Services;
using Neptune.API.Services.Attributes;
using Neptune.Common.Services;
using Microsoft.EntityFrameworkCore;
using Neptune.Common;
using Neptune.Common.Services;
using Neptune.Common.GeoSpatial;
using Neptune.Common.Services;

namespace Neptune.API.Controllers
{
    [ApiController]
    [Route("projects")]
    public class ProjectController(
        NeptuneDbContext dbContext,
        ILogger<ProjectController> logger,
        IOptions<NeptuneConfiguration> neptuneConfiguration,
        AzureBlobStorageService azureBlobStorageService)
        : SitkaController<ProjectController>(dbContext, logger, neptuneConfiguration)
    {
        [HttpGet]
        [JurisdictionEditFeature]
        public async Task<ActionResult<List<ProjectDto>>> List()
        {
            var projectDtos = await Projects.ListByPersonIDAsDtoAsync(DbContext, CallingUser.PersonID);
            return Ok(projectDtos);
        }

        [HttpPost]
        [JurisdictionEditFeature]
        public async Task<ActionResult<ProjectDto>> Create([FromBody] ProjectUpsertDto projectCreateDto)
        {
            if (!(await CallingUser.CanEditJurisdiction(projectCreateDto.StormwaterJurisdictionID.Value, DbContext)))
            {
                return Forbid();
            }

            var projectNameAlreadyExists = DbContext.Projects.Any(x => x.ProjectName == projectCreateDto.ProjectName);
            if (projectNameAlreadyExists)
            {
                ModelState.AddModelError("ProjectName", $"A project with the name {projectCreateDto.ProjectName} already exists");
                return BadRequest(ModelState);
            }
            var project = await Projects.CreateNew(DbContext, projectCreateDto, CallingUser.PersonID);
            return Ok(project);
        }

        [HttpGet("{projectID}")]
        [EntityNotFound(typeof(Project), "projectID")]
        [UserViewFeature]
        public async Task<ActionResult<ProjectDto>> Get([FromRoute] int projectID)
        {
            var projectDto = await Projects.GetByIDAsDtoAsync(DbContext, projectID);

            if (CallingUser.IsOCTAGrantReviewer && projectDto.ShareOCTAM2Tier2Scores)
            {
                return Ok(projectDto);
            }
            if (await CallingUser.CanEditJurisdiction(projectDto.StormwaterJurisdictionID, DbContext))
            {
                return Ok(projectDto);
            }
            return Forbid();
        }

        [HttpGet("{projectID}/bounding-box")]
        [UserViewFeature]
        public async Task<ActionResult<BoundingBoxDto>> GetBoundingBoxByProjectID([FromRoute] int projectID)
        {
            var projectDto = await Projects.GetByIDAsDtoAsync(DbContext, projectID);
            var boundingBoxDto = StormwaterJurisdictions.GetBoundingBoxDtoByJurisdictionID(DbContext, projectDto.StormwaterJurisdictionID);
            return Ok(boundingBoxDto);
        }

        [HttpGet("{projectID}/progress")]
        [EntityNotFound(typeof(Project), "projectID")]
        [JurisdictionEditFeature]
        public ActionResult<ProjectWorkflowProgress.ProjectWorkflowProgressDto> GetProjectProgress([FromRoute] int projectID)
        {
            var project = Projects.GetByIDWithTrackingForWorkflow(DbContext, projectID);
            var treatmentBMPModelingAttributes = vTreatmentBMPModelingAttributes.ListAsDictionary(DbContext);
            var projectWorkflowProgressDto = ProjectWorkflowProgress.GetProgress(project, treatmentBMPModelingAttributes);
            return projectWorkflowProgressDto;
        }

        [HttpPut("{projectID}/update")]
        [EntityNotFound(typeof(Project), "projectID")]
        [JurisdictionEditFeature]
        public async Task<IActionResult> Update([FromRoute] int projectID, [FromBody] ProjectUpsertDto projectCreateDto)
        {
            if (!(await CallingUser.CanEditJurisdiction(projectCreateDto.StormwaterJurisdictionID.Value, DbContext)))
            {
                return Forbid();
            }
            var project = Projects.GetByIDWithChangeTracking(DbContext, projectID);
            await Projects.Update(DbContext, project, projectCreateDto, CallingUser.PersonID);
            return Ok();
        }

        [HttpGet("{projectID}/attachments")]
        [EntityNotFound(typeof(Project), "projectID")]
        [UserViewFeature]
        public ActionResult<List<ProjectDocumentDto>> ListAttachmentsByProjectID([FromRoute] int projectID)
        {
            var projectDocuments = ProjectDocuments.ListByProjectIDAsDto(DbContext, projectID);
            return Ok(projectDocuments);
        }

        [HttpPost("{projectID}/attachments")]
        [EntityNotFound(typeof(Project), "projectID")]
        [RequestSizeLimit(30 * 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = 30 * 1024 * 1024), ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [JurisdictionEditFeature]
        public async Task<ActionResult<ProjectDocumentDto>> AddAttachment([FromRoute] int projectID, [FromForm] ProjectDocumentUpsertDto projectDocumentUpsertDto)
        {
            var fileResource =
                await HttpUtilities.MakeFileResourceFromFormFileAsync(DbContext,
                                                                 HttpContext, azureBlobStorageService, projectDocumentUpsertDto.FileResource);

            var projectDocument = new ProjectDocument()
            {
                ProjectID = projectDocumentUpsertDto.ProjectID,
                FileResource = fileResource,
                DisplayName = projectDocumentUpsertDto.DisplayName,
                DocumentDescription = projectDocumentUpsertDto.DocumentDescription,
                UploadDate = DateOnly.FromDateTime(DateTime.UtcNow)
            };

            DbContext.ProjectDocuments.Add(projectDocument);
            await DbContext.SaveChangesAsync();
            await DbContext.Entry(projectDocument).ReloadAsync();

            return Ok(projectDocument.AsDto());
        }

        [HttpDelete("{projectID}")]
        [EntityNotFound(typeof(Project), "projectID")]
        [JurisdictionEditFeature]
        public async Task<IActionResult> Delete([FromRoute] int projectID)
        {
            var project = Projects.GetByID(DbContext, projectID);
            if (!(await CallingUser.CanEditJurisdiction(project.StormwaterJurisdictionID, DbContext)))
            {
                return Forbid();
            }
            await Projects.Delete(DbContext, projectID);
            return Ok();
        }

        [HttpGet("{projectID}/treatment-bmps")]
        [EntityNotFound(typeof(Project), "projectID")]
        [JurisdictionEditFeature]
        public async Task<ActionResult<List<TreatmentBMPDisplayDto>>> ListTreatmentBMPsByProjectID([FromRoute] int projectID)
        {
            var treatmentBMPDisplayDtos = await TreatmentBMPs.ListByProjectIDsAsDisplayDtoAsync(DbContext, [projectID]);
            return Ok(treatmentBMPDisplayDtos);
        }

        [HttpGet("{projectID}/treatment-bmps/as-upsert-dtos")]
        [UserViewFeature]
        public ActionResult<List<TreatmentBMPUpsertDto>> ListTreatmentBMPsAsUpsertDtosByProjectID([FromRoute] int projectID)
        {
            var treatmentBMPUpsertDtos = TreatmentBMPs.ListByProjectIDAsUpsertDto(DbContext, projectID);
            return Ok(treatmentBMPUpsertDtos);
        }


        [HttpPut("{projectID}/treatment-bmps/update-locations")]
        [JurisdictionEditFeature]
        public async Task<ActionResult> MergeTreatmentBMPLocations([FromRoute] int projectID, List<TreatmentBMPDisplayDto> treatmentBMPDisplayDtos)
        {
            var project = DbContext.Projects.SingleOrDefault(x => x.ProjectID == projectID);
            if (project == null)
            {
                return BadRequest();
            }

            var existingProjectTreatmentBMPs = DbContext.TreatmentBMPs.Where(x => x.ProjectID == project.ProjectID).ToList();
            foreach (var treatmentBMPDisplayDto in treatmentBMPDisplayDtos)
            {
                var treatmentBMP = existingProjectTreatmentBMPs.SingleOrDefault(x =>
                    x.TreatmentBMPID == treatmentBMPDisplayDto.TreatmentBMPID);
                if (treatmentBMP != null)
                {
                    var locationPointGeometry4326 = TreatmentBMPs.CreateLocationPoint4326FromLatLong(treatmentBMPDisplayDto.Latitude, treatmentBMPDisplayDto.Longitude);
                    var locationPoint = locationPointGeometry4326.ProjectTo2771();
                    treatmentBMP.LocationPoint = locationPoint;
                    treatmentBMP.LocationPoint4326 = locationPointGeometry4326;
                }
            }
            await DbContext.SaveChangesAsync();
            return Ok();
        }

        [HttpPut("{projectID}/treatment-bmps")]
        [JurisdictionEditFeature]
        public async Task<ActionResult> MergeTreatmentBMPs([FromRoute] int projectID, List<TreatmentBMPUpsertDto> treatmentBMPUpsertDtos)
        {
            var project = DbContext.Projects.SingleOrDefault(x => x.ProjectID == projectID);
            if (project == null)
            {
                return BadRequest();
            }

            var namingConflicts = treatmentBMPUpsertDtos.GroupBy(x => x.TreatmentBMPName).Where(x => x.Count() > 1).Select(x => x.Key).ToList();
            if (namingConflicts.Any())
            {
                ModelState.AddModelError("TreatmentBMPName",
                    $"Treatment BMP names need to be unique within a project.  The following names are used more than once: {string.Join(", ", namingConflicts)}");
            }
            else
            {
                var existingTreatmentBMPs = DbContext.TreatmentBMPs.Where(x => x.ProjectID == projectID).AsNoTracking().ToList();
                foreach (var treatmentBMPUpsertDto in treatmentBMPUpsertDtos.Where(treatmentBMPUpsertDto =>
                             existingTreatmentBMPs
                                 .Any(x => x.TreatmentBMPName == treatmentBMPUpsertDto.TreatmentBMPName &&
                                           x.TreatmentBMPID != treatmentBMPUpsertDto.TreatmentBMPID)))
                {
                    ModelState.AddModelError("TreatmentBMPName",
                        $"A Treatment BMP with the name {treatmentBMPUpsertDto.TreatmentBMPName} already exists for this project. Treatment BMP names must be unique within a project.");
                }
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            //Clearing out previous Nereid results for the project since Treatment BMPs are changing
            await Projects.DeleteProjectNereidResultsAndGrantScores(DbContext, projectID);

            await MergeTreatmentBMPs(project, treatmentBMPUpsertDtos);

            // update upsert dtos with new TreatmentBMPIDs
            var existingProjectTreatmentBMPs = DbContext.TreatmentBMPs
                .Where(x => x.ProjectID == project.ProjectID).ToList();
            foreach (var treatmentBMPUpsertDto in treatmentBMPUpsertDtos.Where(x => x.TreatmentBMPID <= 0))
            {
                var treatmentBMPID = existingProjectTreatmentBMPs
                    .Single(x => x.TreatmentBMPName == treatmentBMPUpsertDto.TreatmentBMPName).TreatmentBMPID;
                treatmentBMPUpsertDto.TreatmentBMPID = treatmentBMPID;
                foreach (var modelingAttribute in treatmentBMPUpsertDto.ModelingAttributes)
                {
                    modelingAttribute.TreatmentBMPID = treatmentBMPID;
                }
            }

            await MergeCustomAttributes(projectID, treatmentBMPUpsertDtos);

            await CascadeDeleteTreatmentBMPsNoLongerAssociatedToProject(projectID, treatmentBMPUpsertDtos);

            return Ok();
        }

        private async Task MergeTreatmentBMPs(Project project, List<TreatmentBMPUpsertDto> treatmentBMPUpsertDtos)
        {
            var updatedTreatmentBMPs = treatmentBMPUpsertDtos.Select(x => TreatmentBMPs.TreatmentBMPFromUpsertDtoAndProject(DbContext, x, project)).ToList();

            await DbContext.TreatmentBMPs.AddRangeAsync(updatedTreatmentBMPs.Where(x => x.TreatmentBMPID <= 0));
            await DbContext.SaveChangesAsync();

            // Update existing Treatment BMPs
            var existingProjectTreatmentBMPs = DbContext.TreatmentBMPs
                .Where(x => x.ProjectID == project.ProjectID).ToList();
            existingProjectTreatmentBMPs.MergeUpdate(updatedTreatmentBMPs,
                (x, y) => x.TreatmentBMPID == y.TreatmentBMPID,
                (x, y) =>
                {
                    x.TreatmentBMPName = y.TreatmentBMPName;
                    x.LocationPoint4326 = y.LocationPoint4326;
                    x.LocationPoint = y.LocationPoint;
                    x.WatershedID = y.WatershedID;
                    x.ModelBasinID = y.ModelBasinID;
                    x.PrecipitationZoneID = y.PrecipitationZoneID;
                    x.RegionalSubbasinID = y.RegionalSubbasinID;
                    x.Notes = y.Notes;
                });

            // if the location of a Treatment BMP changed, we need to delete any existing centralized delineations for that Treatment BMP
            var treatmentBMPsWhoseLocationChanged = existingProjectTreatmentBMPs
                .Where(x => updatedTreatmentBMPs.Any(y =>
                    x.TreatmentBMPID == y.TreatmentBMPID && (!x.LocationPoint4326.Equals(y.LocationPoint4326))))
                .Select(x => x.TreatmentBMPID).ToList();

            if (treatmentBMPsWhoseLocationChanged.Any())
            {
                await DbContext.Delineations
                    .Where(x => x.DelineationTypeID == (int)DelineationTypeEnum.Centralized &&
                                treatmentBMPsWhoseLocationChanged.Contains(x.TreatmentBMPID)).ExecuteDeleteAsync();
            }

            await DbContext.SaveChangesAsync();
        }

        private async Task MergeCustomAttributes(int projectID, List<TreatmentBMPUpsertDto> treatmentBMPUpsertDtos)
        {
            //Purge all Custom Attribute values that pertain to modeling attributes for this project
            await DbContext.CustomAttributeValues.Include(x => x.CustomAttribute).ThenInclude(x => x.TreatmentBMP)
                .Include(x => x.CustomAttribute)
                .ThenInclude(x => x.CustomAttributeType)
                .Where(x => x.CustomAttribute.TreatmentBMP.ProjectID == projectID &&
                            x.CustomAttribute.CustomAttributeType.CustomAttributeTypePurposeID ==
                            (int)CustomAttributeTypePurposeEnum.Modeling).ExecuteDeleteAsync();


            var existingProjectTreatmentBMPModelingAttributes = DbContext.CustomAttributes
                .Include(x => x.TreatmentBMP)
                .Where(x => x.TreatmentBMP.ProjectID == projectID).ToList();

            //Get all Custom Attribute Types so we can validate values against their data types
            var allCustomAttributeTypes = CustomAttributeTypes.List(DbContext);
            var customAttributeUpsertDtos = treatmentBMPUpsertDtos.SelectMany(x => x.ModelingAttributes).Where(x => x.CustomAttributeValues != null && x.CustomAttributeValues.Count > 0).ToList();
            var customAttributeValuesToUpdate = new List<CustomAttributeValue>();
            foreach (var customAttributeUpsertDto in customAttributeUpsertDtos)
            {
                var customAttribute = existingProjectTreatmentBMPModelingAttributes.SingleOrDefault(x =>
                    x.CustomAttributeID == customAttributeUpsertDto.CustomAttributeID);
                if (customAttribute == null)
                {
                    customAttribute = new CustomAttribute()
                    {
                        TreatmentBMPID = customAttributeUpsertDto.TreatmentBMPID.Value,
                        TreatmentBMPTypeCustomAttributeTypeID = customAttributeUpsertDto.TreatmentBMPTypeCustomAttributeTypeID,
                        TreatmentBMPTypeID = customAttributeUpsertDto.TreatmentBMPTypeID.Value,
                        CustomAttributeTypeID = customAttributeUpsertDto.CustomAttributeTypeID
                    };
                    DbContext.CustomAttributes.Add(customAttribute);
                }

                foreach (var value in customAttributeUpsertDto.CustomAttributeValues)
                {
                    //Ensure that the value can be parsed for the required data type
                    var valueParsedForDataType = allCustomAttributeTypes.Single(x => x.CustomAttributeTypeID == customAttributeUpsertDto.CustomAttributeTypeID).CustomAttributeDataType.ValueParsedForDataType(value);
                    var customAttributeValue = new CustomAttributeValue
                    {
                        CustomAttribute = customAttribute,
                        AttributeValue = valueParsedForDataType
                    };
                    customAttributeValuesToUpdate.Add(customAttributeValue);
                }
            }

            var bmpIds = customAttributeUpsertDtos.Select(y => y.TreatmentBMPID).ToHashSet();
            var typeIds = customAttributeUpsertDtos.Select(y => y.TreatmentBMPTypeCustomAttributeTypeID).ToHashSet();

            await DbContext.CustomAttributes
                .Where(x => x.TreatmentBMP.ProjectID == projectID &&
                            (!bmpIds.Contains(x.TreatmentBMPID) ||
                             !typeIds.Contains(x.TreatmentBMPTypeCustomAttributeTypeID)))
                .ExecuteDeleteAsync();
            
            DbContext.AddRange(customAttributeValuesToUpdate);
            await DbContext.SaveChangesAsync();
        }

        private async Task CascadeDeleteTreatmentBMPsNoLongerAssociatedToProject(int projectID, List<TreatmentBMPUpsertDto> updatedTreatmentBMPs)
        {
            var existingProjectTreatmentBMPs = DbContext.TreatmentBMPs
                .Where(x => x.ProjectID == projectID).ToList();

            var treatmentBMPIDsWhoAreBeingDeleted = existingProjectTreatmentBMPs.Select(x => x.TreatmentBMPID)
                .Where(x => updatedTreatmentBMPs.All(y => x != y.TreatmentBMPID)).ToList();
            // delete all the Delineation related entities
            await DbContext.ProjectHRUCharacteristics.Include(x => x.ProjectLoadGeneratingUnit)
                .ThenInclude(x => x.Delineation).Where(x =>
                    x.ProjectLoadGeneratingUnit.DelineationID.HasValue &&
                    treatmentBMPIDsWhoAreBeingDeleted.Contains(x.ProjectLoadGeneratingUnit.Delineation
                        .TreatmentBMPID))
                .ExecuteDeleteAsync();
            await DbContext.ProjectLoadGeneratingUnits.Include(x => x.Delineation)
                .Where(x => x.DelineationID.HasValue &&
                            treatmentBMPIDsWhoAreBeingDeleted.Contains(x.Delineation.TreatmentBMPID))
                .ExecuteDeleteAsync();
            await DbContext.DelineationOverlaps
                .Include(x => x.Delineation).Include(x => x.OverlappingDelineation).Where(x =>
                    treatmentBMPIDsWhoAreBeingDeleted.Contains(x.Delineation.TreatmentBMPID) ||
                    treatmentBMPIDsWhoAreBeingDeleted.Contains(x.OverlappingDelineation.TreatmentBMPID))
                .ExecuteDeleteAsync();
            await DbContext.Delineations.Where(x => treatmentBMPIDsWhoAreBeingDeleted.Contains(x.TreatmentBMPID))
                .ExecuteDeleteAsync();
            await DbContext.TreatmentBMPModelingAttributes
                .Where(x => treatmentBMPIDsWhoAreBeingDeleted.Contains(x.TreatmentBMPID))
                .ExecuteDeleteAsync();
            await DbContext.TreatmentBMPs.Where(x => treatmentBMPIDsWhoAreBeingDeleted.Contains(x.TreatmentBMPID))
                .ExecuteDeleteAsync();
        }

        [HttpGet("{projectID}/project-network-solve-histories")]
        [EntityNotFound(typeof(Project), "projectID")]
        [UserViewFeature]
        public async Task<ActionResult<List<ProjectNetworkSolveHistorySimpleDto>>> ListProjectNetworkSolveHistoriesForProject([FromRoute] int projectID)
        {
            var project = Projects.GetByID(DbContext, projectID);
            if ((!CallingUser.IsOCTAGrantReviewer || !project.ShareOCTAM2Tier2Scores) && !(await CallingUser.CanEditJurisdiction(project.StormwaterJurisdictionID, DbContext)))
            {
                return Forbid();
            }

            var projectNetworkSolveHistoryDtos = ProjectNetworkSolveHistories.GetByProjectIDAsDto(DbContext, projectID);
            return Ok(projectNetworkSolveHistoryDtos);
        }

        [HttpGet("{projectID}/treatment-bmp-hru-characteristics")]
        [EntityNotFound(typeof(Project), "projectID")]
        [UserViewFeature]
        public async Task<ActionResult<List<TreatmentBMPHRUCharacteristicsSummarySimpleDto>>> ListTreatmentBMPHRUCharacteristicsForProject([FromRoute] int projectID)
        {
            var project = Projects.GetByID(DbContext, projectID);
            if ((!CallingUser.IsOCTAGrantReviewer || !project.ShareOCTAM2Tier2Scores) && !(await CallingUser.CanEditJurisdiction(project.StormwaterJurisdictionID, DbContext)))
            {
                return Forbid();
            }

            var hruCharacteristics = Projects.GetTreatmentBMPHRUCharacteristicSimplesForProject(DbContext, projectID);

            return Ok(hruCharacteristics);
        }

        [HttpGet("{projectID}/load-reducing-results")]
        [EntityNotFound(typeof(Project), "projectID")]
        [UserViewFeature]
        public async Task<ActionResult<List<ProjectLoadReducingResultDto>>> ListLoadReducingResultsForProject([FromRoute] int projectID)
        {
            var project = Projects.GetByID(DbContext, projectID);
            if ((!CallingUser.IsOCTAGrantReviewer || !project.ShareOCTAM2Tier2Scores) && !(await CallingUser.CanEditJurisdiction(project.StormwaterJurisdictionID, DbContext)))
            {
                return Forbid();
            }

            var modeledResults = ProjectLoadReducingResults.ListByProjectIDAsDto(DbContext, projectID);

            return Ok(modeledResults);
        }

        [HttpPost("{projectID}/modeled-performance")]
        [EntityNotFound(typeof(Project), "projectID")]
        [JurisdictionEditFeature]
        public async Task<IActionResult> TriggerModeledPerformanceForProject([FromRoute] int projectID)
        {
            var project = Projects.GetByIDWithChangeTracking(DbContext, projectID);
            if (!(await CallingUser.CanEditJurisdiction(project.StormwaterJurisdictionID, DbContext)))
            {
                return Forbid();
            }

            var projectNetworkSolveHistoryEntity = new ProjectNetworkSolveHistory
            {
                ProjectID = projectID,
                RequestedByPersonID = CallingUser.PersonID,
                ProjectNetworkSolveHistoryStatusTypeID = (int)ProjectNetworkSolveHistoryStatusTypeEnum.Queued,
                LastUpdated = DateTime.UtcNow
            };
            await DbContext.ProjectNetworkSolveHistories.AddAsync(projectNetworkSolveHistoryEntity);
            await DbContext.SaveChangesAsync();

            BackgroundJob.Enqueue<ProjectNetworkSolveJob>(x => x.RunNetworkSolveForProject(projectID, projectNetworkSolveHistoryEntity.ProjectNetworkSolveHistoryID));
            return Ok();
        }

        [HttpGet("{projectID}/delineations")]
        [EntityNotFound(typeof(Project), "projectID")]
        [UserViewFeature]
        public ActionResult<List<DelineationUpsertDto>> ListDelineationsByProjectID([FromRoute] int projectID)
        {
            var delineationUpsertDtos = Delineations.ListByProjectIDAsUpsertDto(DbContext, projectID);
            return Ok(delineationUpsertDtos);
        }

        [HttpPut("{projectID}/delineations")]
        [EntityNotFound(typeof(Project), "projectID")]
        [JurisdictionEditFeature]
        public async Task<IActionResult> MergeDelineationsForProject([FromRoute] int projectID, List<DelineationUpsertDto> delineationUpsertDtos)
        {
            await Projects.DeleteProjectNereidResultsAndGrantScores(DbContext, projectID);
            await Delineations.MergeDelineations(DbContext, delineationUpsertDtos, projectID);
            Projects.SetUpdatePersonAndDate(DbContext, projectID, CallingUser.PersonID);
            return Ok();
        }

        [HttpPost("{projectID}/copy")]
        [EntityNotFound(typeof(Project), "projectID")]
        [JurisdictionEditFeature]
        public async Task<ActionResult<int>> CreateProjectCopy([FromRoute] int projectID)
        {
            var project = Projects.GetByIDWithChangeTracking(DbContext, projectID);
            if (!(await CallingUser.CanEditJurisdiction(project.StormwaterJurisdictionID, DbContext)))
            {
                return Forbid();
            }

            var newProject = await Projects.CreateCopy(DbContext, project, CallingUser.PersonID);
            return Ok(newProject.ProjectID);
        }

        [HttpGet("download")]
        [UserViewFeature]
        [Produces(@"text/csv")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FileStreamResult))]
        public async Task<FileContentResult> DownloadProjectsWithModels()
        {
            var projectIDs = Projects.ListProjectIDs(DbContext);
            var records = Projects.ListByIDsAsModeledResultSummaryDtos(DbContext, projectIDs);

            await using var stream = new MemoryStream();
            await using var writer = new StreamWriter(stream);
            await using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            csv.Context.RegisterClassMap<ProjectModeledResultsSummaryMap>();

            await csv.WriteRecordsAsync(records);
            await csv.FlushAsync();
            stream.Seek(0, SeekOrigin.Begin);

            return File(stream.ToArray(), "text/csv");
        }

        [HttpGet("treatmentBMPs/download")]
        [UserViewFeature]
        [Produces(@"text/csv")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FileStreamResult))]
        public async Task<FileContentResult> DownloadTreatmentBMPsForProjects()
        {
            var projectIDs = Projects.ListProjectIDs(DbContext);
            var records = ProjectLoadReducingResults.ListByProjectIDs(DbContext, projectIDs);

            await using var stream = new MemoryStream();
            await using var writer = new StreamWriter(stream);
            await using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            csv.Context.RegisterClassMap<ProjectLoadRemovingResultMap>();

            await csv.WriteRecordsAsync(records);
            await csv.FlushAsync();
            stream.Seek(0, SeekOrigin.Begin);

            return File(stream.ToArray(), "text/csv");
        }

        [HttpGet("OCTAM2Tier2GrantProgram")]
        [UserViewFeature]
        public async Task<ActionResult<List<ProjectDto>>> ListProjectsSharedWithOCTAM2Tier2GrantProgram()
        {
            if (!CallingUser.IsOCTAGrantReviewer)
            {
                return Forbid();
            }

            var projectDtos = await Projects.ListOCTAM2Tier2AsDtoAsync(DbContext);
            return Ok(projectDtos);
        }

        [HttpGet("OCTAM2Tier2GrantProgram/download")]
        [UserViewFeature]
        [Produces(@"text/csv")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FileStreamResult))]
        public async Task<FileContentResult> DownloadProjectsSharedWithOCTAM2Tier2GrantProgram()
        {
            var projectIDs = Projects.ListOCTAM2Tier2ProjectIDs(DbContext);
            var records = Projects.ListByIDsAsModeledResultSummaryDtos(DbContext, projectIDs);

            await using var stream = new MemoryStream();
            await using var writer = new StreamWriter(stream);
            await using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            csv.Context.RegisterClassMap<ProjectModeledResultsSummaryMap>();

            await csv.WriteRecordsAsync(records);
            await csv.FlushAsync();
            stream.Seek(0, SeekOrigin.Begin);

            return File(stream.ToArray(), "text/csv");
        }

        [HttpGet("OCTAM2Tier2GrantProgram/treatmentBMPs/download")]
        [UserViewFeature]
        [Produces(@"text/csv")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FileStreamResult))]
        public async Task<FileContentResult> DownloadTreatmentBMPsForProjectsSharedWithOCTAM2Tier2GrantProgram()
        {
            var projectIDs = Projects.ListOCTAM2Tier2ProjectIDs(DbContext);
            var records = ProjectLoadReducingResults.ListByProjectIDs(DbContext, projectIDs);

            await using var stream = new MemoryStream();
            await using var writer = new StreamWriter(stream);
            await using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            csv.Context.RegisterClassMap<ProjectLoadRemovingResultMap>();

            await csv.WriteRecordsAsync(records);
            await csv.FlushAsync();
            stream.Seek(0, SeekOrigin.Begin);

            return File(stream.ToArray(), "text/csv");
        }

        [HttpGet("delineations")]
        [UserViewFeature]
        public ActionResult<List<DelineationDto>> ListDelineations()
        {
            if (!CallingUser.IsOCTAGrantReviewer)
            {
                return Forbid();
            }
            var dtos = Delineations.ListProjectDelineationsAsDto(DbContext);
            return Ok(dtos);
        }

    }
}