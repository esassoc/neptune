namespace Neptune.Models.DataTransferObjects
{
    public class WaterQualityManagementPlanDocumentExtractionResultDto
    {
        public string FinalOutput { get; set; }
        public string RawResults { get; set; }
        public DateTime ExtractedAt { get; set; }
    }
}
