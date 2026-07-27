namespace Neptune.Models.DataTransferObjects;

public class OnlandVisualTrashAssessmentAreaMoveAssessmentsDto
{
    public int TargetOnlandVisualTrashAssessmentAreaID { get; set; }
    public IEnumerable<int> OnlandVisualTrashAssessmentIDs { get; set; }
}
