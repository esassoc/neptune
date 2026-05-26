/*-----------------------------------------------------------------------
<copyright file="FieldVisit.DatabaseContextExtensions.cs" company="Tahoe Regional Planning Agency and Sitka Technology Group">
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
using Neptune.Models.DataTransferObjects;
using Neptune.Models.DataTransferObjects.ManagerDashboard;

namespace Neptune.EFModels.Entities
{
    public static class vFieldVisitDetaileds
    {
        // Manager Dashboard: provisional field visits projected straight to the grid DTO so
        // the API can return what the ag-Grid needs without materializing whole view rows.
        // Jurisdiction-scoped via ListViewableStormwaterJurisdictionIDsByPersonForBMPs.
        public static async Task<List<FieldVisitProvisionalGridDto>> GetProvisionalFieldVisitsAsGridDtoAsync(NeptuneDbContext dbContext, Person currentPerson)
        {
            var stormwaterJurisdictionIDsPersonCanView = StormwaterJurisdictionPeople.ListViewableStormwaterJurisdictionIDsByPersonForBMPs(dbContext, currentPerson);
            return await dbContext.vFieldVisitDetaileds.AsNoTracking()
                .Where(x => x.IsFieldVisitVerified == false && stormwaterJurisdictionIDsPersonCanView.Contains(x.StormwaterJurisdictionID))
                .OrderByDescending(x => x.VisitDate)
                .Select(x => new FieldVisitProvisionalGridDto
                {
                    FieldVisitID = x.FieldVisitID,
                    TreatmentBMPID = x.TreatmentBMPID,
                    TreatmentBMPName = x.TreatmentBMPName,
                    VisitDate = x.VisitDate,
                    StormwaterJurisdictionID = x.StormwaterJurisdictionID,
                    StormwaterJurisdictionName = x.OrganizationName,
                    PerformedByPersonID = x.PerformedByPersonID,
                    PerformedByPersonName = x.PerformedByPersonName,
                    FieldVisitStatusID = x.FieldVisitStatusID,
                    FieldVisitStatusDisplayName = x.FieldVisitStatusDisplayName,
                    FieldVisitTypeID = x.FieldVisitTypeID,
                    FieldVisitTypeDisplayName = x.FieldVisitTypeDisplayName,
                    IsFieldVisitVerified = x.IsFieldVisitVerified,
                    TreatmentBMPAssessmentIDInitial = x.TreatmentBMPAssessmentIDInitial,
                    IsAssessmentCompleteInitial = x.IsAssessmentCompleteInitial,
                    AssessmentScoreInitial = x.AssessmentScoreInitial,
                    MaintenanceRecordID = x.MaintenanceRecordID,
                    TreatmentBMPAssessmentIDPM = x.TreatmentBMPAssessmentIDPM,
                    IsAssessmentCompletePM = x.IsAssessmentCompletePM,
                    AssessmentScorePM = x.AssessmentScorePM,
                })
                .ToListAsync();
        }

        public static List<vFieldVisitDetailed> GetProvisionalFieldVisits(NeptuneDbContext dbContext, IEnumerable<int> stormwaterJurisdictionIDsPersonCanView)
        {
            return dbContext.vFieldVisitDetaileds.AsNoTracking()
                .Where(x => x.IsFieldVisitVerified == false && stormwaterJurisdictionIDsPersonCanView.Contains(x.StormwaterJurisdictionID)).OrderByDescending(x => x.VisitDate).ToList();
        }

        public static List<vFieldVisitDetailed> GetProvisionalFieldVisits(NeptuneDbContext dbContext, Person currentPerson)
        {
            var stormwaterJurisdictionIDsPersonCanView = StormwaterJurisdictionPeople.ListViewableStormwaterJurisdictionIDsByPersonForBMPs(dbContext, currentPerson);
            return GetProvisionalFieldVisits(dbContext, stormwaterJurisdictionIDsPersonCanView);
        }

        public static List<vFieldVisitDetailed> ListForStormwaterJurisdictionIDs(NeptuneDbContext dbContext, IEnumerable<int> stormwaterJurisdictionIDsPersonCanView)
        {
            return dbContext.vFieldVisitDetaileds.AsNoTracking()
                .Where(x => stormwaterJurisdictionIDsPersonCanView.Contains(x.StormwaterJurisdictionID)).OrderByDescending(x => x.VisitDate).ToList();
        }

        public static List<vFieldVisitDetailed> ListByTreatmentBMPID(NeptuneDbContext dbContext, int treatmentBMPID)
        {
            return dbContext.vFieldVisitDetaileds.AsNoTracking().Where(x => x.TreatmentBMPID == treatmentBMPID)
                .OrderByDescending(x => x.VisitDate)
                .ToList();
        }

        public static List<FieldVisitDto> ListAsDtoByTreatmentBMPID(NeptuneDbContext dbContext, int treatmentBMPID)
        {
            return ListByTreatmentBMPID(dbContext, treatmentBMPID)
                .Select(x => x.AsDto())
                .ToList();
        }
    }
}
