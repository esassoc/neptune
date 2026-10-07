namespace Neptune.Models.DataTransferObjects
{
    public class WaterQualityManagementPlanDocumentExtractionResultDto
    {
        public string FinalOutput { get; set; }
        public string RawResults { get; set; }
        public DateTime ExtractedAt { get; set; }

        // NPT-1132: per-call usage and the failures the service otherwise papers over
        // (empty fallbacks, retries). Not persisted; the extraction eval reads them.
        public List<WqmpExtractionCallUsageDto> CallUsage { get; set; } = new();
        public List<string> Hiccups { get; set; } = new();
    }

    public class WqmpExtractionCallUsageDto
    {
        public string Category { get; set; }
        public long InputTokens { get; set; }
        public long CacheCreationInputTokens { get; set; }
        public long CacheReadInputTokens { get; set; }
        public long OutputTokens { get; set; }
        public string StopReason { get; set; }
        public long ElapsedMs { get; set; }
    }
}
