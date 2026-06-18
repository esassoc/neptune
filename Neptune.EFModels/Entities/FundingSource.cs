using Microsoft.EntityFrameworkCore;

namespace Neptune.EFModels.Entities
{
    public partial class FundingSource
    {
        public string GetDisplayName()
        {
            return
                $"{FundingSourceName} ({Organization.GetOrganizationShortNameIfAvailable()}){(!IsActive ? " (Inactive)" : string.Empty)}";
        }
    }
}