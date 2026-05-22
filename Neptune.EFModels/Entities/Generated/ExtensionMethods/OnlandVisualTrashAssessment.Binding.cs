//  IMPORTANT:
//  This file is generated. Your changes will be lost.
//  Use the corresponding partial class for customizations.
//  Source Table: [dbo].[OnlandVisualTrashAssessment]
namespace Neptune.EFModels.Entities
{
    public partial class OnlandVisualTrashAssessment : IHavePrimaryKey
    {
        public int PrimaryKey => OnlandVisualTrashAssessmentID;
        public OnlandVisualTrashAssessmentStatus OnlandVisualTrashAssessmentStatus => OnlandVisualTrashAssessmentStatus.AllLookupDictionary[OnlandVisualTrashAssessmentStatusID];
        public OnlandVisualTrashAssessmentScore? OnlandVisualTrashAssessmentScore => OnlandVisualTrashAssessmentScoreID.HasValue ? OnlandVisualTrashAssessmentScore.AllLookupDictionary[OnlandVisualTrashAssessmentScoreID.Value] : null;
        public OvtaAreaSourceType? OvtaAreaSourceType => OvtaAreaSourceTypeID.HasValue ? OvtaAreaSourceType.AllLookupDictionary[OvtaAreaSourceTypeID.Value] : null;

        public static class FieldLengths
        {
            public const int Notes = 500;
            public const int DraftAreaName = 100;
            public const int DraftAreaDescription = 500;
            public const int SecondAssessorName = 200;
        }
    }
}