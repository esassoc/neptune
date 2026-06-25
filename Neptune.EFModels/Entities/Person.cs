/*-----------------------------------------------------------------------
<copyright file="Person.cs" company="Tahoe Regional Planning Agency and Sitka Technology Group">
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
    public partial class Person
    {
        public const int AnonymousPersonID = -999;
        public const int SystemPersonID = 1122;

        public bool IsAnonymousUser()
        {
            return PersonID == AnonymousPersonID;
        }

        public string GetFullNameFirstLast()
        {
            return $"{FirstName} {LastName}";
        }

        public bool IsAdministrator()
        {
            return RoleID == Role.SitkaAdmin.RoleID || RoleID == Role.Admin.RoleID;
        }

        public bool IsAssignedToStormwaterJurisdiction(int stormwaterJurisdictionID)
        {
            return IsAdministrator() ||
                   StormwaterJurisdictionPeople.Any(x => x.StormwaterJurisdictionID == stormwaterJurisdictionID);
        }

        public bool IsManagerOrAdmin()
        {
            return RoleID == Role.Admin.RoleID || RoleID == Role.JurisdictionManager.RoleID ||
                   RoleID == Role.SitkaAdmin.RoleID;
        }

        public bool IsAnonymousOrUnassigned()
        {
            return IsAnonymousUser() || Role == Role.Unassigned;
        }


        public async Task DeleteFull(NeptuneDbContext dbContext)
        {
            foreach (var delineation in dbContext.Delineations.Where(x => x.VerifiedByPersonID == PersonID).ToList())
            {
                delineation.VerifiedByPersonID = SystemPersonID;
            }

            foreach (var fieldVisit in dbContext.FieldVisits.Where(x => x.PerformedByPersonID == PersonID).ToList())
            {
                fieldVisit.PerformedByPersonID = SystemPersonID;
            }

            foreach (var fileResource in dbContext.FileResources.Where(x => x.CreatePersonID == PersonID).ToList())
            {
                fileResource.CreatePersonID = SystemPersonID;
            }

            await dbContext.Notifications.Where(x => x.PersonID == PersonID).ExecuteDeleteAsync();

            foreach (var onlandVisualTrashAssessment in dbContext.OnlandVisualTrashAssessments.Where(x => x.CreatedByPersonID == PersonID).ToList())
            {
                onlandVisualTrashAssessment.CreatedByPersonID = SystemPersonID;
            }
            
            foreach (var project in dbContext.Projects.Where(x => x.CreatePersonID == PersonID 
                                                                  || x.PrimaryContactPersonID == PersonID 
                                                                  || x.UpdatePersonID == PersonID).ToList())
            {
                if (project.CreatePersonID == PersonID)
                {
                    project.CreatePersonID = SystemPersonID;
                }

                if (project.PrimaryContactPersonID == PersonID)
                {
                    project.PrimaryContactPersonID = SystemPersonID;
                }

                if (project.UpdatePersonID == PersonID)
                {
                    project.UpdatePersonID = SystemPersonID;
                }
            }


            foreach (var projectNetworkSolveHistory in dbContext.ProjectNetworkSolveHistories.Where(x => x.RequestedByPersonID == PersonID).ToList())
            {
                projectNetworkSolveHistory.RequestedByPersonID = SystemPersonID;
            }

            foreach (var regionalSubbasinRevisionRequest in dbContext.RegionalSubbasinRevisionRequests
                         .Where(x => x.ClosedByPersonID == PersonID || x.RequestPersonID == PersonID).ToList())
            {
                if (regionalSubbasinRevisionRequest.ClosedByPersonID == PersonID)
                {
                    regionalSubbasinRevisionRequest.ClosedByPersonID = SystemPersonID;
                }

                if (regionalSubbasinRevisionRequest.RequestPersonID == PersonID)
                {
                    regionalSubbasinRevisionRequest.RequestPersonID = SystemPersonID;
                }
            }

            await dbContext.StormwaterJurisdictionPeople.Where(x => x.PersonID == PersonID).ExecuteDeleteAsync();

            foreach (var supportRequestLog in dbContext.SupportRequestLogs.Where(x => x.RequestPersonID == PersonID).ToList())
            {
                supportRequestLog.RequestPersonID = SystemPersonID;
            }

            foreach (var trashGeneratingUnitAdjustment in dbContext.TrashGeneratingUnitAdjustments.Where(x => x.AdjustedByPersonID == PersonID).ToList())
            {
                trashGeneratingUnitAdjustment.AdjustedByPersonID = SystemPersonID;
            }
            foreach (var treatmentBMP in dbContext.TreatmentBMPs.Where(x => x.InventoryVerifiedByPersonID == PersonID).ToList())
            {
                treatmentBMP.InventoryVerifiedByPersonID = SystemPersonID;
            }

            foreach (var waterQualityManagementPlanVerify in dbContext.WaterQualityManagementPlanVerifies.Where(x => x.LastEditedByPersonID == PersonID).ToList())
            {
                waterQualityManagementPlanVerify.LastEditedByPersonID = SystemPersonID;
            }

            await dbContext.SaveChangesAsync();

            await dbContext.People.Where(x => x.PersonID == PersonID).ExecuteDeleteAsync();
        }
    }
}
