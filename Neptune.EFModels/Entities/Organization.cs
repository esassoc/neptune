/*-----------------------------------------------------------------------
<copyright file="Organization.cs" company="Tahoe Regional Planning Agency and Sitka Technology Group">
Copyright (c) Tahoe Regional Planning Agency and Sitka Technology Group. All rights reserved.
<author>Sitka Technology Group</author>
</copyright>

<license>
This program is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Affero General Public License <http://www.gnu.org/licenses/> for more details.

Source code is available upon request via <support@sitkatech.com>.
</license>
-----------------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;

namespace Neptune.EFModels.Entities
{
    public partial class Organization
    {
        public const string OrganizationUnknown = "Unknown or Unspecified Organization";

        public string GetDisplayName() => IsUnknown() ? "Unknown or unspecified" : $"{OrganizationName}{(!IsActive ? " (Inactive)" : string.Empty)}";

        public string GetOrganizationShortNameIfAvailable() => IsUnknown() ? "Unknown or unspecified" : $"{OrganizationShortName ?? OrganizationName}{(!IsActive ? " (Inactive)" : string.Empty)}";

        public bool IsUnknown()
        {
            return !String.IsNullOrWhiteSpace(OrganizationName) &&
                   OrganizationName.Equals(OrganizationUnknown, StringComparison.InvariantCultureIgnoreCase);
        }

        public async Task DeleteFull(NeptuneDbContext dbContext)
        {
            var unknownOrganizationID = Organizations.GetUnknownOrganizationID(dbContext);
            // we don't want to delete any information related to an organization. So we're going to set the org id to unknown
            foreach (var fundingSource in dbContext.FundingSources.Where(x => x.OrganizationID == OrganizationID).ToList())
            {
                fundingSource.OrganizationID = unknownOrganizationID;
            }
            foreach (var person in dbContext.People.Where(x => x.OrganizationID == OrganizationID).ToList())
            {
                person.OrganizationID = unknownOrganizationID;
            }
            foreach (var project in dbContext.Projects.Where(x => x.OrganizationID == OrganizationID).ToList())
            {
                project.OrganizationID = unknownOrganizationID;
            }
            foreach (var treatmentBMP in dbContext.TreatmentBMPs.Where(x => x.OwnerOrganizationID == OrganizationID).ToList())
            {
                treatmentBMP.OwnerOrganizationID = unknownOrganizationID;
            }
            await dbContext.StormwaterJurisdictions.Where(x => x.OrganizationID == OrganizationID).ExecuteDeleteAsync();
            await dbContext.SaveChangesAsync();
            await dbContext.Organizations.Where(x => x.OrganizationID == OrganizationID).ExecuteDeleteAsync();
        }
    }
}
