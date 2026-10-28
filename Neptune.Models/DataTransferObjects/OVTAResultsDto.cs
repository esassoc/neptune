using System.Collections.Generic;

namespace Neptune.Models.DataTransferObjects
{
    public class OVTAResultsDto
    {
        // NPT-1126: one row per land use type (PLU by Priority Land Use Type, ALU by Land Use for TGR).
        // Category subtotals are computed client-side.
        public List<OVTAResultsLandUseRowDto> Rows { get; set; } = new();

        // NPT-1126: OVTA Areas with a Progress Score but no Baseline Score. Shown to users so the jurisdiction
        // can find and fix the data, since a Progress Score should only exist for re-assessed baseline area.
        public List<OVTAResultsAreaDataIssueDto> AreasWithProgressButNoBaseline { get; set; } = new();

        // NPT-1128 rework: empty-state signals so the UI can explain an all-zero table instead of rendering it.
        public bool HasLandUseBlocks { get; set; }
        // True when the jurisdiction has at least one OVTA Area with a baseline score
        // (i.e. >= 2 completed baseline assessments), regardless of land use / permit type.
        public bool HasOVTAResults { get; set; }
    }

    public class OVTAResultsLandUseRowDto
    {
        public bool IsPriorityLandUse { get; set; }
        public string LandUseTypeName { get; set; }
        public int SortOrder { get; set; }

        // Phase I MS4 Land Use Block area, the same denominator the Area-Based Results view uses.
        public double TotalMappedAcres { get; set; }

        public double BaselineAcresA { get; set; }
        public double BaselineAcresB { get; set; }
        public double BaselineAcresC { get; set; }
        public double BaselineAcresD { get; set; }

        public double ProgressAcresA { get; set; }
        public double ProgressAcresB { get; set; }
        public double ProgressAcresC { get; set; }
        public double ProgressAcresD { get; set; }

        // Area by number of completed OVTAs on record (baseline and progress). The four columns sum to TotalMappedAcres.
        public double NotYetAssessedAcres { get; set; }
        public double AssessedOnceAcres { get; set; }
        public double AssessedTwiceAcres { get; set; }
        public double AssessedThreeOrMoreAcres { get; set; }
    }

    public class OVTAResultsAreaDataIssueDto
    {
        public int OnlandVisualTrashAssessmentAreaID { get; set; }
        public string OnlandVisualTrashAssessmentAreaName { get; set; }
    }
}
