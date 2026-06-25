using Neptune.Models.DataTransferObjects;
using Microsoft.EntityFrameworkCore;
using Neptune.Common.DesignByContract;

namespace Neptune.EFModels.Entities
{
    public static class Projects
    {
        private static void ResolveClientSideLookups(ProjectDto dto)
        {
            if (ProjectStatus.AllLookupDictionary.TryGetValue(dto.ProjectStatusID, out var projectStatus))
            {
                dto.ProjectStatus = projectStatus.AsSimpleDto();
            }
            ResolvePersonRoleName(dto.PrimaryContactPerson);
            ResolvePersonRoleName(dto.CreatePerson);
        }

        private static void ResolveClientSideLookups(List<ProjectDto> dtos)
        {
            foreach (var dto in dtos)
            {
                ResolveClientSideLookups(dto);
            }
        }

        private static void ResolvePersonRoleName(PersonSimpleDto personDto)
        {
            if (personDto != null && Role.AllLookupDictionary.TryGetValue(personDto.RoleID, out var role))
            {
                personDto.RoleName = role.RoleDisplayName;
            }
        }

        public static async Task<List<ProjectDto>> ListAsDtoAsync(NeptuneDbContext dbContext)
        {
            var dtos = await dbContext.Projects
                .AsNoTracking()
                .OrderByDescending(x => x.ProjectID)
                .Select(ProjectDtoProjections.AsDto)
                .ToListAsync();
            ResolveClientSideLookups(dtos);
            return dtos;
        }

        public static Project GetByIDWithChangeTracking(NeptuneDbContext dbContext, int projectID)
        {
            var project = dbContext.Projects
                .SingleOrDefault(x => x.ProjectID == projectID);
            Check.RequireNotNull(project, $"Project with ID {projectID} not found!");
            return project;
        }

        public static Project GetByIDWithTrackingForWorkflow(NeptuneDbContext dbContext, int projectID)
        {
            return dbContext.Projects
                .Include(x => x.Organization)
                .Include(x => x.StormwaterJurisdiction)
                .Include(x => x.TreatmentBMPs)
                .ThenInclude(x => x.Delineation)
                .Include(x => x.TreatmentBMPs)
                .Include(x => x.TreatmentBMPs)
                .ThenInclude(x => x.TreatmentBMPType)
                .Include(x => x.ProjectNereidResults)
                .Single(x => x.ProjectID == projectID);
        }

        public static Project GetByID(NeptuneDbContext dbContext, int projectID)
        {
            var project = dbContext.Projects.AsNoTracking()
                .SingleOrDefault(x => x.ProjectID == projectID);
            Check.RequireNotNull(project, $"Project with ID {projectID} not found!");
            return project;
        }

        public static async Task<ProjectDto> GetByIDAsDtoAsync(NeptuneDbContext dbContext, int projectID)
        {
            var dto = await dbContext.Projects
                .AsNoTracking()
                .Where(x => x.ProjectID == projectID)
                .Select(ProjectDtoProjections.AsDto)
                .SingleOrDefaultAsync();
            Check.RequireNotNull(dto, $"Project with ID {projectID} not found!");
            ResolveClientSideLookups(dto);
            return dto;
        }

        public static List<int> ListProjectIDs(NeptuneDbContext dbContext)
        {
            return dbContext.Projects.AsNoTracking().Select(x => x.ProjectID).ToList();
        }

        public static async Task<List<ProjectDto>> ListByPersonIDAsDtoAsync(NeptuneDbContext dbContext, int personID)
        {
            var person = People.GetByID(dbContext, personID);
            if (person.RoleID is (int)RoleEnum.Admin or (int)RoleEnum.SitkaAdmin)
            {
                return await ListAsDtoAsync(dbContext);
            }

            var jurisdictionIDs = await StormwaterJurisdictionPeople.ListViewableStormwaterJurisdictionIDsByPersonIDForBMPsAsync(dbContext, personID);

            var dtos = await dbContext.Projects
                .AsNoTracking()
                .Where(x => jurisdictionIDs.Contains(x.StormwaterJurisdictionID))
                .OrderByDescending(x => x.ProjectID)
                .Select(ProjectDtoProjections.AsDto)
                .ToListAsync();
            ResolveClientSideLookups(dtos);
            return dtos;
        }

        public static async Task<ProjectDto> CreateNew(NeptuneDbContext dbContext, ProjectUpsertDto projectUpsertDto, int personID)
        {
            var project = new Project()
            {
                ProjectName = projectUpsertDto.ProjectName,
                OrganizationID = projectUpsertDto.OrganizationID.Value,
                StormwaterJurisdictionID = projectUpsertDto.StormwaterJurisdictionID.Value,
                ProjectStatusID = (int)ProjectStatusEnum.Draft,
                PrimaryContactPersonID = projectUpsertDto.PrimaryContactPersonID.Value,
                CreatePersonID = personID,
                DateCreated = DateTime.UtcNow,
                ProjectDescription = projectUpsertDto.ProjectDescription,
                AdditionalContactInformation = projectUpsertDto.AdditionalContactInformation,
                DoesNotIncludeTreatmentBMPs = false,
                CalculateOCTAM2Tier2Scores = (projectUpsertDto.CalculateOCTAM2Tier2Scores ?? false),
                ShareOCTAM2Tier2Scores = false
            };
            await dbContext.Projects.AddAsync(project);
            await dbContext.SaveChangesAsync();
            await dbContext.Entry(project).ReloadAsync();
            return await GetByIDAsDtoAsync(dbContext, project.ProjectID);
        }

        public static async Task Update(NeptuneDbContext dbContext, Project project, ProjectUpsertDto projectUpsertDto, int personID)
        {
            project.ProjectName = projectUpsertDto.ProjectName;
            project.OrganizationID = projectUpsertDto.OrganizationID.Value;
            project.StormwaterJurisdictionID = projectUpsertDto.StormwaterJurisdictionID.Value;
            project.PrimaryContactPersonID = projectUpsertDto.PrimaryContactPersonID.Value;
            project.ProjectDescription = projectUpsertDto.ProjectDescription;
            project.AdditionalContactInformation = projectUpsertDto.AdditionalContactInformation;
            project.DoesNotIncludeTreatmentBMPs = (projectUpsertDto.DoesNotIncludeTreatmentBMPs ?? false);
            project.CalculateOCTAM2Tier2Scores = (projectUpsertDto.CalculateOCTAM2Tier2Scores ?? false);
            project.UpdatePersonID = personID;
            project.DateUpdated = DateTime.UtcNow;

            if ((projectUpsertDto.ShareOCTAM2Tier2Scores ?? false) && !project.ShareOCTAM2Tier2Scores)
            {
                project.OCTAM2Tier2ScoresLastSharedDate = DateTime.UtcNow;
            }
            project.ShareOCTAM2Tier2Scores = (projectUpsertDto.ShareOCTAM2Tier2Scores ?? false);

            //If we opt to not include treatmentBMPs, ensure we get rid of our pre-existing treatment bmps
            if (project.DoesNotIncludeTreatmentBMPs)
            {
                foreach (var treatmentBMP in dbContext.TreatmentBMPs.Where(x => x.ProjectID == project.ProjectID)
                             .ToList())
                {
                    await treatmentBMP.DeleteFull(dbContext);
                }
            }

            await dbContext.SaveChangesAsync();
        }

        public static async Task Delete(NeptuneDbContext dbContext, int projectID)
        {
            await dbContext.ProjectHRUCharacteristics.Where(x => x.ProjectID == projectID).ExecuteDeleteAsync();
            await dbContext.ProjectLoadGeneratingUnits.Where(x => x.ProjectID == projectID).ExecuteDeleteAsync();
            await dbContext.ProjectNereidResults.Where(x => x.ProjectID == projectID).ExecuteDeleteAsync();
            await dbContext.ProjectNetworkSolveHistories.Where(x => x.ProjectID == projectID).ExecuteDeleteAsync();
            await dbContext.DelineationOverlaps.Include(x => x.Delineation).ThenInclude(x => x.TreatmentBMP).Where(x => x.Delineation.TreatmentBMP.ProjectID == projectID).ExecuteDeleteAsync();
            await dbContext.Delineations.Include(x => x.TreatmentBMP).Where(x => x.TreatmentBMP.ProjectID == projectID).ExecuteDeleteAsync();
            await dbContext.TreatmentBMPModelingAttributes.Include(x => x.TreatmentBMP).Where(x => x.TreatmentBMP.ProjectID == projectID).ExecuteDeleteAsync();
            await dbContext.TreatmentBMPs.Where(x => x.ProjectID == projectID).ExecuteDeleteAsync();
            await dbContext.Projects.Where(x => x.ProjectID == projectID).ExecuteDeleteAsync();
        }

        public static async Task DeleteProjectNereidResultsAndGrantScores(NeptuneDbContext dbContext, int projectID)
        {
            await dbContext.ProjectNereidResults.Where(x => x.ProjectID == projectID).ExecuteDeleteAsync();

            var project = dbContext.Projects.Single(x => x.ProjectID == projectID);
            project.OCTAWatersheds = null;
            project.PollutantVolume = null;
            project.PollutantMetals = null;
            project.PollutantBacteria = null;
            project.PollutantNutrients = null;
            project.PollutantTSS = null;
            project.TPI = null;
            project.SEA = null;
            project.DryWeatherWQLRI = null;
            project.WetWeatherWQLRI = null;

            await dbContext.SaveChangesAsync();
        }

        public static async Task<Project> CreateCopy(NeptuneDbContext dbContext, Project projectToCopy, int createPersonID)
        {
            var dateCreated = DateTime.UtcNow;

            var newProject = new Project()
            {
                ProjectName = $"{projectToCopy.ProjectName} - Copy {dateCreated}",
                OrganizationID = projectToCopy.OrganizationID,
                StormwaterJurisdictionID = projectToCopy.StormwaterJurisdictionID,
                ProjectStatusID = (int)ProjectStatusEnum.Draft,
                PrimaryContactPersonID = createPersonID,
                CreatePersonID = createPersonID,
                DateCreated = dateCreated,
                ProjectDescription = projectToCopy.ProjectDescription,
                AdditionalContactInformation = projectToCopy.AdditionalContactInformation,
                DoesNotIncludeTreatmentBMPs = projectToCopy.DoesNotIncludeTreatmentBMPs,
                CalculateOCTAM2Tier2Scores = projectToCopy.CalculateOCTAM2Tier2Scores,
                ShareOCTAM2Tier2Scores = false
            };

            await dbContext.Projects.AddAsync(newProject);
            await dbContext.SaveChangesAsync();

            var treatmentBMPsToCopy = dbContext.TreatmentBMPs.Where(x => x.ProjectID == projectToCopy.ProjectID).ToList();

            var newTreatmentBMPs = treatmentBMPsToCopy.Select(x =>
            {
                var treatmentBMP = new TreatmentBMP()
                {
                    ProjectID = newProject.ProjectID,
                    TreatmentBMPName = $"{x.TreatmentBMPName} - Copy {dateCreated}",
                    TreatmentBMPTypeID = x.TreatmentBMPTypeID,
                    LocationPoint = x.LocationPoint,
                    StormwaterJurisdictionID = x.StormwaterJurisdictionID,
                    Notes = x.Notes,
                    OwnerOrganizationID = x.OwnerOrganizationID,
                    InventoryIsVerified = x.InventoryIsVerified,
                    LocationPoint4326 = x.LocationPoint4326,
                    TrashCaptureStatusTypeID = x.TrashCaptureStatusTypeID,
                    SizingBasisTypeID = x.SizingBasisTypeID,
                    WatershedID = x.WatershedID,
                    ModelBasinID = x.ModelBasinID,
                    PrecipitationZoneID = x.PrecipitationZoneID,
                    RegionalSubbasinID = x.RegionalSubbasinID,
                };

                return treatmentBMP;
            }).ToList();

            await dbContext.TreatmentBMPs.AddRangeAsync(newTreatmentBMPs);
            await dbContext.SaveChangesAsync();

            var newTreatmentBMPIDsByCopiedTreatmentBMPIDs = treatmentBMPsToCopy
                .Select(x => new
                {
                    copiedTreatmentBMPID = x.TreatmentBMPID,
                    newTreatmentBMpID = newTreatmentBMPs.Single(y => y.TreatmentBMPName.StartsWith(x.TreatmentBMPName)).TreatmentBMPID
                }).ToDictionary(x => x.copiedTreatmentBMPID, x => x.newTreatmentBMpID);

            var treatmentBMPIDsToCopy = treatmentBMPsToCopy.Select(x => x.TreatmentBMPID).ToList();

            var customAttributesToCopy = dbContext.CustomAttributes
                .Include(x => x.CustomAttributeType)
                .Where(x => treatmentBMPIDsToCopy.Contains(x.TreatmentBMPID) && x.CustomAttributeType.CustomAttributeTypePurposeID == (int)CustomAttributeTypePurposeEnum.Modeling).ToList();
            var newCustomAttributes = customAttributesToCopy.Select(x => new CustomAttribute()
                {
                    TreatmentBMPID = newTreatmentBMPIDsByCopiedTreatmentBMPIDs[x.TreatmentBMPID],
                    CustomAttributeTypeID = x.CustomAttributeTypeID,
                    TreatmentBMPTypeID = x.TreatmentBMPTypeID,
                    TreatmentBMPTypeCustomAttributeTypeID = x.TreatmentBMPTypeCustomAttributeTypeID
                }).ToList();

            await dbContext.CustomAttributes.AddRangeAsync(newCustomAttributes);
            await dbContext.SaveChangesAsync();

            var newCustomAttributeIDsByCopiedCustomAttributeIDs = customAttributesToCopy
                .Select(x => new
                {
                    copiedCustomAttributeID = x.CustomAttributeID,
                    newCustomAttributeID = newCustomAttributes.Single(y => newTreatmentBMPIDsByCopiedTreatmentBMPIDs[x.TreatmentBMPID] == y.TreatmentBMPID && x.CustomAttributeTypeID == y.CustomAttributeTypeID && x.TreatmentBMPTypeID == y.TreatmentBMPTypeID && x.TreatmentBMPTypeCustomAttributeTypeID == y.TreatmentBMPTypeCustomAttributeTypeID).CustomAttributeID
                }).ToDictionary(x => x.copiedCustomAttributeID, x => x.newCustomAttributeID);

            var customAttributeIDsToCopy = customAttributesToCopy.Select(x => x.CustomAttributeID).ToList();

            var newCustomAttributeValues = dbContext.CustomAttributeValues
                .Where(x => customAttributeIDsToCopy.Contains(x.CustomAttributeID))
                .Select(x => new CustomAttributeValue()
                {
                    CustomAttributeID = newCustomAttributeIDsByCopiedCustomAttributeIDs[x.CustomAttributeID],
                    AttributeValue = x.AttributeValue
                });

            await dbContext.CustomAttributeValues.AddRangeAsync(newCustomAttributeValues);
            await dbContext.SaveChangesAsync();

            var newDelineations = dbContext.Delineations
                .Where(x => treatmentBMPIDsToCopy.Contains(x.TreatmentBMPID)).AsEnumerable()

                .Select(x => new Delineation()
                {
                    TreatmentBMPID = newTreatmentBMPIDsByCopiedTreatmentBMPIDs[x.TreatmentBMPID],
                    DelineationGeometry = x.DelineationGeometry,
                    DelineationGeometry4326 = x.DelineationGeometry4326,
                    DelineationTypeID = x.DelineationTypeID,
                    IsVerified = x.IsVerified,
                    VerifiedByPersonID = x.VerifiedByPersonID,
                    DateLastModified = DateTime.UtcNow,
                    HasDiscrepancies = x.HasDiscrepancies
                }).ToList();

            await dbContext.Delineations.AddRangeAsync(newDelineations);
            await dbContext.SaveChangesAsync();

            return newProject;
        }

        public static List<TreatmentBMPHRUCharacteristicsSummarySimpleDto> GetTreatmentBMPHRUCharacteristicSimplesForProject(NeptuneDbContext dbContext, int projectID)
        {
            var projectTreatmentBMPs = dbContext.Projects
                .Include(x => x.TreatmentBMPs)
                .ThenInclude(x => x.Delineation)
                .Include(x => x.TreatmentBMPs)
                .ThenInclude(x => x.TreatmentBMPType)
                .Where(x => x.ProjectID == projectID).SelectMany(x => x.TreatmentBMPs).ToList();

            if (!projectTreatmentBMPs.Any())
            {
                return new List<TreatmentBMPHRUCharacteristicsSummarySimpleDto>();
            }

            return projectTreatmentBMPs.SelectMany(x =>
                x.GetHRUCharacteristics(dbContext)
                .ToList()
                .Select(y => new TreatmentBMPHRUCharacteristicsSummarySimpleDto()
                {
                    ProjectHRUCharacteristicID = y.ProjectHRUCharacteristicID,
                    TreatmentBMPID = x.TreatmentBMPID,
                    Area = y.Area,
                    ImperviousCover = y.ImperviousAcres,
                    LandUse = y.HRUCharacteristicLandUseCode.HRUCharacteristicLandUseCodeDisplayName
                })
                .ToList()
            )
            .ToList();

        }

        public static async Task<List<ProjectDto>> ListOCTAM2Tier2AsDtoAsync(NeptuneDbContext dbContext)
        {
            var dtos = await dbContext.Projects
                .AsNoTracking()
                .Where(x => x.ShareOCTAM2Tier2Scores)
                .Select(ProjectDtoProjections.AsDto)
                .ToListAsync();
            ResolveClientSideLookups(dtos);
            return dtos;
        }

        public static List<int> ListOCTAM2Tier2ProjectIDs(NeptuneDbContext dbContext)
        {
            return dbContext.Projects.AsNoTracking().Where(x => x.ShareOCTAM2Tier2Scores).Select(x => x.ProjectID).ToList();
        }

        public static List<ProjectModeledResultSummaryDto> ListByIDsAsModeledResultSummaryDtos(NeptuneDbContext dbContext, List<int> projectIDs)
        {
            var projects = dbContext.Projects.Where(x => projectIDs.Contains(x.ProjectID)).ToList();
            var projectLoadReducingResultsGroups = ProjectLoadReducingResults
                .ListByProjectIDs(dbContext, projectIDs)
                .GroupBy(x => x.ProjectID);

            var allProjectHRUCharacteristics = dbContext.ProjectHRUCharacteristics
                .Where(x => projectIDs.Contains(x.ProjectID)).ToList();
            var projectModeledResultSummaryDtos = new List<ProjectModeledResultSummaryDto>();

            foreach (var projectLoadReducingResultsGroup in projectLoadReducingResultsGroups)
            {
                var projectID = projectLoadReducingResultsGroup.Key;
                var project = projects.SingleOrDefault(x => x.ProjectID == projectID);
                var projectHRUCharacteristics = allProjectHRUCharacteristics.Where(x => x.ProjectID == projectID).ToList();
                var area = projectHRUCharacteristics.Any() ? projectHRUCharacteristics.Sum(x => x.Area) : (double?)null;
                var imperviousAcres = projectHRUCharacteristics.Any() ? projectHRUCharacteristics.Sum(x => x.ImperviousAcres) : (double?)null;

                var projectModeledResultSummaryDto = new ProjectModeledResultSummaryDto()
                {
                    ProjectName = projectLoadReducingResultsGroup.First().ProjectName,
                    Jurisdiction = projectLoadReducingResultsGroup.First().JurisdictionName,
                    Organization = projectLoadReducingResultsGroup.First().OrganizationName,
                    AcresTreated = area ?? 0,
                    ImperviousAcresTreated = imperviousAcres ?? 0,

                    WetWeatherInflow = projectLoadReducingResultsGroup.Sum(x => x.WetWeatherInflow ?? 0),
                    WetWeatherTreated = projectLoadReducingResultsGroup.Sum(x => x.WetWeatherTreated ?? 0),
                    WetWeatherRetained = projectLoadReducingResultsGroup.Sum(x => x.WetWeatherRetained ?? 0),
                    WetWeatherUntreated = projectLoadReducingResultsGroup.Sum(x => x.WetWeatherUntreated ?? 0),
                    WetWeatherTSSReduced = projectLoadReducingResultsGroup.Sum(x => x.WetWeatherTSSReduced ?? 0),
                    WetWeatherTNReduced = projectLoadReducingResultsGroup.Sum(x => x.WetWeatherTNReduced ?? 0),
                    WetWeatherTPReduced = projectLoadReducingResultsGroup.Sum(x => x.WetWeatherTPReduced ?? 0),
                    WetWeatherFCReduced = projectLoadReducingResultsGroup.Sum(x => x.WetWeatherFCReduced ?? 0),
                    WetWeatherTCuReduced = projectLoadReducingResultsGroup.Sum(x => x.WetWeatherTCuReduced ?? 0),
                    WetWeatherTPbReduced = projectLoadReducingResultsGroup.Sum(x => x.WetWeatherTPbReduced ?? 0),
                    WetWeatherTZnReduced = projectLoadReducingResultsGroup.Sum(x => x.WetWeatherTZnReduced ?? 0),
                    WetWeatherTSSInflow = projectLoadReducingResultsGroup.Sum(x => x.WetWeatherTSSInflow ?? 0),
                    WetWeatherTNInflow = projectLoadReducingResultsGroup.Sum(x => x.WetWeatherTNInflow ?? 0),
                    WetWeatherTPInflow = projectLoadReducingResultsGroup.Sum(x => x.WetWeatherTPInflow ?? 0),
                    WetWeatherFCInflow = projectLoadReducingResultsGroup.Sum(x => x.WetWeatherFCInflow ?? 0),
                    WetWeatherTCuInflow = projectLoadReducingResultsGroup.Sum(x => x.WetWeatherTCuInflow ?? 0),
                    WetWeatherTPbInflow = projectLoadReducingResultsGroup.Sum(x => x.WetWeatherTPbInflow ?? 0),
                    WetWeatherTZnInflow = projectLoadReducingResultsGroup.Sum(x => x.WetWeatherTZnInflow ?? 0),

                    DryWeatherInflow = projectLoadReducingResultsGroup.Sum(x => x.DryWeatherInflow),
                    DryWeatherTreated = projectLoadReducingResultsGroup.Sum(x => x.DryWeatherTreated),
                    DryWeatherRetained = projectLoadReducingResultsGroup.Sum(x => x.DryWeatherRetained),
                    DryWeatherUntreated = projectLoadReducingResultsGroup.Sum(x => x.DryWeatherUntreated),
                    DryWeatherTSSReduced = projectLoadReducingResultsGroup.Sum(x => x.DryWeatherTSSReduced),
                    DryWeatherTNReduced = projectLoadReducingResultsGroup.Sum(x => x.DryWeatherTNReduced),
                    DryWeatherTPReduced = projectLoadReducingResultsGroup.Sum(x => x.DryWeatherTPReduced),
                    DryWeatherFCReduced = projectLoadReducingResultsGroup.Sum(x => x.DryWeatherFCReduced),
                    DryWeatherTCuReduced = projectLoadReducingResultsGroup.Sum(x => x.DryWeatherTCuReduced),
                    DryWeatherTPbReduced = projectLoadReducingResultsGroup.Sum(x => x.DryWeatherTPbReduced),
                    DryWeatherTZnReduced = projectLoadReducingResultsGroup.Sum(x => x.DryWeatherTZnReduced),
                    DryWeatherTSSInflow = projectLoadReducingResultsGroup.Sum(x => x.DryWeatherTSSInflow),
                    DryWeatherTNInflow = projectLoadReducingResultsGroup.Sum(x => x.DryWeatherTNInflow),
                    DryWeatherTPInflow = projectLoadReducingResultsGroup.Sum(x => x.DryWeatherTPInflow),
                    DryWeatherFCInflow = projectLoadReducingResultsGroup.Sum(x => x.DryWeatherFCInflow),
                    DryWeatherTCuInflow = projectLoadReducingResultsGroup.Sum(x => x.DryWeatherFCInflow),
                    DryWeatherTPbInflow = projectLoadReducingResultsGroup.Sum(x => x.DryWeatherTPbInflow),
                    DryWeatherTZnInflow = projectLoadReducingResultsGroup.Sum(x => x.DryWeatherTZnInflow),

                    TotalInflow = projectLoadReducingResultsGroup.Sum(x => x.TotalInflow),
                    TotalTreated = projectLoadReducingResultsGroup.Sum(x => x.TotalTreated),
                    TotalRetained = projectLoadReducingResultsGroup.Sum(x => x.TotalRetained),
                    TotalUntreated = projectLoadReducingResultsGroup.Sum(x => x.TotalUntreated),
                    TotalTSSReduced = projectLoadReducingResultsGroup.Sum(x => x.TotalTSSReduced),
                    TotalTNReduced = projectLoadReducingResultsGroup.Sum(x => x.TotalTNReduced),
                    TotalTPReduced = projectLoadReducingResultsGroup.Sum(x => x.TotalTPReduced),
                    TotalFCReduced = projectLoadReducingResultsGroup.Sum(x => x.TotalFCReduced),
                    TotalTCuReduced = projectLoadReducingResultsGroup.Sum(x => x.TotalTCuReduced),
                    TotalTPbReduced = projectLoadReducingResultsGroup.Sum(x => x.TotalTPbReduced),
                    TotalTZnReduced = projectLoadReducingResultsGroup.Sum(x => x.TotalTZnReduced),
                    TotalTSSInflow = projectLoadReducingResultsGroup.Sum(x => x.TotalTSSInflow),
                    TotalTNInflow = projectLoadReducingResultsGroup.Sum(x => x.TotalTNInflow),
                    TotalTPInflow = projectLoadReducingResultsGroup.Sum(x => x.TotalTPInflow),
                    TotalFCInflow = projectLoadReducingResultsGroup.Sum(x => x.TotalFCInflow),
                    TotalTCuInflow = projectLoadReducingResultsGroup.Sum(x => x.TotalFCInflow),
                    TotalTPbInflow = projectLoadReducingResultsGroup.Sum(x => x.TotalTPbInflow),
                    TotalTZnInflow = projectLoadReducingResultsGroup.Sum(x => x.TotalTZnInflow)
                };

                projectModeledResultSummaryDto.SEA = project.SEA;
                projectModeledResultSummaryDto.TPI = project.TPI;
                projectModeledResultSummaryDto.DryWeatherWQLRI = project.DryWeatherWQLRI;
                projectModeledResultSummaryDto.WetWeatherWQLRI = project.WetWeatherWQLRI;

                projectModeledResultSummaryDtos.Add(projectModeledResultSummaryDto);
            }

            return projectModeledResultSummaryDtos;
        }
        public static void SetUpdatePersonAndDate(NeptuneDbContext dbContext, int projectID, int personID)
        {
            var project = dbContext.Projects.Single(x => x.ProjectID == projectID);
            project.UpdatePersonID = personID;
            project.DateUpdated = DateTime.UtcNow;
        }
    }
}
