/*-----------------------------------------------------------------------
<copyright file="TreatmentBMP.cs" company="Tahoe Regional Planning Agency">
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
using NetTopologySuite.Geometries;
using System.Globalization;

namespace Neptune.EFModels.Entities
{
    public partial class TreatmentBMP
    {
        public bool CanView(Person person)
        {
            return ProjectID == null && person.IsAssignedToStormwaterJurisdiction(StormwaterJurisdictionID);
        }

        public bool CanEdit(Person person)
        {
            return ProjectID == null && person.IsAssignedToStormwaterJurisdiction(StormwaterJurisdictionID);
        }

        public bool CanDelete(Person person)
        {
            return ProjectID == null && person.IsManagerOrAdmin() && person.IsAssignedToStormwaterJurisdiction(StormwaterJurisdictionID);
        }

        public bool IsBenchmarkAndThresholdsComplete(TreatmentBMPType treatmentBMPType)
        {
            var observationTypesIDs = treatmentBMPType.TreatmentBMPTypeAssessmentObservationTypes
                .Where(x => x.TreatmentBMPAssessmentObservationType.GetHasBenchmarkAndThreshold())
                .Select(x => x.TreatmentBMPAssessmentObservationTypeID).ToList();
            var benchmarkAndThresholdObservationTypeIDs = TreatmentBMPBenchmarkAndThresholds.Select(x => x.TreatmentBMPAssessmentObservationTypeID).ToList();

            return !observationTypesIDs.Except(benchmarkAndThresholdObservationTypeIDs).Any();
        }

        public static string GetCustomAttributeValueWithUnits(TreatmentBMPTypeCustomAttributeType treatmentBMPTypeCustomAttributeType, ICollection<CustomAttribute> customAttributes)
        {
            if (customAttributes.Any())
            {
                var customAttribute = customAttributes.SingleOrDefault(x =>
                    x.CustomAttributeTypeID == treatmentBMPTypeCustomAttributeType.CustomAttributeTypeID);
                if (customAttribute != null)
                {
                    var measurementUnit = "";
                    var customAttributeType = treatmentBMPTypeCustomAttributeType.CustomAttributeType;
                    if (customAttributeType.MeasurementUnitTypeID.HasValue)
                    {
                        measurementUnit = $" {customAttributeType.MeasurementUnitType.LegendDisplayName}";
                    }

                    // Format numbers with thousands separators while preserving original decimal precision
                    var formattedValues = customAttribute.CustomAttributeValues
                        .OrderBy(x => x.AttributeValue)
                        .Select(x =>
                        {
                            var raw = x.AttributeValue;
                            if (string.IsNullOrWhiteSpace(raw))
                            {
                                return raw ?? string.Empty;
                            }
                            if (decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var num))
                            {
                                var decimalPlaces = raw.Contains('.') ? raw.Split('.')[1].Length : 0;
                                var formatSpecifier = "N" + decimalPlaces; // e.g. N0, N1, N2
                                return num.ToString(formatSpecifier, CultureInfo.InvariantCulture);
                            }
                            return raw; // non-numeric, return as-is
                        });

                    var value = string.Join(", ", formattedValues);

                    return $"{value}{measurementUnit}";
                }
            }
            return string.Empty;
        }

        public static string GetCustomAttributeStatus(TreatmentBMPType treatmentBMPType,
            List<CustomAttribute> customAttributes)
        {
            var nonMaintenanceTreatmentBMPTypeCustomAttributeTypes =
                treatmentBMPType.TreatmentBMPTypeCustomAttributeTypes.Where(x =>
                    x.CustomAttributeType.CustomAttributeTypePurpose != CustomAttributeTypePurpose.Maintenance).ToList();

            var completedObservationCount = nonMaintenanceTreatmentBMPTypeCustomAttributeTypes.Count(x =>
                x.CustomAttributeType.IsRequired && x.CustomAttributeType.IsCompleteForTreatmentBMP(customAttributes));

            var totalObservationCount = nonMaintenanceTreatmentBMPTypeCustomAttributeTypes.Count(x =>
                x.CustomAttributeType.IsRequired);

            return completedObservationCount == totalObservationCount
                ? "All Required Data Provided"
                : $"In Progress ({completedObservationCount} of {totalObservationCount} required attributes entered)";
        }

        public void MarkAsVerified(Person currentPerson)
        {
            InventoryIsVerified = true;
            DateOfLastInventoryVerification = DateTime.UtcNow;
            InventoryVerifiedByPersonID = currentPerson.PersonID;
        }

        public async Task DeleteFull(NeptuneDbContext dbContext)
        {
            await dbContext.CustomAttributeValues.Include(x => x.CustomAttribute)
                .Where(x => x.CustomAttribute.TreatmentBMPID == TreatmentBMPID).ExecuteDeleteAsync();
            await dbContext.CustomAttributes.Where(x => x.TreatmentBMPID == TreatmentBMPID).ExecuteDeleteAsync();
            await dbContext.DelineationOverlaps
                .Include(x => x.Delineation)
                .Include(x => x.OverlappingDelineation)
                .Where(x => x.Delineation.TreatmentBMPID == TreatmentBMPID || x.OverlappingDelineation.TreatmentBMPID == TreatmentBMPID).ExecuteDeleteAsync();
            await dbContext.HRUCharacteristics
                .Include(x => x.LoadGeneratingUnit)
                .ThenInclude(x => x.Delineation)
                .Where(x => x.LoadGeneratingUnit.Delineation != null && x.LoadGeneratingUnit.Delineation.TreatmentBMPID == TreatmentBMPID)
                .ExecuteDeleteAsync();
            await dbContext.LoadGeneratingUnits
                .Include(x => x.Delineation)
                .Where(x => x.Delineation != null && x.Delineation.TreatmentBMPID == TreatmentBMPID).ExecuteDeleteAsync();
            await dbContext.NereidResults.Include(x => x.Delineation)
                .Where(x => x.Delineation != null && x.Delineation.TreatmentBMPID == TreatmentBMPID).ExecuteDeleteAsync();
            await dbContext.ProjectHRUCharacteristics
                .Include(x => x.ProjectLoadGeneratingUnit)
                .ThenInclude(x => x.Delineation)
                .Where(x => x.ProjectLoadGeneratingUnit.Delineation != null && x.ProjectLoadGeneratingUnit.Delineation.TreatmentBMPID == TreatmentBMPID).ExecuteDeleteAsync();
            await dbContext.ProjectLoadGeneratingUnits
                .Include(x => x.Delineation)
                .Where(x => x.Delineation != null && x.Delineation.TreatmentBMPID == TreatmentBMPID)
                .ExecuteDeleteAsync();
            await dbContext.ProjectNereidResults
                .Include(x => x.Delineation)
                .Where(x => x.Delineation != null && x.Delineation.TreatmentBMPID == TreatmentBMPID)
                .ExecuteDeleteAsync();
            await dbContext.TrashGeneratingUnit4326s.Include(x => x.TrashGeneratingUnit).ThenInclude(x => x.Delineation).Where(x => x.TrashGeneratingUnit != null && x.TrashGeneratingUnit.Delineation != null && x.TrashGeneratingUnit.Delineation.TreatmentBMPID == TreatmentBMPID).ExecuteDeleteAsync();
            await dbContext.TrashGeneratingUnits.Include(x => x.Delineation).Where(x => x.Delineation != null && x.Delineation.TreatmentBMPID == TreatmentBMPID).ExecuteDeleteAsync();
            await dbContext.Delineations.Where(x => x.TreatmentBMPID == TreatmentBMPID).ExecuteDeleteAsync();
            await dbContext.DirtyModelNodes.Where(x => x.TreatmentBMPID == TreatmentBMPID).ExecuteDeleteAsync();
            await dbContext.MaintenanceRecordObservationValues
                .Include(x => x.MaintenanceRecordObservation)
                .ThenInclude(x => x.MaintenanceRecord)
                .Where(x => x.MaintenanceRecordObservation.MaintenanceRecord.TreatmentBMPID == TreatmentBMPID)
                .ExecuteDeleteAsync();
            await dbContext.MaintenanceRecordObservations
                .Include(x => x.MaintenanceRecord)
                .Where(x => x.MaintenanceRecord.TreatmentBMPID == TreatmentBMPID).ExecuteDeleteAsync();
            await dbContext.MaintenanceRecords.Where(x => x.TreatmentBMPID == TreatmentBMPID).ExecuteDeleteAsync();
            await dbContext.TreatmentBMPAssessmentPhotos
                .Include(x => x.TreatmentBMPAssessment)
                .Where(x => x.TreatmentBMPAssessment.TreatmentBMPID == TreatmentBMPID)
                .ExecuteDeleteAsync();
            await dbContext.TreatmentBMPObservations
                .Include(x => x.TreatmentBMPAssessment)
                .Where(x => x.TreatmentBMPAssessment.TreatmentBMPID == TreatmentBMPID)
                .ExecuteDeleteAsync();
            await dbContext.TreatmentBMPAssessments
                .Where(x => x.TreatmentBMPID == TreatmentBMPID)
                .ExecuteDeleteAsync();
            await dbContext.FieldVisits.Where(x => x.TreatmentBMPID == TreatmentBMPID).ExecuteDeleteAsync();
            await dbContext.FundingEventFundingSources
                .Include(x => x.FundingEvent)
                .Where(x => x.FundingEvent.TreatmentBMPID == TreatmentBMPID).ExecuteDeleteAsync();
            await dbContext.FundingEvents.Where(x => x.TreatmentBMPID == TreatmentBMPID).ExecuteDeleteAsync();
            await dbContext.NereidResults.Where(x => x.TreatmentBMPID == TreatmentBMPID).ExecuteDeleteAsync();
            await dbContext.ProjectNereidResults.Where(x => x.TreatmentBMPID == TreatmentBMPID). ExecuteDeleteAsync();
            await dbContext.RegionalSubbasinRevisionRequests.Where(x => x.TreatmentBMPID == TreatmentBMPID).ExecuteDeleteAsync();
            await dbContext.TreatmentBMPAssessmentPhotos
                .Include(x => x.TreatmentBMPAssessment)
                .Where(x => x.TreatmentBMPAssessment.TreatmentBMPID == TreatmentBMPID)
                .ExecuteDeleteAsync();
            await dbContext.TreatmentBMPObservations
                .Include(x => x.TreatmentBMPAssessment)
                .Where(x => x.TreatmentBMPAssessment.TreatmentBMPID == TreatmentBMPID)
                .ExecuteDeleteAsync();
            await dbContext.TreatmentBMPAssessments.Where(x => x.TreatmentBMPID == TreatmentBMPID)
                .ExecuteDeleteAsync();

            await dbContext.TreatmentBMPBenchmarkAndThresholds
                .Where(x => x.TreatmentBMPID == TreatmentBMPID).ExecuteDeleteAsync();
            await dbContext.TreatmentBMPDocuments.Where(x => x.TreatmentBMPID == TreatmentBMPID).ExecuteDeleteAsync();
            await dbContext.TreatmentBMPImages.Where(x => x.TreatmentBMPID == TreatmentBMPID).ExecuteDeleteAsync();
            await dbContext.TreatmentBMPModelingAttributes.Where(x => x.TreatmentBMPID == TreatmentBMPID).ExecuteDeleteAsync();
            await dbContext.WaterQualityManagementPlanVerifyTreatmentBMPs.Where(x => x.TreatmentBMPID == TreatmentBMPID).ExecuteDeleteAsync();
            await dbContext.TreatmentBMPs.Where(x => x.UpstreamBMPID == TreatmentBMPID)
                    .ExecuteUpdateAsync(x => x.SetProperty(y => y.UpstreamBMPID, (int?) null));

            await dbContext.SaveChangesAsync();
            await dbContext.TreatmentBMPs.Where(x => x.TreatmentBMPID == TreatmentBMPID).ExecuteDeleteAsync();
        }

        public IEnumerable<ProjectHRUCharacteristic> GetHRUCharacteristics(NeptuneDbContext dbContext)
        {
            if (Delineation == null)
            {
                return new List<ProjectHRUCharacteristic>();
            }

            if (Delineation.DelineationTypeID == (int)DelineationTypeEnum.Centralized && TreatmentBMPType.TreatmentBMPModelingTypeID != null)
            {
                var regionalSubbasin = GetRegionalSubbasin(dbContext);
                if (regionalSubbasin == null)
                {
                    return Enumerable.Empty<ProjectHRUCharacteristic>();
                }
                var catchmentRegionalSubbasins = vRegionalSubbasinUpstreams.ListUpstreamRegionalBasinIDs(dbContext, regionalSubbasin);

                return dbContext.ProjectHRUCharacteristics
                    .Include(x => x.ProjectLoadGeneratingUnit).AsNoTracking()
                    .Where(x =>
                        x.ProjectID == ProjectID &&
                        x.ProjectLoadGeneratingUnit.RegionalSubbasinID != null &&
                        catchmentRegionalSubbasins.Contains(x.ProjectLoadGeneratingUnit.RegionalSubbasinID.Value));
            }

            return dbContext.ProjectHRUCharacteristics
                .Include(x => x.ProjectLoadGeneratingUnit)
                .ThenInclude(x => x.Delineation).AsNoTracking()
                .Where(x =>
                    x.ProjectID == ProjectID &&
                    x.ProjectLoadGeneratingUnit.Delineation != null &&
                    x.ProjectLoadGeneratingUnit.Delineation.TreatmentBMPID == TreatmentBMPID);
        }

        public RegionalSubbasin? GetRegionalSubbasin(NeptuneDbContext dbContext)
        {
            return dbContext.RegionalSubbasins.AsNoTracking().SingleOrDefault(x =>
                x.CatchmentGeometry.Contains(LocationPoint));
        }

        /// <summary>
        /// Performs the RSB trace for a given Treatment BMP using the EPSG 4326 representation of the regional subbasin geometries
        /// </summary>
        /// <param name="dbContext"></param>
        /// <returns></returns>
        public Geometry? GetCentralizedDelineationGeometry4326(NeptuneDbContext dbContext)
        {
            var regionalSubbasin = GetRegionalSubbasin(dbContext);

            if (regionalSubbasin == null)
            {
                return null;
            }

            var unionOfUpstreamRegionalSubbasins = dbContext.vRegionalSubbasinUpstreamCatchmentGeometry4326s.AsNoTracking()
                .SingleOrDefault(x => x.PrimaryKey == regionalSubbasin.RegionalSubbasinID);

            return unionOfUpstreamRegionalSubbasins?.UpstreamCatchmentGeometry4326;
        }

        /// <summary>
        /// Performs the RSB trace for a given Treatment BMP using the EPSG 2771 representation of the regional subbasin geometries
        /// </summary>
        /// <param name="dbContext"></param>
        /// <returns></returns>
        public Geometry? GetCentralizedDelineationGeometry2771(NeptuneDbContext dbContext)
        {
            var regionalSubbasin = GetRegionalSubbasin(dbContext);

            if (regionalSubbasin == null)
            {
                return null;
            }

            var unionOfUpstreamRegionalSubbasins = dbContext.vRegionalSubbasinUpstreamCatchmentGeometries.AsNoTracking()
                .SingleOrDefault(x => x.PrimaryKey == regionalSubbasin.RegionalSubbasinID);

            return unionOfUpstreamRegionalSubbasins?.UpstreamCatchmentGeometry;
        }
    }
}
