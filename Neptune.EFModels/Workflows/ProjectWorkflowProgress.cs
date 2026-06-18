using Neptune.EFModels.Entities;
using Neptune.Models.DataTransferObjects;

namespace Neptune.EFModels.Workflows;

public class ProjectWorkflowProgress
{
    public enum ProjectWorkflowStep
    {
        Instructions,
        BasicInfo,
        TreatmentBMPs,
        Delineations,
        ModeledPerformanceAndGrantMetrics,
        Attachments,
        ReviewAndShare,
    }

    public static ProjectWorkflowProgressDto GetProgress(Project project,
        Dictionary<int, vTreatmentBMPModelingAttribute> vTreatmentBmpModelingAttributes)
    {
        return new ProjectWorkflowProgressDto
        {
            ProjectID = project.ProjectID,
            ProjectName = project.ProjectName,
            ProjectStatus = project.ProjectStatus.AsSimpleDto(),
            Steps = Enum.GetValuesAsUnderlyingType<ProjectWorkflowStep>().Cast<ProjectWorkflowStep>()
                .ToDictionary(x => x, y => new WorkflowStepStatus()
                {
                    Completed = WorkflowStepComplete(project, y, vTreatmentBmpModelingAttributes),
                    Disabled = !WorkflowStepActive(project, y),
                })
        };
    }

    public static bool WorkflowStepComplete(Project project, ProjectWorkflowStep wellRegistryWorkflowStep, Dictionary<int, vTreatmentBMPModelingAttribute> vTreatmentBMPModelingAttributes)
    {
        switch (wellRegistryWorkflowStep)
        {
            case ProjectWorkflowStep.Instructions:
                return true;
            case ProjectWorkflowStep.ModeledPerformanceAndGrantMetrics:
                if (project.DoesNotIncludeTreatmentBMPs)
                {
                    return true;
                }

                var treatmentBMPIDsWithNereidResults = project.ProjectNereidResults.Where(x => x.TreatmentBMPID.HasValue).Select(x => x.TreatmentBMPID.Value).Distinct().ToList();
                var treatmentBMPIDs = project.TreatmentBMPs.Select(x => x.TreatmentBMPID).ToList();
                return treatmentBMPIDs.Count > 0  && !treatmentBMPIDs.Except(treatmentBMPIDsWithNereidResults).Any();
            case ProjectWorkflowStep.TreatmentBMPs:
                if (project.DoesNotIncludeTreatmentBMPs)
                {
                    return true;
                }
                return project.TreatmentBMPs.Any() && project.TreatmentBMPs.All(x => !x.TreatmentBMPType.MissingModelingAttributes(vTreatmentBMPModelingAttributes.ContainsKey(x.TreatmentBMPID) ? vTreatmentBMPModelingAttributes[x.TreatmentBMPID] : null).Any());
            case ProjectWorkflowStep.Delineations:
                if (project.DoesNotIncludeTreatmentBMPs)
                {
                    return true;
                }
                return project.TreatmentBMPs.Any() && project.TreatmentBMPs.All(x => x.Delineation != null);
            case ProjectWorkflowStep.BasicInfo:
                return !string.IsNullOrWhiteSpace(project.ProjectName)
                       && project is { OrganizationID: > 0, PrimaryContactPersonID: > 0, StormwaterJurisdictionID: > 0 };
            case ProjectWorkflowStep.ReviewAndShare:
                return true;
            case ProjectWorkflowStep.Attachments:
                return true;
            default:
                throw new ArgumentOutOfRangeException(nameof(wellRegistryWorkflowStep), wellRegistryWorkflowStep, null);
        }
    }

    public static bool WorkflowStepActive(Project project, ProjectWorkflowStep wellRegistryWorkflowStep)
    {
        switch (wellRegistryWorkflowStep)
        {
            case ProjectWorkflowStep.Instructions:
                return true;

            case ProjectWorkflowStep.Attachments:
            case ProjectWorkflowStep.TreatmentBMPs:
                if (project.ProjectID > 0)
                {
                    return true;
                }
                return !project.ShareOCTAM2Tier2Scores;

            case ProjectWorkflowStep.ModeledPerformanceAndGrantMetrics:
            case ProjectWorkflowStep.Delineations:
                if (project.ProjectID > 0)
                {
                    return true;
                }
                if (project.ShareOCTAM2Tier2Scores)
                {
                    return false;
                }
                return !project.DoesNotIncludeTreatmentBMPs && project.TreatmentBMPs.Any();

            case ProjectWorkflowStep.BasicInfo:
                return !project.ShareOCTAM2Tier2Scores;

            case ProjectWorkflowStep.ReviewAndShare:
                return project.ProjectID > 0;

            default:
                throw new ArgumentOutOfRangeException(nameof(wellRegistryWorkflowStep), wellRegistryWorkflowStep, null);
        }
    }

    public class ProjectWorkflowProgressDto
    {   
        public int ProjectID { get; set; }
        public string ProjectName { get; set; }
        public ProjectStatusSimpleDto ProjectStatus { get; set; }
        public Dictionary<ProjectWorkflowStep, WorkflowStepStatus> Steps { get; set; }
    }

    public class WorkflowStepStatus
    {
        public bool Completed { get; set; }
        public bool Disabled { get; set; }
    }
}