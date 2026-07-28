using System.Collections.Generic;

namespace Neptune.Models.DataTransferObjects;

public class WaterQualityManagementPlanGdbDownloadRequestDto
{
    // The WQMP IDs to export (the Index page sends its post-filter rows). Empty/null ⇒ all viewable.
    public List<int> WaterQualityManagementPlanIDs { get; set; }
}
