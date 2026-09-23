namespace Neptune.Models.DataTransferObjects
{
    public class OVTAResultsDto
    {
        public double PLUSumAcresWhereOVTAIsA { get; set; }
        public double PLUSumAcresWhereOVTAIsB { get; set; }
        public double PLUSumAcresWhereOVTAIsC { get; set; }
        public double PLUSumAcresWhereOVTAIsD { get; set; }
        public double ALUSumAcresWhereOVTAIsA { get; set; }
        public double ALUSumAcresWhereOVTAIsB { get; set; }
        public double ALUSumAcresWhereOVTAIsC { get; set; }
        public double ALUSumAcresWhereOVTAIsD { get; set; }

        // NPT-1128 rework: empty-state signals so the UI can explain an all-zero table instead of rendering it.
        public bool HasLandUseBlocks { get; set; }
        // True when the jurisdiction has at least one OVTA Area with a baseline score
        // (i.e. >= 2 completed baseline assessments), regardless of land use / permit type.
        public bool HasOVTAResults { get; set; }
    }
}