using System.Collections.Generic;

namespace Neptune.Models.DataTransferObjects
{
    public class StormwaterJurisdictionPersonBulkUpsertDto
    {
        public List<int> PersonIDs { get; set; } = new();
    }
}
