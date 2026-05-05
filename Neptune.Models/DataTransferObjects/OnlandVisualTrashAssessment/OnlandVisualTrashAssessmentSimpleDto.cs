//  IMPORTANT:
//  This file is generated. Your changes will be lost.
//  Use the corresponding partial class for customizations.
//  Source Table: [dbo].[OnlandVisualTrashAssessment]

namespace Neptune.Models.DataTransferObjects
{
    public partial class OnlandVisualTrashAssessmentSimpleDto
    {
        public int OnlandVisualTrashAssessmentID { get; set; }
        public int CreatedByPersonID { get; set; }
        public DateTime CreatedDate { get; set; }
        public int? OnlandVisualTrashAssessmentAreaID { get; set; }
        public string Notes { get; set; }
        public int StormwaterJurisdictionID { get; set; }
        public bool? AssessingNewArea { get; set; }
        public int OnlandVisualTrashAssessmentStatusID { get; set; }
        public bool? IsDraftGeometryManuallyRefined { get; set; }
        public int? OnlandVisualTrashAssessmentScoreID { get; set; }
        public DateOnly? CompletedDate { get; set; }
        public string DraftAreaName { get; set; }
        public string DraftAreaDescription { get; set; }
        public bool IsTransectBackingAssessment { get; set; }
        public bool IsProgressAssessment { get; set; }
        public string SecondAssessorName { get; set; }
        public int? OvtaAreaSourceTypeID { get; set; }
    }
}