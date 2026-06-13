/*-----------------------------------------------------------------------
<copyright file="TreatmentBMPAssessmentObservationType.cs" company="Tahoe Regional Planning Agency">
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
using Neptune.Common;
using Neptune.Common.GeoSpatial;
using Neptune.Models.DataTransferObjects;

namespace Neptune.EFModels.Entities
{
    public partial class TreatmentBMPAssessmentObservationType
    {
        public string DisplayNameWithUnits()
        {
            return
                $"{TreatmentBMPAssessmentObservationTypeName} {(GetMeasurementUnitType() != null ? $"({GetMeasurementUnitType().MeasurementUnitTypeDisplayName})" : string.Empty)}";
        }

        public bool GetHasBenchmarkAndThreshold()
        {
            return ObservationTypeSpecification.ObservationThresholdType != ObservationThresholdType.None;
        }

        public bool GetTargetIsSweetSpot()
        {
            return ObservationTypeSpecification.ObservationTargetType == ObservationTargetType.SpecificValue;
        }

        public MeasurementUnitType? GetMeasurementUnitType()
        {
            return BenchmarkMeasurementUnitType();
        }

        public DiscreteObservationTypeSchema GetDiscreteObservationTypeSchema()
        {
            return GeoJsonSerializer.Deserialize<DiscreteObservationTypeSchema>(TreatmentBMPAssessmentObservationTypeSchema);
        }

        public RateObservationTypeSchema GetRateObservationTypeSchema()
        {
            return GeoJsonSerializer.Deserialize<RateObservationTypeSchema>(
                TreatmentBMPAssessmentObservationTypeSchema);
        }

        public PassFailObservationTypeSchema GetPassFailSchema()
        {
            return GeoJsonSerializer.Deserialize<PassFailObservationTypeSchema>(
                TreatmentBMPAssessmentObservationTypeSchema);
        }

        public PercentageObservationTypeSchema GetPercentageSchema()
        {
            return GeoJsonSerializer.Deserialize<PercentageObservationTypeSchema>(
                TreatmentBMPAssessmentObservationTypeSchema);
        }

        public MeasurementUnitType? BenchmarkMeasurementUnitType()
        {
            var observationTypeCollectionMethod = ObservationTypeSpecification.ObservationTypeCollectionMethod;
            switch (observationTypeCollectionMethod.ToEnum)
            {
                case ObservationTypeCollectionMethodEnum.DiscreteValue:
                    return MeasurementUnitType.All
                        .SingleOrDefault(x => x.MeasurementUnitTypeID == GetDiscreteObservationTypeSchema().MeasurementUnitTypeID);
                case ObservationTypeCollectionMethodEnum.PassFail:
                    return null;
                case ObservationTypeCollectionMethodEnum.Percentage:
                    return MeasurementUnitType.Percent;
                default:
                    return null;
            }
        }

        public string BenchmarkMeasurementUnitLabel()
        {
            // NPT-1069: prefer the authoritative MeasurementUnitType lookup display name (e.g. "inches")
            // so the SPA matches how the unit is presented elsewhere. Fall back to the schema's free-text
            // label only when the OT has no resolvable MeasurementUnitTypeID (so it doesn't lose its suffix).
            var unitType = BenchmarkMeasurementUnitType();
            if (unitType != null)
            {
                return unitType.MeasurementUnitTypeDisplayName;
            }

            var observationTypeCollectionMethod = ObservationTypeSpecification.ObservationTypeCollectionMethod;
            switch (observationTypeCollectionMethod.ToEnum)
            {
                case ObservationTypeCollectionMethodEnum.DiscreteValue:
                    return GetDiscreteObservationTypeSchema().MeasurementUnitLabel;
                case ObservationTypeCollectionMethodEnum.Percentage:
                    return GetPercentageSchema().MeasurementUnitLabel;
                default:
                    return null;
            }
        }

        public string BenchmarkDescription()
        {
            var observationTypeCollectionMethod = ObservationTypeSpecification.ObservationTypeCollectionMethod;
            switch (observationTypeCollectionMethod.ToEnum)
            {
                case ObservationTypeCollectionMethodEnum.DiscreteValue:
                    return GetDiscreteObservationTypeSchema().BenchmarkDescription;
                case ObservationTypeCollectionMethodEnum.PassFail:
                    return null;
                case ObservationTypeCollectionMethodEnum.Percentage:
                    return GetPercentageSchema().BenchmarkDescription;
                default:
                    return null;
            }
        }

        public string ThresholdMeasurementUnitLabel()
        {
            // NPT-1069: resolve the unit from the MeasurementUnitType lookup display name (matching
            // BenchmarkMeasurementUnitLabel). Fall back to the benchmark label for SpecificValue when the
            // lookup is null so a free-text-only OT keeps its suffix.
            var unitType = ThresholdMeasurementUnitType();
            if (unitType != null)
            {
                return unitType.MeasurementUnitTypeDisplayName;
            }

            return ObservationTypeSpecification.ObservationThresholdType.ToEnum == ObservationThresholdTypeEnum.SpecificValue
                ? BenchmarkMeasurementUnitLabel()
                : null;
        }

        public MeasurementUnitType ThresholdMeasurementUnitType()
        {            
            var observationThresholdType = ObservationTypeSpecification.ObservationThresholdType;
            switch (observationThresholdType.ToEnum)
            {
                case ObservationThresholdTypeEnum.SpecificValue:
                    return BenchmarkMeasurementUnitType();
                case ObservationThresholdTypeEnum.RelativeToBenchmark:
                    return ThresholdMeasurementUnitForPercentFromBenchmark();
                case ObservationThresholdTypeEnum.None:
                    return null;
                default:
                    return null;
            }
        }

        public string ThresholdDescription()
        {
            var observationTypeCollectionMethod = ObservationTypeSpecification.ObservationTypeCollectionMethod;
            switch (observationTypeCollectionMethod.ToEnum)
            {
                case ObservationTypeCollectionMethodEnum.DiscreteValue:
                    return GetDiscreteObservationTypeSchema().ThresholdDescription;
                case ObservationTypeCollectionMethodEnum.PassFail:
                    return null;
                case ObservationTypeCollectionMethodEnum.Percentage:
                    return GetPercentageSchema().ThresholdDescription;
                default:
                    return null;
            }
        }

        private MeasurementUnitType ThresholdMeasurementUnitForPercentFromBenchmark()
        {
            var observationTargetType = ObservationTypeSpecification.ObservationTargetType;
            switch (observationTargetType.ToEnum)
            {
                case ObservationTargetTypeEnum.PassFail:
                    return null;
                case ObservationTargetTypeEnum.High:
                    return MeasurementUnitType.PercentDecline;
                case ObservationTargetTypeEnum.Low:
                    return MeasurementUnitType.PercentIncrease;
                case ObservationTargetTypeEnum.SpecificValue:
                    return MeasurementUnitType.PercentDeviation;
                default:
                    return null;
            }
        }

        public bool UseUpperValueForThreshold(double? benchmarkValue,double? observationValue)
        {
            if (benchmarkValue == null || observationValue == null)
            {
                return false;
            }
            return ThresholdMeasurementUnitType() == MeasurementUnitType.PercentIncrease ||
                   GetTargetIsSweetSpot() && observationValue > benchmarkValue;
        }

        public double? GetThresholdValueInBenchmarkUnits(double? benchmarkValue, double? thresholdValue, bool useUpperValue)
        {

            if (benchmarkValue == null || thresholdValue == null)
            {
                return null;
            }

            var thresholdMeasurementUnitType = ThresholdMeasurementUnitType();
            switch (thresholdMeasurementUnitType.ToEnum)
            {
                case MeasurementUnitTypeEnum.Acres:
                case MeasurementUnitTypeEnum.SquareFeet:
                case MeasurementUnitTypeEnum.Kilogram:
                case MeasurementUnitTypeEnum.Count:
                case MeasurementUnitTypeEnum.Percent:
                case MeasurementUnitTypeEnum.MilligamsPerLiter:
                case MeasurementUnitTypeEnum.Meters:
                case MeasurementUnitTypeEnum.Feet:
                case MeasurementUnitTypeEnum.Inches:
                case MeasurementUnitTypeEnum.InchesPerHour:
                case MeasurementUnitTypeEnum.Seconds:
                    if (!GetTargetIsSweetSpot())
                    {
                        return thresholdValue;
                    }
                    if (useUpperValue)
                    {
                        return benchmarkValue + thresholdValue;
                    }
                    return benchmarkValue - thresholdValue;
                case MeasurementUnitTypeEnum.PercentDecline:
                    return benchmarkValue - (thresholdValue / 100) * benchmarkValue;
                case MeasurementUnitTypeEnum.PercentIncrease:
                    return benchmarkValue + (thresholdValue / 100) * benchmarkValue;
                case MeasurementUnitTypeEnum.PercentDeviation:
                    if (useUpperValue)
                    {
                        return benchmarkValue + (thresholdValue / 100) * benchmarkValue;
                    }
                    return benchmarkValue - (thresholdValue / 100) * benchmarkValue;
                default:
                   return null;
            }
        }


        public double? GetBenchmarkValue(ICollection<TreatmentBMPBenchmarkAndThreshold> treatmentBMPBenchmarkAndThresholds)
        {
            var treatmentBMPBenchmarkAndThreshold = treatmentBMPBenchmarkAndThresholds
                .SingleOrDefault(x => x.TreatmentBMPAssessmentObservationTypeID == TreatmentBMPAssessmentObservationTypeID);

            return treatmentBMPBenchmarkAndThreshold?.BenchmarkValue;
        }

        public double? GetThresholdValue(ICollection<TreatmentBMPBenchmarkAndThreshold> treatmentBMPBenchmarkAndThresholds)
        {
            var treatmentBMPBenchmarkAndThreshold = treatmentBMPBenchmarkAndThresholds
                .SingleOrDefault(x => x.TreatmentBMPAssessmentObservationTypeID == TreatmentBMPAssessmentObservationTypeID);

            return treatmentBMPBenchmarkAndThreshold?.ThresholdValue;
        }

        public string GetFormattedBenchmarkValue(double? benchmarkValue)
        {
            if (!GetHasBenchmarkAndThreshold())
            {
                return ViewUtilities.NaString;
            }

            if (benchmarkValue == null)
            {
                return "-";
            }

            var optionalSpace = GetMeasurementUnitType().IncludeSpaceBeforeLegendLabel ? " " : "";
            return $"{benchmarkValue}{optionalSpace}{GetMeasurementUnitType().LegendDisplayName}";
        }

        public string GetFormattedThresholdValue(double? thresholdValue, double? benchmarkValue)
        {
            // observation type has no benchmark and thresholds, return "not applicable"
            if (!GetHasBenchmarkAndThreshold())
            {
                return ViewUtilities.NaString;
            }

            // threshold value not set, return "-"
            if (thresholdValue == null)
            {
                return "-";
            }

            var optionalSpace = ThresholdMeasurementUnitType().IncludeSpaceBeforeLegendLabel ? " " : "";
            var benchmarkOptionalSpace = BenchmarkMeasurementUnitType().IncludeSpaceBeforeLegendLabel ? " " : "";
            var formattedThresholdValue = $"{thresholdValue}{optionalSpace}{ThresholdMeasurementUnitType().LegendDisplayName}";

            if (!GetTargetIsSweetSpot() || benchmarkValue == null)
            {                
                return formattedThresholdValue;
            }

            // If target is sweet spot or high or low
            if (GetTargetIsSweetSpot())
            {
                var upperValueInBenchmarkUnits = GetThresholdValueInBenchmarkUnits(benchmarkValue, thresholdValue, true);
                var lowerValueInBenchmarkUnits = GetThresholdValueInBenchmarkUnits(benchmarkValue, thresholdValue, false);                
                return $"+/- {formattedThresholdValue} ({lowerValueInBenchmarkUnits} - {upperValueInBenchmarkUnits}{benchmarkOptionalSpace}{BenchmarkMeasurementUnitType().LegendDisplayName})";
            }

            var thresholdValueInBenchmarkUnits = GetThresholdValueInBenchmarkUnits(benchmarkValue, thresholdValue, ThresholdMeasurementUnitType() == MeasurementUnitType.PercentIncrease);

            return $"{formattedThresholdValue} ({thresholdValueInBenchmarkUnits}{benchmarkOptionalSpace}{BenchmarkMeasurementUnitType().LegendDisplayName})";
        }

        public async Task DeleteFull(NeptuneDbContext dbContext)
        {
            await dbContext.TreatmentBMPBenchmarkAndThresholds.Where(x =>
                x.TreatmentBMPAssessmentObservationTypeID == TreatmentBMPAssessmentObservationTypeID).ExecuteDeleteAsync();
            await dbContext.TreatmentBMPObservations
                .Where(x => x.TreatmentBMPAssessmentObservationTypeID == TreatmentBMPAssessmentObservationTypeID)
                .ExecuteDeleteAsync();
            await dbContext.TreatmentBMPTypeAssessmentObservationTypes.Where(x =>
                x.TreatmentBMPAssessmentObservationTypeID == TreatmentBMPAssessmentObservationTypeID).ExecuteDeleteAsync();
            await dbContext.TreatmentBMPAssessmentObservationTypes.Where(x =>
                x.TreatmentBMPAssessmentObservationTypeID == TreatmentBMPAssessmentObservationTypeID).ExecuteDeleteAsync();
        }
    }


}
