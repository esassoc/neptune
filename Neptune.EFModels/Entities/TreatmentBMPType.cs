/*-----------------------------------------------------------------------
<copyright file="TreatmentBMPType.cs" company="Tahoe Regional Planning Agency">
Copyright (c) Tahoe Regional Planning Agency. All rights reserved.
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
    public partial class TreatmentBMPType
    {

        public List<TreatmentBMPAssessmentObservationType> GetObservationTypes()
        {
            return TreatmentBMPTypeAssessmentObservationTypes.OrderBy(x => x.SortOrder).ThenBy(x => x.TreatmentBMPAssessmentObservationType.TreatmentBMPAssessmentObservationTypeName).Select(x => x.TreatmentBMPAssessmentObservationType).ToList();
        }

        public List<TreatmentBMPTypeAssessmentObservationType> GetObservationTypesForAssessment()
        {
            return TreatmentBMPTypeAssessmentObservationTypes.OrderBy(x => x.SortOrder).ThenBy(x =>
                x.TreatmentBMPAssessmentObservationType.TreatmentBMPAssessmentObservationTypeName).ToList();
        }

        public TreatmentBMPTypeAssessmentObservationType? GetTreatmentBMPTypeObservationTypeOrDefault(TreatmentBMPAssessmentObservationType observationType)
        {
            return TreatmentBMPTypeAssessmentObservationTypes.SingleOrDefault(x =>
                x.TreatmentBMPTypeID == TreatmentBMPTypeID &&
                x.TreatmentBMPAssessmentObservationTypeID == observationType.TreatmentBMPAssessmentObservationTypeID);
        }

        public TreatmentBMPTypeAssessmentObservationType GetTreatmentBMPTypeObservationType(TreatmentBMPAssessmentObservationType observationType)
        {
            return GetTreatmentBMPTypeObservationTypeOrDefault(observationType)
                ?? throw new InvalidOperationException(
                    $"The Observation Type '{observationType.TreatmentBMPAssessmentObservationTypeName}' is not applicable to the Treatment BMP Type '{TreatmentBMPTypeName}'.");
        }

        public async Task DeleteFull(NeptuneDbContext dbContext)
        {
            await dbContext.CustomAttributeValues
                .Include(x => x.CustomAttribute)
                .Where(x => x.CustomAttribute.TreatmentBMPTypeID == TreatmentBMPTypeID).ExecuteDeleteAsync();
            await dbContext.CustomAttributes.Where(x => x.TreatmentBMPTypeID == TreatmentBMPTypeID).ExecuteDeleteAsync();
            await dbContext.MaintenanceRecordObservationValues
                .Include(x => x.MaintenanceRecordObservation)
                .Where(x => x.MaintenanceRecordObservation.TreatmentBMPTypeID == TreatmentBMPTypeID)
                .ExecuteDeleteAsync();
            await dbContext.MaintenanceRecordObservations.Where(x => x.TreatmentBMPTypeID == TreatmentBMPTypeID)
                .ExecuteDeleteAsync();
            await dbContext.MaintenanceRecords.Where(x => x.TreatmentBMPTypeID == TreatmentBMPTypeID)
                .ExecuteDeleteAsync();
            await dbContext.WaterQualityManagementPlanVerifyQuickBMPs
                .Include(x => x.QuickBMP)
                .Where(x => x.QuickBMP.TreatmentBMPTypeID == TreatmentBMPTypeID).ExecuteDeleteAsync();
            await dbContext.QuickBMPs.Where(x => x.TreatmentBMPTypeID == TreatmentBMPTypeID).ExecuteDeleteAsync();
            foreach (var treatmentBMP in dbContext.TreatmentBMPs.Where(x => x.TreatmentBMPTypeID == TreatmentBMPTypeID).ToList())
            {
                await treatmentBMP.DeleteFull(dbContext);
            }
            await dbContext.TreatmentBMPTypeAssessmentObservationTypes.Where(x => x.TreatmentBMPTypeID == TreatmentBMPTypeID)
                .ExecuteDeleteAsync();
            await dbContext.TreatmentBMPTypeCustomAttributeTypes.Where(x => x.TreatmentBMPTypeID == TreatmentBMPTypeID). ExecuteDeleteAsync();
            await dbContext.TreatmentBMPTypes.Where(x => x.TreatmentBMPTypeID == TreatmentBMPTypeID).ExecuteDeleteAsync();
        }
    }
}
