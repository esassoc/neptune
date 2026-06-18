using Neptune.EFModels.Entities;
using Neptune.Models.DataTransferObjects;

namespace Neptune.EFModels.Workflows;

public class OnlandVisualTrashAssessmentWorkflowProgress
{
    public enum OnlandVisualTrashAssessmentWorkflowStep
    {
        Instructions,
        InitiateOvta,
        RecordObservations,
        AddOrRemoveParcels,
        RefineAssessmentArea,
        ReviewAndFinalize
    }

    public static OnlandVisualTrashAssessmentWorkflowProgressDto GetProgress(OnlandVisualTrashAssessment onlandVisualTrashAssessment)
    {
        return new OnlandVisualTrashAssessmentWorkflowProgressDto
        {
            OnlandVisualTrashAssessmentID = onlandVisualTrashAssessment.OnlandVisualTrashAssessmentID,
            OnlandVisualTrashAssessmentAreaID = onlandVisualTrashAssessment.OnlandVisualTrashAssessmentAreaID,
            OnlandVisualTrashAssessmentAreaName = onlandVisualTrashAssessment.OnlandVisualTrashAssessmentArea?.OnlandVisualTrashAssessmentAreaName,
            OnlandVisualTrashAssessmentStatus = onlandVisualTrashAssessment.OnlandVisualTrashAssessmentStatus.AsSimpleDto(),
            CreatedDate = onlandVisualTrashAssessment.CreatedDate,
            Steps = Enum.GetValuesAsUnderlyingType<OnlandVisualTrashAssessmentWorkflowStep>().Cast<OnlandVisualTrashAssessmentWorkflowStep>()
                .ToDictionary(x => x, y => new ProjectWorkflowProgress.WorkflowStepStatus()
                {
                    Completed = WorkflowStepComplete(onlandVisualTrashAssessment, y),
                    Disabled = !WorkflowStepActive(onlandVisualTrashAssessment, y),
                })
        };
    }

    public static bool WorkflowStepComplete(OnlandVisualTrashAssessment onlandVisualTrashAssessment, OnlandVisualTrashAssessmentWorkflowStep wellRegistryWorkflowStep)
    {
        switch (wellRegistryWorkflowStep)
        {
            case OnlandVisualTrashAssessmentWorkflowStep.Instructions:
                return true;
            case OnlandVisualTrashAssessmentWorkflowStep.InitiateOvta:
                return onlandVisualTrashAssessment.OnlandVisualTrashAssessmentID > 0;
            case OnlandVisualTrashAssessmentWorkflowStep.RecordObservations:
                return (onlandVisualTrashAssessment.AssessingNewArea != null) && onlandVisualTrashAssessment
                    .OnlandVisualTrashAssessmentObservations.ToList().Count > 0;
            case OnlandVisualTrashAssessmentWorkflowStep.AddOrRemoveParcels:
                return onlandVisualTrashAssessment.DraftGeometry != null || (onlandVisualTrashAssessment.AssessingNewArea.HasValue && !onlandVisualTrashAssessment.AssessingNewArea.Value);
            case OnlandVisualTrashAssessmentWorkflowStep.RefineAssessmentArea:
                return onlandVisualTrashAssessment.DraftGeometry != null || (onlandVisualTrashAssessment.AssessingNewArea.HasValue && !onlandVisualTrashAssessment.AssessingNewArea.Value);
            case OnlandVisualTrashAssessmentWorkflowStep.ReviewAndFinalize:
                return onlandVisualTrashAssessment is
                {
                    OnlandVisualTrashAssessmentID: > 0, OnlandVisualTrashAssessmentObservations.Count: > 0,
                    CompletedDate: not null
                };
            default:
                throw new ArgumentOutOfRangeException(nameof(wellRegistryWorkflowStep), wellRegistryWorkflowStep, null);
        }
    }

    public static bool WorkflowStepActive(OnlandVisualTrashAssessment onlandVisualTrashAssessment, OnlandVisualTrashAssessmentWorkflowStep wellRegistryWorkflowStep)
    {
        switch (wellRegistryWorkflowStep)
        {
            case OnlandVisualTrashAssessmentWorkflowStep.Instructions:
                return true;

            case OnlandVisualTrashAssessmentWorkflowStep.ReviewAndFinalize:
                return onlandVisualTrashAssessment.OnlandVisualTrashAssessmentID > 0;

            case OnlandVisualTrashAssessmentWorkflowStep.InitiateOvta:
                return onlandVisualTrashAssessment.OnlandVisualTrashAssessmentID == 0;
            case OnlandVisualTrashAssessmentWorkflowStep.RecordObservations:
                return onlandVisualTrashAssessment.OnlandVisualTrashAssessmentID > 0;
            case OnlandVisualTrashAssessmentWorkflowStep.AddOrRemoveParcels:
                return onlandVisualTrashAssessment is { OnlandVisualTrashAssessmentID: > 0, AssessingNewArea: true };
            case OnlandVisualTrashAssessmentWorkflowStep.RefineAssessmentArea:
                return onlandVisualTrashAssessment is { OnlandVisualTrashAssessmentID: > 0, AssessingNewArea: true };
            default:
                throw new ArgumentOutOfRangeException(nameof(wellRegistryWorkflowStep), wellRegistryWorkflowStep, null);
        }

        return false;
    }

    public class OnlandVisualTrashAssessmentWorkflowProgressDto
    {
        public int OnlandVisualTrashAssessmentID { get; set; }
        public int? OnlandVisualTrashAssessmentAreaID { get; set; }
        public string? OnlandVisualTrashAssessmentAreaName { get; set; }
        public OnlandVisualTrashAssessmentStatusSimpleDto OnlandVisualTrashAssessmentStatus { get; set; }
        public Dictionary<OnlandVisualTrashAssessmentWorkflowStep, ProjectWorkflowProgress.WorkflowStepStatus> Steps { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}