using System.Collections.Generic;

namespace Neptune.Models.DataTransferObjects
{
    /// <summary>
    /// Pre-commit report for a Land Use Block GDB staging batch. NPT-1077: the prior flow enqueued
    /// a background job that performed PriorityLandUseType + PermitType validation row-by-row and
    /// emailed the user the results. This DTO surfaces the same validation synchronously so the
    /// SPA approve page can show the user errors before they commit. Modeled on
    /// <c>DelineationGdbUploadValidationDto</c>.
    /// </summary>
    public class LandUseBlockGdbUploadValidationDto
    {
        public int TotalStagedRowCount { get; set; }

        /// <summary>
        /// Wholesale-replace semantics: the existing background job deletes all
        /// <c>LandUseBlock</c> rows in the staging-affected jurisdictions before re-inserting from
        /// staging. This count tells the user how many existing rows will be replaced.
        /// </summary>
        public int ExistingRowsToReplace { get; set; }

        public List<string> Errors { get; set; } = new();
    }
}
